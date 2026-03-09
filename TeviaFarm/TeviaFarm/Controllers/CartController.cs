using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;

namespace TeviaFarm.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
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

        private async Task<Models.Cart> GetOrCreateCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null)
            {
                return cart;
            }

            cart = new Models.Cart
            {
                UserId = userId
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            return cart;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetOrCreateCartAsync(userId.Value);
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (productId <= 0)
            {
                return NotFound();
            }

            if (quantity <= 0 || quantity > 1000)
            {
                return RedirectToAction("Index");
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = await GetOrCreateCartAsync(userId.Value);

            var item = await _context.CartItems
                .FirstOrDefaultAsync(i => i.CartId == cart.CartId && i.ProductId == productId);

            if (item == null)
            {
                item = new Models.CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(item);
            }
            else
            {
                item.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = $"Đã thêm vào giỏ: {product.ProductName} (x{quantity}).";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (cartItemId <= 0)
            {
                return RedirectToAction("Index");
            }

            if (quantity < 0 || quantity > 1000)
            {
                return RedirectToAction("Index");
            }

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                return RedirectToAction("Index");
            }

            var item = await _context.CartItems
                .FirstOrDefaultAsync(i => i.CartItemId == cartItemId && i.CartId == cart.CartId);

            if (item == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (quantity == 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = quantity == 0 ? "Đã xóa sản phẩm khỏi giỏ hàng." : "Đã cập nhật số lượng.";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (cartItemId <= 0)
            {
                return RedirectToAction("Index");
            }

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                return RedirectToAction("Index");
            }

            var item = await _context.CartItems
                .FirstOrDefaultAsync(i => i.CartItemId == cartItemId && i.CartId == cart.CartId);

            if (item == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }
    }
}