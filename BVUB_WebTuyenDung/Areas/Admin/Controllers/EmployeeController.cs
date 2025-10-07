using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.Security;
using BVUB_WebTuyenDung.Areas.Admin.Utilities;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;
using BVUB_WebTuyenDung.Areas.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,1")] // chỉ Admin được quản lý Nhân viên
    public class EmployeeController : Controller
    {
        private readonly AdminDbContext _context;
        private readonly IAuditTrailService _audit;

        public EmployeeController(AdminDbContext context, IAuditTrailService audit)
        {
            _context = context;
            _audit = audit; // NEW
        }

        private string CurrentUser() => User?.Identity?.Name ?? "system";

        // Danh sách nhân viên
        public async Task<IActionResult> Index(string q)
        {
            var query = _context.AdminUsers.AsQueryable()
                                           .Where(u => u.Role != 1); // ẩn Admin

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                var qLower = q.ToLower();
                var isId = int.TryParse(q, out var id);
                query = query.Where(u =>
                    (isId && u.AdminId == id) ||
                    u.Username.ToLower().Contains(qLower) ||
                    (u.Email != null && u.Email.ToLower().Contains(qLower)));
            }

            var items = await query.OrderBy(u => u.AdminId).ToListAsync();
            ViewBag.q = q;
            return View(items);
        }

        // GET: Xem chi tiết (popup)
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var user = await _context.AdminUsers.AsNoTracking()
                             .FirstOrDefaultAsync(x => x.AdminId == id);
            if (user == null) return NotFound("Không tìm thấy nhân viên.");
            if (user.Role == 1) return Forbid(); // không cho xem Admin
            return PartialView("_EmployeeDetailsPartial", user);
        }

        // GET: Tạo mới nhân viên
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.PermOptions = StaffPermOptions.All;
            return View(new AdminUserCreateVm());
        }

        // So khớp mật khẩu admin
        private static bool VerifyAdminPassword(string inputPlain, string stored)
        {
            if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(inputPlain)) return false;

            // 1) DB lưu plain
            if (string.Equals(inputPlain, stored, StringComparison.Ordinal)) return true;

            // 2) DB lưu SHA-256 HEX
            try { return PasswordHasher.Verify(inputPlain, stored); } catch { return false; }
        }

        // POST: Tạo mới nhân viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminUserCreateVm vm)
        {
            ViewBag.PermOptions = StaffPermOptions.All;
            if (!ModelState.IsValid) return View(vm);

            // Xác nhận admin
            var myUsername = User.Identity?.Name;
            var me = await _context.AdminUsers.FirstOrDefaultAsync(x => x.Username == myUsername);
            if (me == null || me.Role == 0 || me.Role != 1) return Forbid();

            if (!VerifyAdminPassword(vm.AdminPassword, me.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu quản trị không đúng.");
                ViewBag.ToastError = "Mật khẩu quản trị không đúng.";
                return View(vm);
            }

            // Chống trùng
            if (await _context.AdminUsers.AnyAsync(u => u.Username == vm.Username.Trim()))
            { ModelState.AddModelError(nameof(vm.Username), "Tên đăng nhập đã tồn tại."); ViewBag.ToastError = "Tên đăng nhập đã tồn tại."; return View(vm); }

            if (await _context.AdminUsers.AnyAsync(u => u.Email == vm.Email.Trim()))
            { ModelState.AddModelError(nameof(vm.Email), "Email đã tồn tại."); ViewBag.ToastError = "Email đã tồn tại."; return View(vm); }

            // Tính bitmask menu
            int bits = 0;
            foreach (var v in (vm.SelectedPerms ?? new List<int>()).Distinct()) bits |= v;
            if (bits == 1 || bits == 0) bits = (int)StaffPerms.Dashboard;

            // Lưu mật khẩu plain
            var entity = new AdminUser
            {
                Username = vm.Username.Trim(),
                Email = vm.Email.Trim(),
                Role = bits,
                PasswordHash = vm.Password  
            };

            _context.AdminUsers.Add(entity);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUser(), $"Tạo mới nhân viên: {entity.Username} ({entity.Email})");

            TempData["ToastType"] = "success";
            TempData["ToastMsg"] = "Đã thêm nhân viên mới.";
            return RedirectToAction(nameof(Index));
        }

        // GET: 
        [HttpGet]
        public async Task<IActionResult> AssignRole(int id)
        {
            var user = await _context.AdminUsers.FirstOrDefaultAsync(x => x.AdminId == id);
            if (user == null) return NotFound();
            return View(user); 
        }

        // POST: Xóa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.AdminUsers.FirstOrDefaultAsync(x => x.AdminId == id);
            if (user == null) return Json(new { ok = false, message = "Không tìm thấy nhân viên." });
            if (user.Role == 1) return Forbid();

            _context.AdminUsers.Remove(user);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUser(), $"Xóa nhân viên: {user.Username} ({user.Email})");

            return Json(new { ok = true });
        }

        // EXPORT CSV
        [HttpGet]
        public async Task<FileResult> ExportCsv(string q)
        {
            var query = _context.AdminUsers.Where(u => u.Role != 1);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                if (int.TryParse(q, out var id))
                    query = query.Where(u => u.AdminId == id ||
                                             u.Username.ToLower().Contains(q) ||
                                             (u.Email ?? "").ToLower().Contains(q));
                else
                    query = query.Where(u => u.Username.ToLower().Contains(q) ||
                                             (u.Email ?? "").ToLower().Contains(q));
            }

            var data = await query.OrderBy(u => u.AdminId).ToListAsync();
            var sb = new StringBuilder().AppendLine("AdminId,Username,Email,Role");
            foreach (var u in data)
            {
                var roleLabel = u.Role == 1 ? "Quản trị viên" : "Nhân viên";
                sb.AppendLine($"{u.AdminId},{(u.Username ?? "").Replace(",", " ")},{(u.Email ?? "").Replace(",", " ")},{roleLabel}");
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", "employees.csv");
        }

        // GET: edit 
        [HttpGet]
        [Authorize(Roles = "Admin,1")]
        public async Task<IActionResult> Edit(int id)
        {
            var u = await _context.AdminUsers.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.AdminId == id);
            if (u == null) return NotFound("Không tìm thấy nhân viên.");

            // không cho sửa admin
            if (u.Role == 1) return Forbid();

            ViewBag.PermOptions = StaffPermOptions.All;

            // tách bitmask -> SelectedPerms
            var selected = new List<int>();
            foreach (var (val, _) in StaffPermOptions.All)
                if ((u.Role & val) == val) selected.Add(val);

            var vm = new AdminUserEditVm
            {
                AdminId = u.AdminId,
                Username = u.Username,
                Email = u.Email,
                SelectedPerms = selected
            };

            return View(vm);
        }

        // POST: edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,1")]
        public async Task<IActionResult> Edit(AdminUserEditVm vm)
        {
            ViewBag.PermOptions = StaffPermOptions.All;

            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.Remove(nameof(vm.Password));
                ModelState.Remove(nameof(vm.ConfirmPassword));
            }
            if (!ModelState.IsValid) return View(vm);

            // xác nhận admin đang đăng nhập
            var myUsername = User.Identity?.Name;
            var me = await _context.AdminUsers.FirstOrDefaultAsync(x => x.Username == myUsername);
            if (me == null || me.Role == 0) return Forbid(); 

            // verify password admin 
            bool verify = string.Equals(vm.AdminPassword, me.PasswordHash, StringComparison.Ordinal);
            if (!verify)
            {
                try { verify = BVUB_WebTuyenDung.Areas.Admin.Utilities.PasswordHasher.Verify(vm.AdminPassword, me.PasswordHash); }
                catch { verify = false; }
            }
            if (!verify)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu quản trị không đúng.");
                ViewBag.ToastError = "Mật khẩu quản trị không đúng.";
                return View(vm);
            }

            var u = await _context.AdminUsers.FirstOrDefaultAsync(x => x.AdminId == vm.AdminId);
            if (u == null) return NotFound("Không tìm thấy nhân viên.");

            if (u.Role == 1) return Forbid();

            // Unique: Username/Email 
            var newUser = vm.Username?.Trim();
            var newMail = vm.Email?.Trim();

            if (await _context.AdminUsers.AnyAsync(x => x.AdminId != u.AdminId && x.Username == newUser))
            {
                ModelState.AddModelError(nameof(vm.Username), "Tên đăng nhập đã tồn tại.");
                ViewBag.ToastError = "Tên đăng nhập đã tồn tại.";
                return View(vm);
            }
            if (!string.IsNullOrEmpty(newMail) &&
                await _context.AdminUsers.AnyAsync(x => x.AdminId != u.AdminId && x.Email == newMail))
            {
                ModelState.AddModelError(nameof(vm.Email), "Email đã tồn tại.");
                ViewBag.ToastError = "Email đã tồn tại.";
                return View(vm);
            }

            // Tính lại bitmask từ checkbox
            int bits = 0;
            foreach (var v in (vm.SelectedPerms ?? new List<int>()).Distinct())
                bits |= v;

            if (bits == 0 || bits == 1) bits = (int)StaffPerms.Dashboard;

            // Update
            u.Username = newUser;
            u.Email = newMail;
            u.Role = bits;

            // Đổi mật khẩu nếu nhập mới
            if (!string.IsNullOrWhiteSpace(vm.Password))
            {
                u.PasswordHash = vm.Password;
            }

            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUser(), $"Chỉnh sửa nhân viên ID={u.AdminId}, Username={u.Username}");

            TempData["ToastType"] = "success";
            TempData["ToastMsg"] = "Đã cập nhật nhân viên.";
            return RedirectToAction(nameof(Index));
        }
    }
}

