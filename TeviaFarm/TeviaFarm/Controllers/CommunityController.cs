using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;
using TeviaFarm.Models;

namespace TeviaFarm.Controllers
{
    public class CommunityController : Controller
    {
        private readonly AppDbContext _context;

        public CommunityController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId") != null;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .Where(p => p.IsApproved)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
            return View(posts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Post post)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(post);
            }

            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            post.UserId = userId;
            post.CreatedDate = DateTime.UtcNow;
            post.IsApproved = true; // hiển thị ngay sau khi tạo
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null || !post.IsApproved)
            {
                return NotFound();
            }
            return View(post);
        }
    }
}

