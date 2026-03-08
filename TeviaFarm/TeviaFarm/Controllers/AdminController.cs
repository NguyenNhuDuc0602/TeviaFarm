using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;

namespace TeviaFarm.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        public IActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // Product Management
        public async Task<IActionResult> Products()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var products = await _context.Products.ToListAsync();
            return View(products);
        }

        public IActionResult CreateProduct()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(Models.Product product)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        public async Task<IActionResult> EditProduct(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Models.Product product)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Products));
        }

        // Order Management
        public async Task<IActionResult> Orders()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Orders));
        }

        // User Management
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // Community Management
        public async Task<IActionResult> Posts()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var posts = await _context.Posts.OrderByDescending(p => p.CreatedDate).ToListAsync();
            return View(posts);
        }

        [HttpPost]
        public async Task<IActionResult> ApprovePost(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                post.IsApproved = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Posts));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Posts));
        }

        // Course Management
        public async Task<IActionResult> Courses()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var courses = await _context.Courses
                .Include(c => c.Lessons)
                .ToListAsync();
            return View(courses);
        }

        public IActionResult CreateCourse()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(Models.Course course)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(course);
            }

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Courses));
        }

        public async Task<IActionResult> CreateLesson(int courseId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var lesson = new Models.Lesson { CourseId = courseId };
            return View(lesson);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLesson(Models.Lesson lesson)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(lesson);
            }

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Courses));
        }
    }
}

