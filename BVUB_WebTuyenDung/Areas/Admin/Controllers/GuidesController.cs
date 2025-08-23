using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BVUB_WebTuyenDung.Areas.Admin.Models;


namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class GuidesController : Controller
    {
        private readonly AdminDbContext _ctx;
        public GuidesController(AdminDbContext ctx) => _ctx = ctx;

        // Hướng dẫn đăng ký
        public async Task<IActionResult> Index(string? loai)
        {
            var q = _ctx.HuongDans.AsQueryable();
            if (!string.IsNullOrWhiteSpace(loai)) q = q.Where(x => x.LoaiHuongDan == loai);
            var list = await q.OrderByDescending(x => x.NgayCapNhat).ToListAsync();
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
        public async Task<IActionResult> Create(HuongDanDangKy m)
        {
            if (!ModelState.IsValid) return View(m);
            m.NgayCapNhat = DateTime.Today;
            _ctx.Add(m);
            await _ctx.SaveChangesAsync();
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
        public async Task<IActionResult> Edit(int id, HuongDanDangKy m)
        {
            if (id != m.HuongDanId) return NotFound();
            if (!ModelState.IsValid) return View(m);
            m.NgayCapNhat = DateTime.Today;
            _ctx.Update(m);
            await _ctx.SaveChangesAsync();
            TempData["ok"] = "Đã cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Xem chi tiết (popup)
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var m = await _ctx.HuongDans.FirstOrDefaultAsync(x => x.HuongDanId == id);
            if (m == null) return NotFound("Không tìm thấy hướng dẫn.");
            return PartialView("_GuideDetails", m);
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
            return Json(new { ok = true });
        }
    }
}