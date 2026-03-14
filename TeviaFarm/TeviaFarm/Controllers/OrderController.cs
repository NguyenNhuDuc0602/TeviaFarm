using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;
using TeviaFarm.Models;
using TeviaFarm.Services;

namespace TeviaFarm.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly VnPayService _vnPayService;
        private readonly IGhtkService _ghtkService;

        private static class OrderStatuses
        {
            public const string Pending = "Pending";
            public const string PendingPayment = "PendingPayment";
            public const string Shipping = "Shipping";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
        }

        public OrderController(
            AppDbContext context,
            IConfiguration config,
            VnPayService vnPayService,
            IGhtkService ghtkService)
        {
            _context = context;
            _config = config;
            _vnPayService = vnPayService;
            _ghtkService = ghtkService;
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

            var subtotal = cart.Items.Sum(i => i.Quantity * (i.Product != null ? i.Product.Price : 0));

            var model = new CheckoutViewModel
            {
                SubtotalAmount = subtotal,
                ShippingFee = 0,
                TotalAmount = subtotal,
                PaymentMethod = "COD"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CalculateShippingFee([FromBody] ShippingFeeRequest model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Bạn chưa đăng nhập." });
            }

            if (string.IsNullOrWhiteSpace(model.ShippingProvince) || string.IsNullOrWhiteSpace(model.ShippingDistrict))
            {
                return BadRequest(new { message = "Vui lòng nhập tỉnh/thành và quận/huyện." });
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || !cart.Items.Any())
            {
                return BadRequest(new { message = "Giỏ hàng trống." });
            }

            var subtotal = cart.Items.Sum(i => i.Quantity * (i.Product != null ? i.Product.Price : 0));

            // Nếu chưa có WeightKg thì tạm tính mặc định 0.5kg / mỗi sản phẩm
            double totalWeightKg = cart.Items.Sum(i => i.Quantity * ((i.Product?.WeightKg) ?? 0.5));

            var shippingFee = await _ghtkService.CalculateShippingFeeAsync(
                model.ShippingProvince.Trim(),
                model.ShippingDistrict.Trim(),
                totalWeightKg,
                subtotal
            );

            if (shippingFee == null)
            {
                return BadRequest(new { message = "Không lấy được phí vận chuyển từ GHTK." });
            }

            return Json(new
            {
                subtotal,
                shippingFee = shippingFee.Value,
                total = subtotal + shippingFee.Value
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.ReceiverName = model.ReceiverName?.Trim() ?? "";
            model.ReceiverPhone = model.ReceiverPhone?.Trim() ?? "";
            model.ShippingAddress = model.ShippingAddress?.Trim() ?? "";
            model.ShippingWard = model.ShippingWard?.Trim() ?? "";
            model.ShippingDistrict = model.ShippingDistrict?.Trim() ?? "";
            model.ShippingProvince = model.ShippingProvince?.Trim() ?? "";
            model.PaymentMethod = model.PaymentMethod?.Trim() ?? "";

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var allowedPaymentMethods = new[] { "COD", "VnPay" };

            if (string.IsNullOrWhiteSpace(model.ReceiverName))
            {
                ModelState.AddModelError(nameof(model.ReceiverName), "Vui lòng nhập họ tên người nhận.");
            }

            if (string.IsNullOrWhiteSpace(model.ReceiverPhone))
            {
                ModelState.AddModelError(nameof(model.ReceiverPhone), "Vui lòng nhập số điện thoại người nhận.");
            }

            if (string.IsNullOrWhiteSpace(model.ShippingAddress))
            {
                ModelState.AddModelError(nameof(model.ShippingAddress), "Vui lòng nhập địa chỉ chi tiết.");
            }

            if (string.IsNullOrWhiteSpace(model.ShippingWard))
            {
                ModelState.AddModelError(nameof(model.ShippingWard), "Vui lòng nhập phường/xã.");
            }

            if (string.IsNullOrWhiteSpace(model.ShippingDistrict))
            {
                ModelState.AddModelError(nameof(model.ShippingDistrict), "Vui lòng nhập quận/huyện.");
            }

            if (string.IsNullOrWhiteSpace(model.ShippingProvince))
            {
                ModelState.AddModelError(nameof(model.ShippingProvince), "Vui lòng nhập tỉnh/thành phố.");
            }

            if (string.IsNullOrWhiteSpace(model.PaymentMethod) || !allowedPaymentMethods.Contains(model.PaymentMethod))
            {
                ModelState.AddModelError(nameof(model.PaymentMethod), "Phương thức thanh toán không hợp lệ.");
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

            var subtotal = cart.Items.Sum(i => i.Quantity * (i.Product != null ? i.Product.Price : 0));
            model.SubtotalAmount = subtotal;
            model.TotalAmount = subtotal;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            double totalWeightKg = cart.Items.Sum(i => i.Quantity * ((i.Product?.WeightKg) ?? 0.5));

            var shippingFee = await _ghtkService.CalculateShippingFeeAsync(
                model.ShippingProvince,
                model.ShippingDistrict,
                totalWeightKg,
                subtotal
            );

            if (shippingFee == null)
            {
                ModelState.AddModelError(string.Empty, "Không lấy được phí vận chuyển từ GHTK.");
                model.ShippingFee = 0;
                model.TotalAmount = subtotal;
                return View(model);
            }

            model.ShippingFee = shippingFee.Value;
            model.TotalAmount = subtotal + shippingFee.Value;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var orderStatus = model.PaymentMethod == "VnPay"
                    ? OrderStatuses.PendingPayment
                    : OrderStatuses.Pending;

                var txnRef = model.PaymentMethod == "VnPay"
                    ? $"ORD_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}"
                    : null;

                var order = new Order
                {
                    UserId = userId.Value,
                    ReceiverName = model.ReceiverName,
                    ReceiverPhone = model.ReceiverPhone,
                    ShippingAddress = model.ShippingAddress,
                    ShippingWard = model.ShippingWard,
                    ShippingDistrict = model.ShippingDistrict,
                    ShippingProvince = model.ShippingProvince,
                    PaymentMethod = model.PaymentMethod,
                    Status = orderStatus,
                    PaymentStatus = model.PaymentMethod == "VnPay" ? "Pending" : "Unpaid",
                    OrderDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    SubtotalAmount = subtotal,
                    ShippingFee = shippingFee.Value,
                    TotalAmount = subtotal + shippingFee.Value,
                    VnpTxnRef = txnRef
                };

                foreach (var item in cart.Items)
                {
                    order.OrderDetails.Add(new OrderDetail
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

                if (model.PaymentMethod == "VnPay")
                {
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
                        ["vnp_TxnRef"] = order.VnpTxnRef!,
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
                return View(model);
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