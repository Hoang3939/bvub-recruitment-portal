using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;
using M = BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class DepartmentsController : Controller
    {
        private readonly AdminDbContext _ctx;
        public DepartmentsController(AdminDbContext ctx) => _ctx = ctx;

        // Danh sách khoa phòng
        public async Task<IActionResult> Index(string q)
        {
            var query = _ctx.DanhMucKhoaPhongs
                .Include(k => k.KhoaPhongViTris).ThenInclude(kvt => kvt.ViTri)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(k =>
                    EF.Functions.Like(k.Ten, "%" + q + "%") ||
                    EF.Functions.Like(k.Loai, "%" + q + "%"));
            }
            ViewBag.q = q;

            var items = await query.OrderBy(k => k.Ten).ToListAsync();
            return View(items);
        }

        // ---- Build VM (đổ dropdown Chức danh; nếu edit -> nạp SelectedViTriIds)
        private async Task<DepartmentFrmVm> BuildVm(DepartmentFrmVm vm = null, int? kpId = null)
        {
            var titles = await _ctx.DanhMucChucDanhDuTuyens
                .OrderBy(t => t.TenChucDanh)
                .Select(t => new SelectListItem { Value = t.ChucDanhId.ToString(), Text = t.TenChucDanh })
                .ToListAsync();

            vm ??= new DepartmentFrmVm();
            vm.AllChucDanhs = titles;

            if (kpId.HasValue && (vm.SelectedViTriIds == null || vm.SelectedViTriIds.Count == 0))
            {
                vm.SelectedViTriIds = await _ctx.KhoaPhongViTris
                    .Where(x => x.KhoaPhongId == kpId.Value)
                    .Select(x => x.ViTriId)
                    .ToListAsync();
            }
            return vm;
        }

        // GET: Create
        [HttpGet]
        public async Task<IActionResult> Create() => View(await BuildVm(new DepartmentFrmVm()));

        // API: Lấy vị trí theo CHỨC DANH
        [HttpGet]
        public async Task<IActionResult> GetPositionsByTitle(int chucDanhId)
        {
            var items = await _ctx.DanhMucViTriDuTuyens
                .Where(v => v.ChucDanhId == chucDanhId && v.TamNgung == 0)
                .OrderBy(v => v.TenViTri)
                .Select(v => new { value = v.ViTriId, text = v.TenViTri })
                .ToListAsync();

            return Json(new { items });
        }

        // API: Lấy tên vị trí theo danh sách id (hydrate phần "Đã chọn")
        [HttpGet]
        public async Task<IActionResult> GetPositionsByIds(string ids)
        {
            var idList = (ids ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                .Where(n => n.HasValue).Select(n => n.Value).ToList();

            if (idList.Count == 0) return Json(new { items = new object[0] });

            var items = await _ctx.DanhMucViTriDuTuyens
                .Where(v => idList.Contains(v.ViTriId))
                .Select(v => new { value = v.ViTriId, text = v.TenViTri })
                .ToListAsync();

            return Json(new { items });
        }

        // POST: Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentFrmVm vm)
        {
            vm = await BuildVm(vm); // nạp lại dropdown khi lỗi
            if (!ModelState.IsValid) return View(vm);

            var validIds = await _ctx.DanhMucViTriDuTuyens
                .Where(v => vm.SelectedViTriIds.Contains(v.ViTriId))
                .Select(v => v.ViTriId)
                .ToListAsync();

            var invalid = vm.SelectedViTriIds.Except(validIds).ToList();
            if (invalid.Count > 0)
            {
                ModelState.AddModelError(string.Empty, "Có vị trí không hợp lệ.");
                return View(vm);
            }

            try
            {
                var kp = new M.DanhMucKhoaPhong
                {
                    Ten = vm.Ten,
                    Loai = vm.Loai,
                    TamNgung = vm.TamNgung
                };
                _ctx.DanhMucKhoaPhongs.Add(kp);
                await _ctx.SaveChangesAsync();

                if (vm.SelectedViTriIds?.Any() == true)
                {
                    foreach (var vid in vm.SelectedViTriIds.Distinct())
                        _ctx.KhoaPhongViTris.Add(new M.KhoaPhongViTri
                        {
                            KhoaPhongId = kp.KhoaPhongId,
                            ViTriId = vid
                        });
                    await _ctx.SaveChangesAsync();
                }

                TempData["ToastSuccess"] = "Đã thêm khoa/phòng.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ToastError"] = "Có lỗi khi lưu dữ liệu.";
                return View(vm);
            }
        }

        // GET: Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var kp = await _ctx.DanhMucKhoaPhongs.FindAsync(id);
            if (kp == null) return NotFound();

            var vm = new DepartmentFrmVm
            {
                KhoaPhongId = kp.KhoaPhongId,
                Ten = kp.Ten,
                Loai = kp.Loai,
                TamNgung = kp.TamNgung
            };
            return View(await BuildVm(vm, kp.KhoaPhongId));
        }

        // POST: Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DepartmentFrmVm vm)
        {
            vm = await BuildVm(vm, id);
            if (!ModelState.IsValid) return View(vm);

            var kp = await _ctx.DanhMucKhoaPhongs.FindAsync(id);
            if (kp == null) return NotFound();

            // Validate vị trí
            var validIds = await _ctx.DanhMucViTriDuTuyens
                .Where(v => vm.SelectedViTriIds.Contains(v.ViTriId))
                .Select(v => v.ViTriId)
                .ToListAsync();
            var invalid = vm.SelectedViTriIds.Except(validIds).ToList();
            if (invalid.Count > 0)
            {
                ModelState.AddModelError(string.Empty, "Có vị trí không hợp lệ.");
                return View(vm);
            }

            try
            {
                kp.Ten = vm.Ten;
                kp.Loai = vm.Loai;
                kp.TamNgung = vm.TamNgung;

                // Đồng bộ bảng nối
                var current = await _ctx.KhoaPhongViTris
                    .Where(x => x.KhoaPhongId == id)
                    .Select(x => x.ViTriId)
                    .ToListAsync();

                var selected = vm.SelectedViTriIds?.Distinct().ToList() ?? new List<int>();

                var toAdd = selected.Except(current).ToList();
                var toRemove = current.Except(selected).ToList();

                if (toAdd.Count > 0)
                {
                    foreach (var vid in toAdd)
                        _ctx.KhoaPhongViTris.Add(new M.KhoaPhongViTri { KhoaPhongId = id, ViTriId = vid });
                }

                if (toRemove.Count > 0)
                {
                    var rm = await _ctx.KhoaPhongViTris
                        .Where(x => x.KhoaPhongId == id && toRemove.Contains(x.ViTriId))
                        .ToListAsync();
                    _ctx.KhoaPhongViTris.RemoveRange(rm);
                }

                await _ctx.SaveChangesAsync();
                TempData["ToastSuccess"] = "Đã cập nhật khoa/phòng.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ToastError"] = "Có lỗi khi cập nhật.";
                return View(vm);
            }
        }

        // POST: Delete
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _ctx.DanhMucKhoaPhongs.FindAsync(id);
            if (e != null)
            {
                _ctx.DanhMucKhoaPhongs.Remove(e); 
                await _ctx.SaveChangesAsync();
                TempData["ToastSuccess"] = "Đã xóa khoa/phòng.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Xem chi tiết (popup)
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var e = await _ctx.DanhMucKhoaPhongs
                .Include(k => k.KhoaPhongViTris).ThenInclude(kvt => kvt.ViTri)
                .FirstOrDefaultAsync(k => k.KhoaPhongId == id);

            if (e == null) return NotFound();
            return PartialView("_DepartmentDetailsPartial", e);
        }
    }
}
