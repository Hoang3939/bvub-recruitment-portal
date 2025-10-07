using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.Services;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class RecruitmentsController : Controller
    {
        private readonly AdminDbContext _ctx;
        private readonly IWebHostEnvironment _env;
        private readonly IAuditTrailService _audit;
        public RecruitmentsController(AdminDbContext ctx, IWebHostEnvironment env, IAuditTrailService audit)
        {
            _ctx = ctx; _env = env; _audit = audit;
        }

        private string CurrentUser() => User?.Identity?.Name ?? "unknown";

        // ===== Danh sách =====
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int? st)
        {
            var query = _ctx.ThongTinTuyenDungs.AsNoTracking().AsQueryable();

            if (st is >= 1 and <= 4)
            {
                var status = (TrangThaiTuyenDung)st.Value;
                query = query.Where(x => x.TrangThai == status);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                if (TryParseVnDate(kw, out var d))
                    query = query.Where(x => x.NgayDang == d || x.HanNopHoSo == d);
                else
                    query = query.Where(x => x.TieuDe.Contains(kw) || x.NoiDung.Contains(kw) || x.LoaiTuyenDung.Contains(kw));
            }

            var items = await query.OrderBy(x => x.TrangThai).ThenByDescending(x => x.NgayDang).ToListAsync();
            ViewBag.HasFilter = (!string.IsNullOrWhiteSpace(q)) || (st is >= 1 and <= 4);
            return View(items);
        }

        // ===== Chi tiết (modal) =====
        // GET
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            if (id <= 0)
                return Content("<div>Không tìm thấy dữ liệu.</div>", "text/html; charset=utf-8");

            var item = await _ctx.ThongTinTuyenDungs.AsNoTracking()
                           .FirstOrDefaultAsync(x => x.TuyenDungId == id);

            if (item == null)
                return Content("<div>Không tìm thấy dữ liệu.</div>", "text/html; charset=utf-8");

            // <-- Trỏ tuyệt đối để khỏi lệ thuộc casing/thư mục
            return PartialView("~/Areas/Admin/Views/Recruitments/_RecruitmentDetails.cshtml", item);
        }

        // POST 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DetailsPartialPost(int id)
        {
            if (id <= 0)
                return Content("<div>Không tìm thấy dữ liệu.</div>", "text/html; charset=utf-8");

            var item = await _ctx.ThongTinTuyenDungs.AsNoTracking()
                           .FirstOrDefaultAsync(x => x.TuyenDungId == id);

            if (item == null)
                return Content("<div>Không tìm thấy dữ liệu.</div>", "text/html; charset=utf-8");

            return PartialView("~/Areas/Admin/Views/Recruitments/_RecruitmentDetails.cshtml", item);
        }



        // ===== Create =====
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin()) return Forbid();
            var m = new ThongTinTuyenDung
            {
                NgayDang = DateTime.Today,
                HanNopHoSo = DateTime.Today.AddDays(30),
                TrangThai = TrangThaiTuyenDung.DangTuyen
            };
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThongTinTuyenDung m, IFormFile? file, IFormFile? image)
        {
            if (!IsAdmin()) return Forbid();
            if (file != null && file.Length > 0) m.FileDinhKem = await SaveUploadAsync(file);
            if (image != null && image.Length > 0) m.FileAnh = await SaveImageAsync(image);
            if (!ModelState.IsValid) return View(m);
            _ctx.ThongTinTuyenDungs.Add(m);
            await _ctx.SaveChangesAsync();

            await _audit.LogAsync(
                CurrentUser(),
                $"Tạo tuyển dụng ID={m.TuyenDungId}, TieuDe='{m.TieuDe}', Loai='{m.LoaiTuyenDung}', TrangThai={m.TrangThai}, NgayDang={m.NgayDang:dd/MM/yyyy}, HanNop={m.HanNopHoSo:dd/MM/yyyy}"
            );

            TempData["ToastMsg"] = "Đã thêm thông tin tuyển dụng.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // ===== Edit =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin()) return Forbid();
            var m = await _ctx.ThongTinTuyenDungs.FindAsync(id);
            if (m == null) return NotFound();
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ThongTinTuyenDung m, IFormFile? file, IFormFile? image)
        {
            if (!IsAdmin()) return Forbid();
            if (id != m.TuyenDungId) return BadRequest();

            var entity = await _ctx.ThongTinTuyenDungs.FirstOrDefaultAsync(x => x.TuyenDungId == id);
            if (entity == null) return NotFound();
            if (!ModelState.IsValid) return View(m);

            var oldTitle = entity.TieuDe;
            var oldLoai = entity.LoaiTuyenDung;
            var oldTrangThai = entity.TrangThai;
            var oldNgayDang = entity.NgayDang;
            var oldHanNop = entity.HanNopHoSo;

            entity.TieuDe = m.TieuDe;
            entity.NoiDung = m.NoiDung;
            entity.NgayDang = m.NgayDang;
            entity.HanNopHoSo = m.HanNopHoSo;
            entity.LoaiTuyenDung = m.LoaiTuyenDung;
            entity.TrangThai = m.TrangThai;
            if (file != null && file.Length > 0) entity.FileDinhKem = await SaveUploadAsync(file);
            if (image != null && image.Length > 0) entity.FileAnh = await SaveImageAsync(image);
            await _ctx.SaveChangesAsync();

            await _audit.LogAsync(
                CurrentUser(),
                $"Sửa tuyển dụng ID={entity.TuyenDungId}: " +
                $"TieuDe '{oldTitle}' -> '{entity.TieuDe}', " +
                $"Loai '{oldLoai}' -> '{entity.LoaiTuyenDung}', " +
                $"TrangThai {oldTrangThai} -> {entity.TrangThai}, " +
                $"NgayDang {oldNgayDang:dd/MM/yyyy} -> {entity.NgayDang:dd/MM/yyyy}, " +
                $"HanNop {oldHanNop:dd/MM/yyyy} -> {entity.HanNopHoSo:dd/MM/yyyy}"
            );

            TempData["ToastMsg"] = "Đã lưu thay đổi.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // ===== Delete =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin()) return Forbid();
            var m = await _ctx.ThongTinTuyenDungs.FindAsync(id);
            if (m == null) return Json(new { ok = false, message = "Không tìm thấy bản ghi." });

            try
            {
                if (!string.IsNullOrWhiteSpace(m.FileDinhKem) &&
                    m.FileDinhKem.StartsWith("/uploads/recruitments/", StringComparison.OrdinalIgnoreCase))
                {
                    var p = Path.Combine(_env.WebRootPath, m.FileDinhKem.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
                }
                if (!string.IsNullOrWhiteSpace(m.FileAnh) &&
                    m.FileAnh.StartsWith("/uploads/recruitments/", StringComparison.OrdinalIgnoreCase))
                {
                    var p2 = Path.Combine(_env.WebRootPath, m.FileAnh.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(p2)) System.IO.File.Delete(p2);
                }
            }
            catch { }

            _ctx.ThongTinTuyenDungs.Remove(m);
            await _ctx.SaveChangesAsync();

            await _audit.LogAsync(CurrentUser(), $"Xóa tuyển dụng ID={id}, TieuDe='{m.TieuDe}'");

            return Json(new { ok = true });
        }

        // ===== Helpers =====
        private static bool TryParseVnDate(string input, out DateTime date)
        {
            return DateTime.TryParseExact(input, "dd/MM/yyyy",
                CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out date);
        }

        private async Task<string> SaveUploadAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "recruitments");
            Directory.CreateDirectory(folder);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Path.GetFileName(file.FileName)}";
            using var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(fs);
            return $"/uploads/recruitments/{fileName}";
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "recruitments", "images");
            Directory.CreateDirectory(folder);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Path.GetFileName(image.FileName)}";
            using var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await image.CopyToAsync(fs);
            return $"/uploads/recruitments/images/{fileName}";
        }

        private bool IsAdmin() =>
            User.IsInRole("Admin") || User.IsInRole("1") ||
            User.Claims.Any(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" &&
                                 (string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase) || c.Value == "1"));
    }
}
