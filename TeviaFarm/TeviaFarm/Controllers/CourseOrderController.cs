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
    public class CourseOrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly VnPayService _vnPayService;

        private static class CourseOrderStatuses
        {
            public const string Pending = "Pending";
            public const string Paid = "Paid";
            public const string Cancelled = "Cancelled";
        }

        public CourseOrderController(AppDbContext context, IConfiguration config, VnPayService vnPayService)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int courseId)
        {
            if (courseId <= 0)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return NotFound();
            }

            var alreadyOwned = await _context.UserCourses
                .AnyAsync(x => x.UserId == userId.Value && x.CourseId == courseId);

            if (alreadyOwned)
            {
                TempData["ToastMessage"] = "Bạn đã sở hữu khóa học này rồi.";
                TempData["ToastType"] = "info";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            var existingPendingOrder = await _context.CourseOrders
                .Include(o => o.CourseOrderDetails)
                .FirstOrDefaultAsync(o =>
                    o.UserId == userId.Value &&
                    o.Status == CourseOrderStatuses.Pending &&
                    o.CourseOrderDetails.Any(d => d.CourseId == courseId));

            if (existingPendingOrder != null)
            {
                TempData["ToastMessage"] = "Bạn đã có đơn chờ thanh toán cho khóa học này.";
                TempData["ToastType"] = "info";
                return RedirectToAction(nameof(Checkout), new { id = existingPendingOrder.CourseOrderId });
            }

            var order = new CourseOrder
            {
                UserId = userId.Value,
                Status = CourseOrderStatuses.Pending,
                PaymentStatus = "Unpaid",
                TotalAmount = course.Price,
                CreatedDate = DateTime.UtcNow
            };

            order.CourseOrderDetails.Add(new CourseOrderDetail
            {
                CourseId = course.CourseId,
                Price = course.Price
            });

            _context.CourseOrders.Add(order);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Đã tạo đơn mua khóa học. Vui lòng tiếp tục thanh toán qua VNPAY.";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Checkout), new { id = order.CourseOrderId });
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.CourseOrders
                .Include(o => o.CourseOrderDetails)
                .ThenInclude(d => d.Course)
                .FirstOrDefaultAsync(o => o.CourseOrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && order.UserId != userId.Value)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (order.Status != CourseOrderStatuses.Pending)
            {
                return RedirectToAction(nameof(Details), new { id = order.CourseOrderId });
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id, string paymentMethod)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            paymentMethod = paymentMethod?.Trim() ?? "";

            var allowedPaymentMethods = new[] { "VnPay" };
            if (string.IsNullOrWhiteSpace(paymentMethod) || !allowedPaymentMethods.Contains(paymentMethod))
            {
                ModelState.AddModelError(string.Empty, "Khóa học chỉ hỗ trợ thanh toán qua VNPAY.");
            }

            var order = await _context.CourseOrders
                .Include(o => o.CourseOrderDetails)
                .ThenInclude(d => d.Course)
                .FirstOrDefaultAsync(o => o.CourseOrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && order.UserId != userId.Value)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (order.Status != CourseOrderStatuses.Pending)
            {
                return RedirectToAction(nameof(Details), new { id = order.CourseOrderId });
            }

            if (!ModelState.IsValid)
            {
                return View(order);
            }

            try
            {
                order.PaymentMethod = paymentMethod;
                order.PaymentStatus = "Pending";
                order.Status = CourseOrderStatuses.Pending;

                if (string.IsNullOrWhiteSpace(order.VnpTxnRef))
                {
                    order.VnpTxnRef = $"COURSE{order.CourseOrderId}_{DateTime.Now:yyyyMMddHHmmss}";
                }

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
                    ["vnp_OrderInfo"] = $"Thanh toan khoa hoc {order.CourseOrderId}",
                    ["vnp_OrderType"] = "other",
                    ["vnp_ReturnUrl"] = _config["VnPay:ReturnUrl"]!,
                    ["vnp_TxnRef"] = order.VnpTxnRef,
                    ["vnp_ExpireDate"] = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss")
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    _config["VnPay:BaseUrl"]!,
                    _config["VnPay:HashSecret"]!,
                    vnpData);

                return Redirect(paymentUrl);
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Không thể tạo thanh toán VNPAY. Vui lòng thử lại.");
                return View(order);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.CourseOrders
                .Include(o => o.CourseOrderDetails)
                .ThenInclude(d => d.Course)
                .FirstOrDefaultAsync(o => o.CourseOrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && order.UserId != userId.Value)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.CourseOrders
                .Include(o => o.CourseOrderDetails)
                .ThenInclude(d => d.Course)
                .FirstOrDefaultAsync(o => o.CourseOrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && order.UserId != userId.Value)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> History(int page = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            const int pageSize = 10;

            var query = _context.CourseOrders
                .Include(o => o.CourseOrderDetails)
                .ThenInclude(d => d.Course)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.CreatedDate)
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