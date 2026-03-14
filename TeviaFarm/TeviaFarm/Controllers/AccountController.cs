using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;
using TeviaFarm.Models;

namespace TeviaFarm.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            model.Username = (model.Username ?? "").Trim();
            model.Email = (model.Email ?? "").Trim().ToLower();
            model.Password = model.Password ?? "";
            model.ConfirmPassword = model.ConfirmPassword ?? "";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Username.Contains(" "))
            {
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập không được chứa khoảng trắng.");
                return View(model);
            }

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == model.Username.ToLower());

            if (usernameExists)
            {
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                Role = "Customer",
                CreatedDate = DateTime.UtcNow
            };

            user.Password = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.Username = (model.Username ?? "").Trim();
            model.Password = model.Password ?? "";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == model.Username.ToLower());

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            await SignInUserAsync(user);

            TempData["ToastMessage"] = $"Đăng nhập thành công. Xin chào {user.Username}!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy thông tin tài khoản.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index", "Home");
            }

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedDate = user.CreatedDate
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            model.Username = (model.Username ?? "").Trim();
            model.Email = (model.Email ?? "").Trim().ToLower();
            model.CurrentPassword = (model.CurrentPassword ?? "").Trim();
            model.NewPassword = (model.NewPassword ?? "").Trim();
            model.ConfirmNewPassword = (model.ConfirmNewPassword ?? "").Trim();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy thông tin tài khoản.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index", "Home");
            }

            model.UserId = user.UserId;
            model.Role = user.Role;
            model.CreatedDate = user.CreatedDate;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Username.Contains(" "))
            {
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập không được chứa khoảng trắng.");
                return View(model);
            }

            var usernameExists = await _context.Users
                .AnyAsync(u => u.UserId != user.UserId && u.Username.ToLower() == model.Username.ToLower());

            if (usernameExists)
            {
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.UserId != user.UserId && u.Email.ToLower() == model.Email.ToLower());

            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
                return View(model);
            }

            var isChangingPassword =
                !string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                !string.IsNullOrWhiteSpace(model.NewPassword) ||
                !string.IsNullOrWhiteSpace(model.ConfirmNewPassword);

            if (isChangingPassword)
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "Vui lòng nhập mật khẩu hiện tại.");
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    ModelState.AddModelError(nameof(model.NewPassword), "Vui lòng nhập mật khẩu mới.");
                    return View(model);
                }

                var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, model.CurrentPassword);
                if (verifyResult == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng.");
                    return View(model);
                }

                user.Password = _passwordHasher.HashPassword(user, model.NewPassword);
            }

            user.Username = model.Username;
            user.Email = model.Email;

            await _context.SaveChangesAsync();
            await SignInUserAsync(user);

            TempData["ToastMessage"] = "Cập nhật hồ sơ thành công.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            TempData["ToastMessage"] = "Bạn không có quyền truy cập chức năng này.";
            TempData["ToastType"] = "warning";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            TempData["ToastMessage"] = "Đăng xuất thành công.";
            TempData["ToastType"] = "info";

            return RedirectToAction("Index", "Home");
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }

        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}