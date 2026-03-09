using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;

namespace TeviaFarm.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private const int AdminPageSize = 10;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Products(int page = 1)
        {
            var query = _context.Products
                .OrderByDescending(p => p.ProductId)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / AdminPageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var products = await query
                .Skip((page - 1) * AdminPageSize)
                .Take(AdminPageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(products);
        }

        public IActionResult CreateProduct()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Models.Product product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Đã thêm sản phẩm.";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Products));
        }

        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Models.Product product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Đã cập nhật sản phẩm.";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Đã xóa sản phẩm.";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không tìm thấy sản phẩm để xóa.";
                TempData["ToastType"] = "warning";
            }

            return RedirectToAction(nameof(Products));
        }

        public async Task<IActionResult> Orders(int page = 1)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / AdminPageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var orders = await query
                .Skip((page - 1) * AdminPageSize)
                .Take(AdminPageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            status = status?.Trim() ?? "";

            var allowedStatuses = new[] { "Pending", "Shipping", "Completed", "Cancelled" };
            if (string.IsNullOrWhiteSpace(status) || !allowedStatuses.Contains(status))
            {
                TempData["ToastMessage"] = "Trạng thái không hợp lệ.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Orders));
            }

            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = $"Đã cập nhật trạng thái đơn #{id}.";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không tìm thấy đơn hàng.";
                TempData["ToastType"] = "warning";
            }

            return RedirectToAction(nameof(Orders));
        }

        public async Task<IActionResult> Users(int page = 1)
        {
            var query = _context.Users
                .OrderByDescending(u => u.CreatedDate)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / AdminPageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var users = await query
                .Skip((page - 1) * AdminPageSize)
                .Take(AdminPageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(users);
        }

        public async Task<IActionResult> Posts(int page = 1)
        {
            var query = _context.Posts
                .OrderByDescending(p => p.CreatedDate)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / AdminPageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var posts = await query
                .Skip((page - 1) * AdminPageSize)
                .Take(AdminPageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(posts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                post.IsApproved = true;
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Đã duyệt bài viết.";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không tìm thấy bài viết.";
                TempData["ToastType"] = "warning";
            }

            return RedirectToAction(nameof(Posts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Đã xóa bài viết.";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không tìm thấy bài viết để xóa.";
                TempData["ToastType"] = "warning";
            }

            return RedirectToAction(nameof(Posts));
        }

        public async Task<IActionResult> Courses()
        {
            var courses = await _context.Courses
                .Include(c => c.Lessons)
                .ToListAsync();

            return View(courses);
        }

        public IActionResult CreateCourse()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Models.Course course)
        {
            if (!ModelState.IsValid)
            {
                return View(course);
            }

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Đã tạo khóa học.";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Courses));
        }

        public async Task<IActionResult> CreateLesson(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var lesson = new Models.Lesson { CourseId = courseId };
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(Models.Lesson lesson)
        {
            if (!ModelState.IsValid)
            {
                return View(lesson);
            }

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Đã thêm bài học.";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Courses));
        }
    }
}