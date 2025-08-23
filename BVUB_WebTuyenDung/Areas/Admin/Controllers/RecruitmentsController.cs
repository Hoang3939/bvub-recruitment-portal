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

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class RecruitmentsController : Controller
    {
        private readonly AdminDbContext _ctx;
        private readonly IWebHostEnvironment _env;

        public RecruitmentsController(AdminDbContext ctx, IWebHostEnvironment env)
        {
            _ctx = ctx;
            _env = env;
        }

        // GET: Recruitments
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int? st)
        {
            var query = _ctx.ThongTinTuyenDungs.AsNoTracking().AsQueryable();

            if (st.HasValue && st.Value >= 1 && st.Value <= 4)
            {
                var status = (TrangThaiTuyenDung)st.Value;
                query = query.Where(x => x.TrangThai == status);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();

                if (TryParseVnDate(kw, out var d))
                {
                    query = query.Where(x => x.NgayDang == d || x.HanNopHoSo == d);
                }
                else
                {
                    query = query.Where(x =>
                        x.TieuDe.Contains(kw) ||
                        x.NoiDung.Contains(kw) ||
                        x.LoaiTuyenDung.Contains(kw));
                }
            }

            var items = await query
                .OrderBy(x => x.TrangThai)           // 1→4
                .ThenByDescending(x => x.NgayDang)   // mới nhất trước
                .ToListAsync();

            return View(items); 
        }

        // GET: Xem chi tiết
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var item = await _ctx.ThongTinTuyenDungs.AsNoTracking()
                         .FirstOrDefaultAsync(x => x.TuyenDungId == id);
            if (item == null) return NotFound();

            return PartialView("_RecruitmentDetails", item);
        }

        // Create
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
        public async Task<IActionResult> Create(ThongTinTuyenDung m, IFormFile? file)
        {
            if (!IsAdmin()) return Forbid();

            if (file != null && file.Length > 0)
            {
                m.FileDinhKem = await SaveUploadAsync(file);
            }

            if (!ModelState.IsValid) return View(m);

            _ctx.ThongTinTuyenDungs.Add(m);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // EDIT 
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
        public async Task<IActionResult> Edit(int id,ThongTinTuyenDung m, IFormFile? file)
        {
            if (!IsAdmin()) return Forbid();
            if (id != m.TuyenDungId) return BadRequest();

            var entity = await _ctx.ThongTinTuyenDungs.FirstOrDefaultAsync(x => x.TuyenDungId == id);
            if (entity == null) return NotFound();

            if (!ModelState.IsValid) return View(m);

            entity.TieuDe = m.TieuDe;
            entity.NoiDung = m.NoiDung;
            entity.NgayDang = m.NgayDang;
            entity.HanNopHoSo = m.HanNopHoSo;
            entity.LoaiTuyenDung = m.LoaiTuyenDung;
            entity.TrangThai = m.TrangThai;

            if (file != null && file.Length > 0)
            {
                entity.FileDinhKem = await SaveUploadAsync(file);
            }

            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helpers
        private static bool TryParseVnDate(string input, out DateTime date)
        {
            return DateTime.TryParseExact(
                input,
                "dd/MM/yyyy",
                CultureInfo.GetCultureInfo("vi-VN"),
                DateTimeStyles.None,
                out date);
        }

        private async Task<string> SaveUploadAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "recruitments");
            Directory.CreateDirectory(folder);

            var safeName = Path.GetFileName(file.FileName);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeName}";
            var fullPath = Path.Combine(folder, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            // Trả về đường dẫn web
            return $"/uploads/recruitments/{fileName}";
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin") || User.IsInRole("1") ||
                   User.Claims.Any(c =>
                       c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" &&
                       (string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase) || c.Value == "1"));
        }

        // POST: Xóa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin()) return Forbid();

            var m = await _ctx.ThongTinTuyenDungs.FindAsync(id);
            if (m == null) return Json(new { ok = false, message = "Không tìm thấy bản ghi." });

            // Xóa file đính kèm trong wwwroot nếu là file của module
            try
            {
                if (!string.IsNullOrWhiteSpace(m.FileDinhKem) &&
                    m.FileDinhKem.StartsWith("/uploads/recruitments/", StringComparison.OrdinalIgnoreCase))
                {
                    var p = Path.Combine(_env.WebRootPath, m.FileDinhKem.TrimStart('/')
                                              .Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
                }
            }
            catch {}

            _ctx.ThongTinTuyenDungs.Remove(m);
            await _ctx.SaveChangesAsync();
            return Json(new { ok = true });
        }

    }
}
