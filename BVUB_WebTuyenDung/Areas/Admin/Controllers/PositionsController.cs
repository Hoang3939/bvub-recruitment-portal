using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;
using M = BVUB_WebTuyenDung.Areas.Admin.Models;

using BVUB_WebTuyenDung.Areas.Admin.Services;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class PositionsController : Controller
    {
        private readonly AdminDbContext _ctx;
        private readonly IAuditTrailService _audit;
        public PositionsController(AdminDbContext ctx, IAuditTrailService audit)
        {
            _ctx = ctx;
            _audit = audit;
        }

        private string CurrentUser() => User?.Identity?.Name ?? "unknown";

        // Index + filter trạng thái
        public async Task<IActionResult> Index(string q, int? st)
        {
            var query = _ctx.DanhMucViTriDuTuyens
                            .Include(v => v.ChucDanh)
                            .AsQueryable();

            // lọc trạng thái (0: sử dụng, 1: tạm ngưng)
            if (st.HasValue && (st.Value == 0 || st.Value == 1))
            {
                query = query.Where(v => v.TamNgung == st.Value);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(v =>
                    EF.Functions.Like(v.TenViTri, $"%{q}%") ||
                    EF.Functions.Like(v.ChucDanh.TenChucDanh, $"%{q}%"));
            }

            ViewBag.q = q;
            ViewBag.st = st;

            // Sort theo MÃ (ViTriId) tăng dần
            var items = await query.OrderBy(v => v.ViTriId).ToListAsync();
            return View(items);
        }

        // Build VM
        private async Task<PositionFormVm> BuildVm(PositionFormVm vm = null)
        {
            var chucDanhs = await _ctx.DanhMucChucDanhDuTuyens
                .OrderBy(x => x.TenChucDanh)
                .Select(x => new SelectListItem { Value = x.ChucDanhId.ToString(), Text = x.TenChucDanh })
                .ToListAsync();

            vm ??= new PositionFormVm();
            vm.AllChucDanhs = chucDanhs;
            return vm;
        }

        // Create
        [HttpGet]
        public async Task<IActionResult> Create() => View(await BuildVm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PositionFormVm vm)
        {
            if (!ModelState.IsValid) return View(await BuildVm(vm));

            var e = new M.DanhMucViTriDuTuyen
            {
                TenViTri = vm.TenViTri,
                ChucDanhId = vm.ChucDanhId,
                TamNgung = vm.TamNgung
            };
            _ctx.DanhMucViTriDuTuyens.Add(e);
            await _ctx.SaveChangesAsync();

            // Mirror trạng thái lên chức danh nếu chức danh chỉ có 1 vị trí
            var count = await _ctx.DanhMucViTriDuTuyens.CountAsync(x => x.ChucDanhId == e.ChucDanhId);
            if (count == 1)
            {
                var title = await _ctx.DanhMucChucDanhDuTuyens.FindAsync(e.ChucDanhId);
                if (title != null)
                {
                    title.TamNgung = e.TamNgung;
                    await _ctx.SaveChangesAsync();
                }
            }

            await _audit.LogAsync(CurrentUser(), $"Tạo vị trí mới ID={e.ViTriId}, Ten='{e.TenViTri}', ChucDanhId={e.ChucDanhId}, TamNgung={e.TamNgung}");

            TempData["ToastSuccess"] = "Đã thêm vị trí mới.";
            return RedirectToAction(nameof(Index));
        }

        // Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var e = await _ctx.DanhMucViTriDuTuyens.FindAsync(id);
            if (e == null) return NotFound();

            var vm = new PositionFormVm
            {
                ViTriId = e.ViTriId,
                TenViTri = e.TenViTri,
                ChucDanhId = e.ChucDanhId,
                TamNgung = e.TamNgung
            };
            return View(await BuildVm(vm));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PositionFormVm vm)
        {
            if (!ModelState.IsValid) return View(await BuildVm(vm));

            var e = await _ctx.DanhMucViTriDuTuyens.FindAsync(id);
            if (e == null) return NotFound();

            var oldTen = e.TenViTri;
            var oldCdId = e.ChucDanhId;
            var oldSt = e.TamNgung;

            e.TenViTri = vm.TenViTri;
            e.ChucDanhId = vm.ChucDanhId;
            e.TamNgung = vm.TamNgung;

            // Mirror trạng thái
            var count = await _ctx.DanhMucViTriDuTuyens.CountAsync(x => x.ChucDanhId == e.ChucDanhId);
            if (count == 1)
            {
                var title = await _ctx.DanhMucChucDanhDuTuyens.FindAsync(e.ChucDanhId);
                if (title != null) title.TamNgung = e.TamNgung;
            }

            await _ctx.SaveChangesAsync();

            await _audit.LogAsync(
                CurrentUser(),
                $"Cập nhật Vị trí ID={e.ViTriId}: Ten '{oldTen}' -> '{e.TenViTri}', ChucDanhId {oldCdId} -> {e.ChucDanhId}, TamNgung {oldSt} -> {e.TamNgung}"
            );

            TempData["ToastSuccess"] = "Đã cập nhật vị trí.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Xóa
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _ctx.DanhMucViTriDuTuyens.FindAsync(id);
            if (e != null)
            {
                var ten = e.TenViTri; var cd = e.ChucDanhId;
                _ctx.DanhMucViTriDuTuyens.Remove(e);
                await _ctx.SaveChangesAsync();

                await _audit.LogAsync(CurrentUser(), $"Xóa Vị trí ID={id}, Ten='{ten}', ChucDanhId={cd}");
            }

            TempData["ToastSuccess"] = "Đã xóa vị trí.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Xem chi tiết (popup)
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var e = await _ctx.DanhMucViTriDuTuyens
                              .Include(v => v.ChucDanh)
                              .FirstOrDefaultAsync(v => v.ViTriId == id);
            if (e == null) return NotFound();
            return PartialView("_PositionDetailsPartial", e);
        }
    }
}
