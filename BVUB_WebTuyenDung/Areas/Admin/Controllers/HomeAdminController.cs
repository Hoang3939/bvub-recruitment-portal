using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Area("Admin")]
[Authorize(Policy = "StaffAndAdmin")]
public class HomeAdminController : Controller
{
    public IActionResult Index()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);

        ViewBag.Username = username;
        ViewBag.Role = role;

        return View();
    }
}
