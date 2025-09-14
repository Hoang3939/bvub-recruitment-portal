using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Services;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using M = BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class DepartmentsController : Controller
    {
        private readonly AdminDbContext _ctx;
        private readonly IAuditTrailService _audit;

        public DepartmentsController(AdminDbContext ctx, IAuditTrailService audit)
        {
            _ctx = ctx;
            _audit = audit;
        }

        // ===== Helpers: chuẩn hoá tên để so trùng
        private static string NormalizeDeptName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // 1) Trim + gộp khoảng trắng
            var s = string.Join(' ', input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            // 2) Bỏ dấu tiếng Việt
            var normalized = s.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            s = new string(chars).Normalize(NormalizationForm.FormC);
            // 3) Không phân biệt hoa/thường
            return s.ToUpperInvariant();
        }

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

        // ====== CREATE: chỉ tạo Tên/Loại/Trạng thái 
        [HttpGet]
        public IActionResult Create() => View(new DepartmentFrmVm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentFrmVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var name = (vm.Ten ?? "").Trim();

            // Kiểm tra trùng nhanh theo DB (không phân biệt hoa/thường)
            bool existed = await _ctx.DanhMucKhoaPhongs
                .AnyAsync(k => k.Ten.ToLower() == name.ToLower());

            // Kiểm tra trùng (bỏ dấu, gộp khoảng trắng, không phân biệt hoa/thường)
            if (!existed)
            {
                var normalizedNew = NormalizeDeptName(name);
                var allNames = await _ctx.DanhMucKhoaPhongs.Select(x => x.Ten).ToListAsync();
                existed = allNames.Any(n => NormalizeDeptName(n) == normalizedNew);
            }

            if (existed)
            {
                ModelState.AddModelError(nameof(vm.Ten), "Tên khoa/phòng đã tồn tại.");
                TempData["ToastError"] = "Khoa/Phòng này đã tồn tại (tên bị trùng).";
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

                var userName = User?.Identity?.Name ?? "system";
                await _audit.LogAsync(userName, $"Thêm khoa/phòng '{kp.Ten}' (ID={kp.KhoaPhongId})");

                TempData["ToastSuccess"] = "Đã thêm khoa/phòng.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ToastError"] = "Có lỗi khi lưu dữ liệu.";
                return View(vm);
            }
        }

        // ====== EditInfo: SỬA tên/loại/trạng thái
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

            // Kiểm tra trùng nhanh theo DB (không phân biệt hoa/thường, loại trừ chính nó)
            bool existed = await _ctx.DanhMucKhoaPhongs
                .AnyAsync(k => k.KhoaPhongId != id && k.Ten.ToLower() == name.ToLower());

            // Kiểm tra trùng (bỏ dấu, gộp khoảng trắng)
            if (!existed)
            {
                var normalizedNew = NormalizeDeptName(name);
                var otherNames = await _ctx.DanhMucKhoaPhongs
                    .Where(k => k.KhoaPhongId != id)
                    .Select(x => x.Ten)
                    .ToListAsync();

                existed = otherNames.Any(n => NormalizeDeptName(n) == normalizedNew);
            }

            if (existed)
            {
                ModelState.AddModelError(nameof(vm.Ten), "Tên khoa/phòng đã tồn tại.");
                TempData["ToastError"] = "Tên khoa/phòng đã tồn tại. Vui lòng nhập tên khác.";
                return View(vm);
            }

            try
            {
                kp.Ten = name;
                kp.Loai = vm.Loai;
                kp.TamNgung = vm.TamNgung;
                await _ctx.SaveChangesAsync();

                var userName = User?.Identity?.Name ?? "system";
                await _audit.LogAsync(userName, $"Sửa thông tin khoa/phòng ID={id} thành '{kp.Ten}'");

                TempData["ToastSuccess"] = "Đã cập nhật khoa/phòng.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ToastError"] = "Có lỗi khi cập nhật.";
                return View(vm);
            }
        }

        // ====== Edit (Thiết lập vị trí)
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

            if (!existed)
            {
                var normalizedNew = NormalizeDeptName(name);
                var otherNames = await _ctx.DanhMucKhoaPhongs
                    .Where(k => k.KhoaPhongId != id)
                    .Select(x => x.Ten)
                    .ToListAsync();
                existed = otherNames.Any(n => NormalizeDeptName(n) == normalizedNew);
            }

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

                var userName = User?.Identity?.Name ?? "system";
                await _audit.LogAsync(userName, $"Cập nhật khoa/phòng ID={id}, tên '{kp.Ten}' (chọn vị trí)");

                TempData["ToastSuccess"] = "Đã cập nhật.";
                return RedirectToAction(nameof(Index), new { tab = "map" });
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

                var userName = User?.Identity?.Name ?? "system";
                await _audit.LogAsync(userName, $"Xóa khoa/phòng '{e.Ten}' (ID={id})");
                
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
