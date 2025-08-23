using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;
using M = BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class TitlesController : Controller
    {
        private readonly AdminDbContext _ctx;
        public TitlesController(AdminDbContext ctx) => _ctx = ctx;

        // Index
        public async Task<IActionResult> Index(string q)
        {
            var query = _ctx.DanhMucChucDanhDuTuyens.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                if (int.TryParse(q, out var id))
                {
                    query = query.Where(x =>
                        x.ChucDanhId == id ||
                        EF.Functions.Like(x.TenChucDanh, "%" + q + "%"));
                }
                else
                {
                    query = query.Where(x => EF.Functions.Like(x.TenChucDanh, "%" + q + "%"));
                }
            }

            ViewBag.q = q;

            var items = await query.OrderBy(x => x.ChucDanhId).ToListAsync();
            return View(items);
        }

        // Tạo mới
        [HttpGet]
        public IActionResult Create() => View(new TitleFormVm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TitleFormVm vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["ToastError"] = "Lưu thất bại. Vui lòng kiểm tra lại dữ liệu.";
                return View(vm);
            }

            try
            {
                _ctx.DanhMucChucDanhDuTuyens.Add(new M.DanhMucChucDanhDuTuyen
                {
                    TenChucDanh = vm.TenChucDanh,
                    TamNgung = vm.TamNgung
                });
                await _ctx.SaveChangesAsync();

                TempData["ToastSuccess"] = "Đã thêm chức danh mới.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ToastError"] = "Có lỗi khi lưu dữ liệu.";
                return View(vm);
            }
        }

        // Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var e = await _ctx.DanhMucChucDanhDuTuyens.FindAsync(id);
            if (e == null) return NotFound();

            return View(new TitleFormVm
            {
                ChucDanhId = e.ChucDanhId,
                TenChucDanh = e.TenChucDanh,
                TamNgung = e.TamNgung
            });
        }

        // Edit: cập nhật Title và ĐỒNG BỘ trạng thái xuống toàn bộ Positions thuộc Title đó
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TitleFormVm vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["ToastError"] = "Cập nhật thất bại. Vui lòng kiểm tra lại dữ liệu.";
                return View(vm);
            }

            var e = await _ctx.DanhMucChucDanhDuTuyens.FindAsync(id);
            if (e == null) return NotFound();

            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                e.TenChucDanh = vm.TenChucDanh;
                e.TamNgung = vm.TamNgung;
                await _ctx.SaveChangesAsync();

                // Đồng bộ tất cả Vị trí thuộc chức danh này
                await _ctx.DanhMucViTriDuTuyens
                    .Where(v => v.ChucDanhId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.TamNgung, vm.TamNgung));

                await tx.CommitAsync();

                TempData["ToastSuccess"] = "Đã cập nhật chức danh và đồng bộ trạng thái vị trí.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["ToastError"] = "Có lỗi khi cập nhật.";
                return View(vm);
            }
        }

        // Xóa
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _ctx.DanhMucChucDanhDuTuyens.FindAsync(id);
            if (e != null) _ctx.DanhMucChucDanhDuTuyens.Remove(e);
            await _ctx.SaveChangesAsync();

            TempData["ToastSuccess"] = "Đã xóa chức danh.";
            return RedirectToAction(nameof(Index));
        }

        // Toggle: bật/tắt nhanh Title và ĐỒNG BỘ toàn bộ Positions
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var e = await _ctx.DanhMucChucDanhDuTuyens.FindAsync(id);
            if (e == null) return NotFound();

            var newStatus = (e.TamNgung == 1) ? 0 : 1;

            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                e.TamNgung = newStatus;
                await _ctx.SaveChangesAsync();

                await _ctx.DanhMucViTriDuTuyens
                    .Where(v => v.ChucDanhId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.TamNgung, newStatus));

                await tx.CommitAsync();

                TempData["ToastSuccess"] = newStatus == 1
                    ? "Đã tạm ngưng chức danh và toàn bộ vị trí."
                    : "Đã mở lại chức danh và toàn bộ vị trí.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["ToastError"] = "Không đổi được trạng thái.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Partial cho popup chi tiết
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var e = await _ctx.DanhMucChucDanhDuTuyens
                              .FirstOrDefaultAsync(x => x.ChucDanhId == id);
            if (e == null) return NotFound();

            return PartialView("_TitleDetailsPartial", e);
        }
    }
}
