using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Utilities;
using BVUB_WebTuyenDung.Areas.Admin.Services; 
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting; // <-- để biết env

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly AdminDbContext _context;
        private readonly IDataProtector _protector;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _env; // để show lỗi chi tiết khi Development

        private const string LoginFailCountKey = "AdminLoginFailCount";

        public AccountController(
            AdminDbContext context,
            IDataProtectionProvider dp,
            ILogger<AccountController> logger,
            IEmailSender emailSender,
            IWebHostEnvironment env)
        {
            _context = context;
            _protector = dp.CreateProtector("Admin.PasswordResetToken.v1");
            _logger = logger;
            _emailSender = emailSender;
            _env = env;
        }

        // ===================== LOGIN =====================
        [HttpGet, AllowAnonymous]
        public IActionResult Login()
        {
            ViewBag.ShowForgot = (HttpContext.Session.GetInt32(LoginFailCountKey) ?? 0) >= 3;
            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
        {
            username = username?.Trim();
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                BumpFailAndMarkForgot();
                ViewBag.Error = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
                return View();
            }

            var key = username.ToLower();
            var user = await _context.AdminUsers
                .FirstOrDefaultAsync(a =>
                    (a.Username != null && a.Username.ToLower() == key) ||
                    (a.Email != null && a.Email.ToLower() == key));

            if (user == null)
            {
                BumpFailAndMarkForgot();
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
                return View();
            }

            bool ok = string.Equals(user.PasswordHash ?? string.Empty, password, StringComparison.Ordinal);
            if (!ok)
            {
                try { ok = PasswordHasher.Verify(password, user.PasswordHash); }
                catch { ok = false; }
            }

            if (!ok)
            {
                BumpFailAndMarkForgot();
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
                return View();
            }

            HttpContext.Session.Remove(LoginFailCountKey);

            if (!IsLikelyHash(user.PasswordHash))
            {
                user.PasswordHash = HashPassword(password);
                await _context.SaveChangesAsync();
            }

            bool isAdmin = user.Role == 1;
            string roleName = isAdmin ? "Admin" : "Staff";
            string roleNumeric = isAdmin ? "1" : "2";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(ClaimTypes.Role, roleNumeric),
                new Claim("MenuPerms", user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
            return RedirectToAction("Index", "HomeAdmin", new { area = "Admin" });
        }

        private void BumpFailAndMarkForgot()
        {
            var cnt = (HttpContext.Session.GetInt32(LoginFailCountKey) ?? 0) + 1;
            HttpContext.Session.SetInt32(LoginFailCountKey, cnt);
            ViewBag.ShowForgot = cnt >= 3;
        }

        // ===================== CHANGE PASSWORD (SELF) =====================
        public class ChangePasswordVm
        {
            public string CurrentPassword { get; set; } = "";
            public string NewPassword { get; set; } = "";
            public string ConfirmPassword { get; set; } = "";
            public string? Message { get; set; }
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordVm());

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVm vm)
        {
            if (string.IsNullOrWhiteSpace(vm.CurrentPassword))
                ModelState.AddModelError(nameof(vm.CurrentPassword), "Vui lòng nhập mật khẩu hiện tại.");
            if (string.IsNullOrWhiteSpace(vm.NewPassword) || vm.NewPassword.Length < 6)
                ModelState.AddModelError(nameof(vm.NewPassword), "Mật khẩu mới tối thiểu 6 ký tự.");
            if (vm.NewPassword != vm.ConfirmPassword)
                ModelState.AddModelError(nameof(vm.ConfirmPassword), "Xác nhận mật khẩu không khớp.");
            if (!ModelState.IsValid) return View(vm);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _context.AdminUsers.FirstOrDefaultAsync(a => a.Username == username);
            if (user == null) return NotFound();

            bool ok = string.Equals(user.PasswordHash ?? "", vm.CurrentPassword, StringComparison.Ordinal);
            if (!ok)
            {
                try { ok = PasswordHasher.Verify(vm.CurrentPassword, user.PasswordHash); }
                catch { ok = false; }
            }
            if (!ok)
            {
                ModelState.AddModelError(nameof(vm.CurrentPassword), "Mật khẩu hiện tại không đúng.");
                return View(vm);
            }

            string newHash;
            try { newHash = PasswordHasher.Hash(vm.NewPassword); }
            catch
            {
                using var sha = SHA256.Create();
                newHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(vm.NewPassword)));
            }

            user.PasswordHash = newHash;
            await _context.SaveChangesAsync();

            ModelState.Clear();
            return View(new ChangePasswordVm { Message = "Cập nhật mật khẩu thành công." });
        }

        // ===================== FORGOT / RESET (EMAIL + DATA PROTECTION) =====================
        public class ForgotPasswordVm
        {
            public string Login { get; set; } = "";
            public bool Sent { get; set; }
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPassword() => View(new ForgotPasswordVm());

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVm vm)
        {
            vm.Login = vm.Login?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(vm.Login))
            {
                ModelState.AddModelError(nameof(vm.Login), "Vui lòng nhập tên đăng nhập hoặc email.");
                return View(vm);
            }

            var key = vm.Login.ToLower();
            var user = await _context.AdminUsers
                .FirstOrDefaultAsync(a =>
                    (a.Username != null && a.Username.ToLower() == key) ||
                    (a.Email != null && a.Email.ToLower() == key));

            // Không lộ thông tin tài khoản
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                vm.Sent = true;
                return View(vm);
            }

            var payload = new ResetPayload
            {
                UserId = user.AdminId,
                HashSnapshot = user.PasswordHash ?? "",
                ExpUtc = DateTime.UtcNow.AddMinutes(30)
            };
            var token = _protector.Protect(JsonSerializer.Serialize(payload));
            var link = Url.Action("ResetPassword", "Account", new { area = "Admin", token }, Request.Scheme)!;

            var subject = "Đặt lại mật khẩu tài khoản";
            var body =
