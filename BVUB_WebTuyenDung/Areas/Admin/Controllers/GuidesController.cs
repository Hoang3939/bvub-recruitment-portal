using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class GuidesController : Controller
    {
        private readonly AdminDbContext _ctx;
        private readonly IWebHostEnvironment _env;
        private readonly IAuditTrailService _audit;

        public GuidesController(AdminDbContext ctx, IWebHostEnvironment env, IAuditTrailService audit)
        {
            _ctx = ctx;
            _env = env;
            _audit = audit;
        }

        private string CurrentUser() => User?.Identity?.Name ?? "unknown";

        // Hướng dẫn đăng ký
        public async Task<IActionResult> Index(string? loai)
        {
            var q = _ctx.HuongDans.AsQueryable();
            if (!string.IsNullOrWhiteSpace(loai)) q = q.Where(x => x.LoaiHuongDan == loai);

            var list = await q.OrderByDescending(x => x.NgayCapNhat).ToListAsync();

            ViewBag.HasFilter = !string.IsNullOrWhiteSpace(loai);

            return View(list);
        }

        // Tạo mới 
        public IActionResult Create() => View(new HuongDanDangKy
        {
            NgayCapNhat = DateTime.Today,
            LoaiHuongDan = "Người lao động"
        });

        // POST: Tạo mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HuongDanDangKy m, IFormFile? file)
        {
            if (!ModelState.IsValid) return View(m);

            // <-- Upload file hướng dẫn -->
            if (file != null && file.Length > 0)
            {
                m.FileHuongDan = await SaveGuideFileAsync(file);
            }

            m.NgayCapNhat = DateTime.Today;
            _ctx.Add(m);
            await _ctx.SaveChangesAsync();

            await _audit.LogAsync(CurrentUser(), $"Thêm hướng dẫn ID={m.HuongDanId}, Tiêu đề='{m.TieuDe}'");

            TempData["ok"] = "Đã thêm hướng dẫn.";
            return RedirectToAction(nameof(Index));
        }

        // Edit
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _ctx.HuongDans.FirstOrDefaultAsync(x => x.HuongDanId == id);
            if (m == null) return NotFound();
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HuongDanDangKy m, IFormFile? file)
        {
            if (id != m.HuongDanId) return NotFound();
            if (!ModelState.IsValid) return View(m);

            var entity = await _ctx.HuongDans.FirstOrDefaultAsync(x => x.HuongDanId == id);
            if (entity == null) return NotFound();

            entity.TieuDe = m.TieuDe;
            entity.LoaiHuongDan = m.LoaiHuongDan;
            entity.NoiDung = m.NoiDung;
            entity.NgayCapNhat = DateTime.Today;

            // <-- Upload/thay thế file hướng dẫn -->
            if (file != null && file.Length > 0)
            {
                entity.FileHuongDan = await SaveGuideFileAsync(file);
            }

            await _ctx.SaveChangesAsync();

            await _audit.LogAsync(CurrentUser(), $"Chỉnh sửa hướng dẫn ID={entity.HuongDanId}, Tiêu đề='{entity.TieuDe}'");

            TempData["ok"] = "Đã cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Xem chi tiết (popup)
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var m = await _ctx.HuongDans.FirstOrDefaultAsync(x => x.HuongDanId == id);
            if (m == null) return Content("<div>Không tìm thấy hướng dẫn.</div>", "text/html; charset=utf-8");
            return PartialView("~/Areas/Admin/Views/Guides/_GuideDetails.cshtml", m);
        }

        // Xoá bằng AJAX trong popup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var m = await _ctx.HuongDans.FirstOrDefaultAsync(x => x.HuongDanId == id);
            if (m == null)
                return Json(new { ok = false, message = "Không tìm thấy hướng dẫn." });

            _ctx.HuongDans.Remove(m);
            await _ctx.SaveChangesAsync();

            await _audit.LogAsync(CurrentUser(), $"Xóa hướng dẫn ID={m.HuongDanId}, Tiêu đề='{m.TieuDe}'");

            return Json(new { ok = true });
        }

        // ==== Helpers ====
        private async Task<string> SaveGuideFileAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "guides");
            Directory.CreateDirectory(folder);

            var safeName = Path.GetFileName(file.FileName);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeName}";
            var fullPath = Path.Combine(folder, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }
            // Trả về path để bind ra UI
            return $"/uploads/guides/{fileName}";
        }
    }
}
