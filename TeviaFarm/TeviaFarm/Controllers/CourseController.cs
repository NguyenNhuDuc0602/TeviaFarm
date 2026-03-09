using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;

namespace TeviaFarm.Controllers
{
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;

        public CourseController(AppDbContext context)
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

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 9;

            var query = _context.Courses
                .OrderByDescending(c => c.CourseId)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var courses = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(courses);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            bool isOwned = false;

            var userId = GetCurrentUserId();
            if (userId != null)
            {
                isOwned = await _context.UserCourses
                    .AnyAsync(uc => uc.UserId == userId.Value && uc.CourseId == id);
            }

            ViewBag.IsOwned = isOwned;

            return View(course);
        }

        [Authorize]
        public async Task<IActionResult> Lesson(int id)
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

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (lesson == null)
            {
                return NotFound();
            }

            var isOwned = await _context.UserCourses
                .AnyAsync(uc => uc.UserId == userId.Value && uc.CourseId == lesson.CourseId);

            if (!isOwned && !User.IsInRole("Admin"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(lesson);
        }

        [Authorize]
        public async Task<IActionResult> MyCourses()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var courseIds = await _context.UserCourses
                .Where(uc => uc.UserId == userId.Value)
                .Select(uc => uc.CourseId)
                .ToListAsync();

            var courses = await _context.Courses
                .Include(c => c.Lessons)
                .Where(c => courseIds.Contains(c.CourseId))
                .ToListAsync();

            return View(courses);
        }
    }
}