$@"Xin chào {user.Username},

Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản của mình.
Nhấn vào liên kết sau để đặt lại (hết hạn lúc {payload.ExpUtc:HH:mm dd/MM/yyyy} UTC):

{link}

Nếu bạn không yêu cầu, vui lòng bỏ qua email này.";

            try
            {
                await _emailSender.SendAsync(user.Email!, user.Username ?? "Người dùng", subject, body);
                vm.Sent = true;
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể gửi email đặt lại mật khẩu (SendGrid).");

                // Ở môi trường Development: show lỗi chi tiết để debug ngay
                if (_env.EnvironmentName == "Development")
                {
                    ModelState.AddModelError("", "Lỗi gửi email: " + ex.Message);
                }
                else
                {
                    ModelState.AddModelError("", "Không thể gửi email đặt lại mật khẩu. Vui lòng kiểm tra cấu hình dịch vụ gửi mail.");
                }

                vm.Sent = false;
                return View(vm);
            }
        }

        private class ResetPayload
        {
            public int UserId { get; set; }
            public string HashSnapshot { get; set; } = "";
            public DateTime ExpUtc { get; set; }
        }

        public class ResetPasswordVm
        {
            public string Token { get; set; } = "";
            public string NewPassword { get; set; } = "";
            public string ConfirmPassword { get; set; } = "";
            public bool Done { get; set; }
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction(nameof(ForgotPassword));
            return View(new ResetPasswordVm { Token = token });
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVm vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Token))
                ModelState.AddModelError("", "Liên kết không hợp lệ.");

            if (string.IsNullOrWhiteSpace(vm.NewPassword) || vm.NewPassword.Length < 6)
                ModelState.AddModelError(nameof(vm.NewPassword), "Mật khẩu mới tối thiểu 6 ký tự.");

            if (vm.NewPassword != vm.ConfirmPassword)
                ModelState.AddModelError(nameof(vm.ConfirmPassword), "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid) return View(vm);

            ResetPayload payload;
            try
            {
                var json = _protector.Unprotect(vm.Token);
                payload = JsonSerializer.Deserialize<ResetPayload>(json)!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unprotect token failed");
                ModelState.AddModelError("", "Liên kết đặt lại không hợp lệ.");
                return View(vm);
            }

            if (payload == null || DateTime.UtcNow > payload.ExpUtc)
            {
                ModelState.AddModelError("", "Liên kết đã hết hạn.");
                return View(vm);
            }

            var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.AdminId == payload.UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return View(vm);
            }

            if (!string.Equals(user.PasswordHash ?? "", payload.HashSnapshot ?? "", StringComparison.Ordinal))
            {
                ModelState.AddModelError("", "Liên kết đã được sử dụng hoặc không còn hợp lệ.");
                return View(vm);
            }

            user.PasswordHash = HashPassword(vm.NewPassword);
            await _context.SaveChangesAsync();

            vm.Done = true;
            return View(vm);
        }

        // ===================== PROFILE / LOGOUT =====================
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account", new { area = "Admin" });

            var user = await _context.AdminUsers.FirstOrDefaultAsync(a => a.Username == username);
            if (user == null) return NotFound();

            return View(user);
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }

        [HttpGet, AllowAnonymous]
        public IActionResult AccessDenied() => View();

        // ===================== HELPERS =====================
        private static string HashPassword(string password)
        {
            try { return PasswordHasher.Hash(password); }
            catch
            {
                using var sha = SHA256.Create();
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToHexString(bytes);
            }
        }

        private static bool IsLikelyHash(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            return s.Length >= 32;
        }
    }
}
