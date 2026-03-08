using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            return int.Parse(userIdClaim);
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.IsApproved)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(posts);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(post);
            }

            post.UserId = userId.Value;
            post.CreatedDate = DateTime.UtcNow;
            post.IsApproved = false; // chờ admin duyệt

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Bài viết đã được gửi và đang chờ quản trị viên duyệt.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null || !post.IsApproved)
            {
                return NotFound();
            }

            return View(post);
        }
    }
}