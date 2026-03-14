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

        [HttpGet]
        public async Task<IActionResult> Index(int? id)
        {
            var approvedPostsQuery = _context.Posts
                .Include(p => p.User)
                .Where(p => p.IsApproved)
                .OrderByDescending(p => p.CreatedDate)
                .AsQueryable();

            var latestPosts = await approvedPostsQuery
                .Take(5)
                .ToListAsync();

            Post? currentPost;

            if (id.HasValue)
            {
                currentPost = await _context.Posts
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PostId == id.Value && p.IsApproved);
            }
            else
            {
                currentPost = await approvedPostsQuery.FirstOrDefaultAsync();
            }

            if (currentPost == null)
            {
                var emptyModel = new CommunityReaderViewModel
                {
                    LatestPosts = latestPosts
                };

                return View(emptyModel);
            }

            var comments = await _context.PostComments
                .Include(c => c.User)
                .Where(c => c.PostId == currentPost.PostId)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            var allApprovedPosts = await _context.Posts
                .Where(p => p.IsApproved)
                .OrderBy(p => p.CreatedDate)
                .Select(p => new { p.PostId, p.Title })
                .ToListAsync();

            var currentIndex = allApprovedPosts.FindIndex(p => p.PostId == currentPost.PostId);

            int? previousPostId = null;
            string? previousPostTitle = null;
            int? nextPostId = null;
            string? nextPostTitle = null;

            if (currentIndex > 0)
            {
                previousPostId = allApprovedPosts[currentIndex - 1].PostId;
                previousPostTitle = allApprovedPosts[currentIndex - 1].Title;
            }

            if (currentIndex >= 0 && currentIndex < allApprovedPosts.Count - 1)
            {
                nextPostId = allApprovedPosts[currentIndex + 1].PostId;
                nextPostTitle = allApprovedPosts[currentIndex + 1].Title;
            }

            var model = new CommunityReaderViewModel
            {
                CurrentPost = currentPost,
                LatestPosts = latestPosts.Where(p => p.PostId != currentPost.PostId).ToList(),
                Comments = comments,
                PreviousPostId = previousPostId,
                PreviousPostTitle = previousPostTitle,
                NextPostId = nextPostId,
                NextPostTitle = nextPostTitle
            };

            return View(model);
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

            post.Title = post.Title?.Trim() ?? string.Empty;
            post.Content = post.Content?.Trim() ?? string.Empty;
            post.ImageUrl = string.IsNullOrWhiteSpace(post.ImageUrl) ? null : post.ImageUrl.Trim();

            post.UserId = userId.Value;
            post.CreatedDate = DateTime.UtcNow;
            post.IsApproved = false;

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Bài viết đã được gửi và đang chờ quản trị viên duyệt.";
            return RedirectToAction(nameof(Details), new { id = post.PostId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var currentUserId = GetCurrentUserId();

            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            var isOwner = currentUserId != null && post.UserId == currentUserId.Value;

            if (!post.IsApproved && !isOwner)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index), new { id });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != userId.Value)
            {
                return Forbid();
            }

            return View(post);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Post model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == model.PostId);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != userId.Value)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            post.Title = model.Title?.Trim() ?? string.Empty;
            post.Content = model.Content?.Trim() ?? string.Empty;
            post.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();

            await _context.SaveChangesAsync();

            TempData["Message"] = "Đã cập nhật bài viết thành công.";
            return RedirectToAction(nameof(Index), new { id = post.PostId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != userId.Value && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đã xóa bài viết thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
            if (post == null)
            {
                return NotFound();
            }

            var isOwner = post.UserId == userId.Value;

            if (!post.IsApproved && !isOwner)
            {
                return NotFound();
            }

            content = content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Message"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction(nameof(Index), new { id = postId });
            }

            var comment = new PostComment
            {
                PostId = postId,
                UserId = userId.Value,
                Content = content,
                CreatedDate = DateTime.UtcNow
            };

            _context.PostComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đã thêm bình luận.";
            return RedirectToAction(nameof(Index), new { id = postId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCommentFromReader(int postId, string content)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["Message"] = "Vui lòng đăng nhập để bình luận.";
                return RedirectToAction("Login", "Account");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == postId && p.IsApproved);
            if (post == null)
            {
                return NotFound();
            }

            content = content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Message"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction(nameof(Index), new { id = postId });
            }

            var comment = new PostComment
            {
                PostId = postId,
                UserId = userId.Value,
                Content = content,
                CreatedDate = DateTime.UtcNow
            };

            _context.PostComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đã thêm bình luận.";
            return RedirectToAction(nameof(Index), new { id = postId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var comment = await _context.PostComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.PostCommentId == id);

            if (comment == null)
            {
                return NotFound();
            }

            var isCommentOwner = comment.UserId == userId.Value;
            var isAdmin = User.IsInRole("Admin");

            if (!isCommentOwner && !isAdmin)
            {
                return Forbid();
            }

            var postId = comment.PostId;

            _context.PostComments.Remove(comment);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đã xóa bình luận.";
            return RedirectToAction(nameof(Index), new { id = postId });
        }
    }
}