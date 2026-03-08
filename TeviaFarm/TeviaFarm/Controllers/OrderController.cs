using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;

namespace TeviaFarm.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        private static class OrderStatuses
        {
            public const string Pending = "Pending";
            public const string Shipping = "Shipping";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
        }

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            return int.Parse(userIdClaim);
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string shippingAddress, string? paymentMethod)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            shippingAddress = shippingAddress?.Trim() ?? "";
            paymentMethod = paymentMethod?.Trim();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var allowedPaymentMethods = new[] { "COD", "ChuyenKhoan" };

            if (string.IsNullOrWhiteSpace(shippingAddress))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập địa chỉ giao hàng.");
            }
            else if (shippingAddress.Length > 255)
            {
                ModelState.AddModelError(string.Empty, "Địa chỉ giao hàng không được vượt quá 255 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod) || !allowedPaymentMethods.Contains(paymentMethod))
            {
                ModelState.AddModelError(string.Empty, "Phương thức thanh toán không hợp lệ.");
            }

            foreach (var item in cart.Items)
            {
                if (item.Product == null)
                {
                    ModelState.AddModelError(string.Empty, "Có sản phẩm không tồn tại trong giỏ hàng.");
                    continue;
                }

                if (item.Quantity <= 0)
                {
                    ModelState.AddModelError(string.Empty, $"Số lượng không hợp lệ cho sản phẩm {item.Product.ProductName}.");
                }

                if (item.Product.Stock < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Sản phẩm '{item.Product.ProductName}' chỉ còn {item.Product.Stock} trong kho, không đủ số lượng bạn đặt.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(cart);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Models.Order
                {
                    UserId = userId.Value,
                    ShippingAddress = shippingAddress,
                    PaymentMethod = paymentMethod,
                    Status = OrderStatuses.Pending,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = cart.Items.Sum(i => i.Product!.Price * i.Quantity)
                };

                foreach (var item in cart.Items)
                {
                    order.OrderDetails.Add(new Models.OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Product!.Price
                    });

                    item.Product.Stock -= item.Quantity;
                }

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cart.Items);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("Success", new { id = order.OrderId });
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Đặt hàng thất bại. Vui lòng thử lại.");
                return View(cart);
            }
        }

        public async Task<IActionResult> Success(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!User.IsInRole("Admin") && order.UserId != currentUserId.Value)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(order);
        }

        public async Task<IActionResult> History()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}