using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;
using TeviaFarm.Services;

namespace TeviaFarm.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly VnPayService _vnPayService;

        private static class OrderStatuses
        {
            public const string Pending = "Pending";
            public const string PendingPayment = "PendingPayment";
            public const string Shipping = "Shipping";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
        }

        public OrderController(AppDbContext context, IConfiguration config, VnPayService vnPayService)
        {
            _context = context;
            _config = config;
            _vnPayService = vnPayService;
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
            paymentMethod = paymentMethod?.Trim() ?? "";

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var allowedPaymentMethods = new[] { "COD", "VnPay" };

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
                    ModelState.AddModelError(
                        string.Empty,
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
                var orderStatus = paymentMethod == "VnPay"
                    ? OrderStatuses.PendingPayment
                    : OrderStatuses.Pending;

                var order = new Models.Order
                {
                    UserId = userId.Value,
                    ShippingAddress = shippingAddress,
                    PaymentMethod = paymentMethod,
                    Status = orderStatus,
                    PaymentStatus = paymentMethod == "VnPay" ? "Pending" : "Unpaid",
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

                    // Giữ logic hiện tại của bạn: trừ kho ngay khi tạo đơn
                    item.Product.Stock -= item.Quantity;
                }

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cart.Items);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (paymentMethod == "VnPay")
                {
                    var txnRef = $"ORD{order.OrderId}_{DateTime.Now:yyyyMMddHHmmss}";
                    order.VnpTxnRef = txnRef;
                    await _context.SaveChangesAsync();

                    var vnpData = new SortedDictionary<string, string>
                    {
                        ["vnp_Version"] = "2.1.0",
                        ["vnp_Command"] = "pay",
                        ["vnp_TmnCode"] = _config["VnPay:TmnCode"]!,
                        ["vnp_Amount"] = ((long)order.TotalAmount * 100).ToString(),
                        ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
                        ["vnp_CurrCode"] = "VND",
                        ["vnp_IpAddr"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                        ["vnp_Locale"] = "vn",
                        ["vnp_OrderInfo"] = $"Thanh toan don hang {order.OrderId}",
                        ["vnp_OrderType"] = "other",
                        ["vnp_ReturnUrl"] = _config["VnPay:ReturnUrl"]!,
                        ["vnp_TxnRef"] = txnRef,
                        ["vnp_ExpireDate"] = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss")
                    };

                    var paymentUrl = _vnPayService.CreatePaymentUrl(
                        _config["VnPay:BaseUrl"]!,
                        _config["VnPay:HashSecret"]!,
                        vnpData);

                    return Redirect(paymentUrl);
                }

                TempData["ToastMessage"] = "Đặt hàng thành công. Đơn hàng của bạn đang chờ xử lý.";
                TempData["ToastType"] = "success";
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

        public async Task<IActionResult> History(int page = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            const int pageSize = 10;

            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }
    }
}