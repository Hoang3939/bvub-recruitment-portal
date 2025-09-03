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

        // Index: q (text), st (status: null all, 0 active, 1 suspended)
        public async Task<IActionResult> Index(string q, int? st)
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

            if (st.HasValue)
            {
                query = query.Where(k => k.TamNgung == st.Value);
            }

            ViewBag.q = q;
            ViewBag.st = st;

            // Sort ID tăng dần
            var items = await query.OrderBy(k => k.KhoaPhongId).ToListAsync();
            return View(items);
        }

        // ====== Tạo VM cho form map vị trí (vẫn giữ để trang Edit map dùng)
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

        // ====== CREATE: chỉ tạo Tên/Loại/Trạng thái (không map vị trí)
        [HttpGet]
        public IActionResult Create() => View(new DepartmentFrmVm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentFrmVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var name = (vm.Ten ?? "").Trim();

            // Chống trùng tên (không phân biệt hoa/thường)
            bool existed = await _ctx.DanhMucKhoaPhongs
                .AnyAsync(k => k.Ten.ToLower() == name.ToLower());
            if (existed)
            {
                ModelState.AddModelError(nameof(vm.Ten), "Tên khoa/phòng đã tồn tại.");
                TempData["ToastError"] = "Khoa/Phòng này đã tồn tại (đang sử dụng hoặc đã có trong hệ thống).";
                return View(vm);
            }

            try
            {
                var kp = new M.DanhMucKhoaPhong
                {
                    Ten = name,
                    Loai = vm.Loai,
                    TamNgung = vm.TamNgung
                };
                _ctx.DanhMucKhoaPhongs.Add(kp);
                await _ctx.SaveChangesAsync();

                TempData["ToastSuccess"] = "Đã thêm khoa/phòng.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ToastError"] = "Có lỗi khi lưu dữ liệu.";
                return View(vm);
            }
        }

        // ====== EditInfo: SỬA CHỈ tên/loại/trạng thái (không map vị trí)
        [HttpGet]
        public async Task<IActionResult> EditInfo(int id)
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
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInfo(int id, DepartmentFrmVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var kp = await _ctx.DanhMucKhoaPhongs.FindAsync(id);
            if (kp == null) return NotFound();

            var name = (vm.Ten ?? "").Trim();
            bool existed = await _ctx.DanhMucKhoaPhongs
                .AnyAsync(k => k.KhoaPhongId != id && k.Ten.ToLower() == name.ToLower());
            if (existed)
            {
                ModelState.AddModelError(nameof(vm.Ten), "Tên khoa/phòng đã tồn tại.");
                TempData["ToastError"] = "Tên khoa/phòng đã tồn tại.";
                return View(vm);
            }

            try
            {
                kp.Ten = name;
                kp.Loai = vm.Loai;
                kp.TamNgung = vm.TamNgung;
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

        // ====== Edit (GIỮ NGUYÊN) – trang MAP vị trí
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

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DepartmentFrmVm vm)
        {
            vm = await BuildVm(vm, id);
            if (!ModelState.IsValid) return View(vm);

            var kp = await _ctx.DanhMucKhoaPhongs.FindAsync(id);
            if (kp == null) return NotFound();

            var name = (vm.Ten ?? "").Trim();
            bool existed = await _ctx.DanhMucKhoaPhongs
                .AnyAsync(k => k.KhoaPhongId != id && k.Ten.ToLower() == name.ToLower());
            if (existed)
            {
                ModelState.AddModelError(nameof(vm.Ten), "Tên khoa/phòng đã tồn tại.");
                TempData["ToastError"] = "Tên khoa/phòng đã tồn tại.";
                return View(vm);
            }

            // Validate SelectedViTriIds
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
                kp.Ten = name;
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

        // Delete
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

        // Details partial
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var e = await _ctx.DanhMucKhoaPhongs
                .Include(k => k.KhoaPhongViTris).ThenInclude(kvt => kvt.ViTri)
                .FirstOrDefaultAsync(k => k.KhoaPhongId == id);

            if (e == null) return NotFound();
            return PartialView("_DepartmentDetailsPartial", e);
        }

        // (Giữ nguyên 2 API GetPositionsByTitle / GetPositionsByIds nếu còn dùng ở Edit map)
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
    }
}
