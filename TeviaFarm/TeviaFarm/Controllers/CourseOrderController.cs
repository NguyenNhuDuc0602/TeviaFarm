using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;
using TeviaFarm.Models;

namespace TeviaFarm.Controllers
{
    [Authorize]
    public class CourseOrderController : Controller
    {
        private readonly AppDbContext _context;

        private static class CourseOrderStatuses
        {
            public const string Pending = "Pending";
            public const string Paid = "Paid";
            public const string Cancelled = "Cancelled";
        }

        public CourseOrderController(AppDbContext context)
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
                TempData["ToastMessage"] = "Bạn có đơn chờ thanh toán cho khóa học này. Tiếp tục thanh toán nhé.";
                TempData["ToastType"] = "info";
                return RedirectToAction(nameof(Checkout), new { id = existingPendingOrder.CourseOrderId });
            }

            var order = new CourseOrder
            {
                UserId = userId.Value,
                Status = CourseOrderStatuses.Pending,
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

            TempData["ToastMessage"] = "Đã tạo đơn mua khóa học. Vui lòng thanh toán để mở khóa.";
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

            var allowedPaymentMethods = new[] { "COD", "ChuyenKhoan" };
            if (string.IsNullOrWhiteSpace(paymentMethod) || !allowedPaymentMethods.Contains(paymentMethod))
            {
                ModelState.AddModelError(string.Empty, "Phương thức thanh toán không hợp lệ.");
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

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                order.PaymentMethod = paymentMethod;
                order.Status = CourseOrderStatuses.Paid;

                foreach (var detail in order.CourseOrderDetails)
                {
                    var alreadyOwned = await _context.UserCourses
                        .AnyAsync(x => x.UserId == userId.Value && x.CourseId == detail.CourseId);

                    if (!alreadyOwned)
                    {
                        _context.UserCourses.Add(new UserCourse
                        {
                            UserId = userId.Value,
                            CourseId = detail.CourseId,
                            EnrolledDate = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["ToastMessage"] = "Thanh toán khóa học thành công.";
                TempData["ToastType"] = "success";
                return RedirectToAction(nameof(Success), new { id = order.CourseOrderId });
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Thanh toán khóa học thất bại. Vui lòng thử lại.");
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