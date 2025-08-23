using BVUB_WebTuyenDung.Data;
using BVUB_WebTuyenDung.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BVUB_WebTuyenDung.Controllers
{
    public class DanhMucKhoaPhongController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DanhMucKhoaPhongController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.DanhMucKhoaPhong.ToListAsync());
        public IActionResult Create() => View();
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DanhMucKhoaPhong model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Add(model); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var model = await _context.DanhMucKhoaPhong.FindAsync(id);
            return model == null ? NotFound() : View(model);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DanhMucKhoaPhong model)
        {
            if (id != model.KhoaPhongId) return NotFound();
            if (!ModelState.IsValid) return View(model);
            _context.Update(model); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var model = await _context.DanhMucKhoaPhong.FindAsync(id);
            return model == null ? NotFound() : View(model);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await _context.DanhMucKhoaPhong.FindAsync(id);
            _context.DanhMucKhoaPhong.Remove(model); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var model = await _context.DanhMucKhoaPhong.FindAsync(id);
            return model == null ? NotFound() : View(model);
        }
    }
}
