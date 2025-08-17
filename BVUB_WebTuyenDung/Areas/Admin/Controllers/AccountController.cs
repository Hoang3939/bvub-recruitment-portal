using Microsoft.AspNetCore.Mvc;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

[Area("Admin")]
public class AccountController : Controller
{
    private readonly AdminDbContext _context;

    public AccountController(AdminDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        var admin = _context.AdminUsers
            .FirstOrDefault(a => a.Username == username && a.PasswordHash == password);

        if (admin == null)
        {
            ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
            return View();
        }

        var roleName = admin.Role == 1 ? "Admin" : "Staff";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, admin.Username ?? string.Empty),
            new Claim(ClaimTypes.Role, roleName)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity)
        );

        return RedirectToAction("Index", "HomeAdmin", new { area = "Admin" });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account", new { area = "Admin" });
    }

    [Authorize]
    public IActionResult Profile()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return RedirectToAction("Login", "Account", new { area = "Admin" });

        var admin = _context.AdminUsers.FirstOrDefault(a => a.Username == username);
        if (admin == null) return NotFound();

        return View(admin);
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
