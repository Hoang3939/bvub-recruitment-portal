using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class HopDongNguoiLaoDongController : Controller
    {
        private readonly AdminDbContext _context;
        public HopDongNguoiLaoDongController(AdminDbContext context) => _context = context;

        // Helpers
        private static (string Label, string Css) MapStatus(int? stt) => stt switch
        {
            1 => ("Chờ xử lý", "pending"),
            2 => ("Đã duyệt", "approved"),
            3 => ("Đã hủy", "cancelled"),
            _ => ("", "")
        };
        private bool IsAjax() =>
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

        private string JoinModelErrors()
        {
            var errs = ModelState.Where(kv => kv.Value.Errors.Count > 0)
                                 .Select(kv =>
                                 {
                                     var msg = string.Join(", ", kv.Value.Errors.Select(e => e.ErrorMessage));
                                     return string.IsNullOrWhiteSpace(kv.Key) ? msg : $"{kv.Key}: {msg}";
                                 });
            return string.Join(" | ", errs);
        }

        private async System.Threading.Tasks.Task LoadSelects()
        {
            ViewBag.KhoaPhongList = new SelectList(
                await _context.DanhMucKhoaPhongs.AsNoTracking()
                          .Where(x => x.TamNgung == 0).ToListAsync(),
                "KhoaPhongId", "Ten");
        }
        private static string H(object? s) => WebUtility.HtmlEncode(s?.ToString() ?? "");

        private static string FormatValue(string propName, object? v)
        {
            if (propName.Equals("GioiTinh", StringComparison.OrdinalIgnoreCase))
            {
                int? g = v is int gi ? gi
                        : (v is string gs && int.TryParse(gs, out var t) ? t : (int?)null);
                return g == 0 ? "Nam" : g == 1 ? "Nữ" : "";
            }
            if (v is DateTime dt) return dt.ToString("dd/MM/yyyy");
            if (v is DateTimeOffset dto) return dto.ToString("dd/MM/yyyy");
            return v?.ToString() ?? "";
        }

        private static IEnumerable<(string Label, string Value)> BuildRows(
            object obj, IDictionary<string, string> labelMap, params string[] exclude)
        {
            var skip = new HashSet<string>(exclude ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            foreach (var p in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (skip.Contains(p.Name)) continue;

                var pt = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                bool simple =
                    pt.IsPrimitive || pt.IsEnum || pt == typeof(string) || pt == typeof(decimal) ||
                    pt == typeof(DateTime) || pt == typeof(DateTimeOffset) || pt == typeof(Guid);

                if (!simple) continue;

                var display = p.GetCustomAttributes(typeof(DisplayAttribute), true)
                               .OfType<DisplayAttribute>()
                               .FirstOrDefault()?.Name;

                var label = display ?? (labelMap.TryGetValue(p.Name, out var lbl) ? lbl : p.Name);
                var val = p.GetValue(obj);
                yield return (label, FormatValue(p.Name, val));
            }
        }

        private static void ApplyUngVien(UngVien s, UngVien d)
        {
            d.Email = s.Email?.Trim();
            d.HoTen = s.HoTen?.Trim();
            d.GioiTinh = s.GioiTinh;
            d.NgaySinh = s.NgaySinh;
            d.SoDienThoai = s.SoDienThoai?.Trim();
            d.CCCD = s.CCCD?.Trim();
            d.NgayCapCCCD = s.NgayCapCCCD;
            d.NoiCapCCCD = s.NoiCapCCCD?.Trim();
            d.DiaChiThuongTru = s.DiaChiThuongTru?.Trim();
            d.DiaChiCuTru = s.DiaChiCuTru?.Trim();
            d.MaSoThue = s.MaSoThue?.Trim();
            d.SoTaiKhoan = s.SoTaiKhoan?.Trim();
            d.TinhTrangSucKhoe = s.TinhTrangSucKhoe?.Trim();
            d.TrinhDoChuyenMon = s.TrinhDoChuyenMon?.Trim();
            // d.NgayUngTuyen       = keep
        }

        private async Task LoadKhoaPhongSelect(int? selectedId = null)
        {
            var data = await _context.DanhMucKhoaPhongs
                .AsNoTracking()
                .Where(x => x.TamNgung == 0)
                .ToListAsync();

            ViewBag.KhoaPhongList = new SelectList(data, "KhoaPhongId", "Ten", selectedId);
        }

        // GET: Xem chi tiết đơn
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var hd = await _context.HopDongNguoiLaoDongs.AsNoTracking()
                .Include(h => h.UngVien)
                .Include(h => h.KhoaPhongCongTac)
                .FirstOrDefaultAsync(h => h.HopDongId == id);

            if (hd == null) return NotFound();

            var (label, css) = MapStatus(hd.TrangThai);
            var vm = new HopDongNguoiLaoDongDetailsVm
            {
                Don = hd,
                UngVien = hd.UngVien!,
                TrangThaiLabel = label,
                TrangThaiClass = css
            };
            return View(vm);
        }

        // GET: Xuất word
        [HttpGet]
        public async Task<IActionResult> ExportWord(int id)
        {
            var don = await _context.HopDongNguoiLaoDongs.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.HopDongId == id);
            if (don == null) return NotFound();

            var uv = await _context.UngViens.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.UngVienId == don.UngVienId);
            if (uv == null) return NotFound();

            var uvLabel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["UngVienId"] = "Mã ứng viên",
                ["HoTen"] = "Họ và tên",
                ["NgaySinh"] = "Ngày sinh",
                ["GioiTinh"] = "Giới tính",
                ["SoDienThoai"] = "Số điện thoại",
                ["Email"] = "Email",
                ["CCCD"] = "CCCD",
                ["NgayCapCCCD"] = "Ngày cấp CCCD",
                ["NoiCapCCCD"] = "Nơi cấp CCCD",
                ["DiaChiThuongTru"] = "Địa chỉ thường trú",
                ["DiaChiCuTru"] = "Địa chỉ cư trú",
                ["MaSoThue"] = "Mã số thuế",
                ["SoTaiKhoan"] = "Số tài khoản",
                ["TinhTrangSucKhoe"] = "Tình trạng sức khỏe",
                ["TrinhDoChuyenMon"] = "Trình độ chuyên môn",
                ["NgayUngTuyen"] = "Ngày ứng tuyển"
            };

            var hdLabel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["HopDongId"] = "Mã hợp đồng",
                ["UngVienId"] = "Mã ứng viên",
                ["KhoaPhongCongTacId"] = "Khoa phòng công tác",
                ["NoiSinh"] = "Nơi sinh",
                ["ChuyenNganhDaoTao"] = "Chuyên ngành đào tạo",
                ["NamTotNghiep"] = "Năm tốt nghiệp",
                ["TrinhDoTinHoc"] = "Trình độ tin học",
                ["TrinhDoNgoaiNgu"] = "Trình độ ngoại ngữ",
                ["ChungChiHanhNghe"] = "Chứng chỉ hành nghề",
                ["NgheNghiepTruocTuyenDung"] = "Nghề nghiệp trước tuyển dụng",
                ["MaTraCuu"] = "Mã tra cứu",
                ["NgayNop"] = "Ngày nộp"
            };

            var (statusLabel, statusCss) = MapStatus(don.TrangThai);

            var sb = new StringBuilder();
            sb.Append($@"
<html><head><meta charset='utf-8'/><title>HopDong_{don.HopDongId}</title>
<style>
body {{ font-family:'Times New Roman', serif; font-size:12pt }}
h1 {{ text-align:center; margin:0 0 16px 0; font-weight:800 }}
h2 {{ margin:18px 0 8px 0; font-weight:800 }}
table {{ width:100%; border-collapse:collapse; margin-bottom:12px }}
td {{ border:1px solid #999; padding:6px 8px; vertical-align:top }}
td.label {{ width:35%; font-weight:bold; background:#f5f5f5 }}
.badge {{ display:inline-block; padding:4px 8px; border-radius:12px; color:#fff }}
.badge.pending {{ background:#f0ad4e }}
.badge.approved {{ background:#5cb85c }}
.badge.cancelled {{ background:#dc3545 }}
</style></head><body>
<h1>HỢP ĐỒNG NGƯỜI LAO ĐỘNG</h1>

<h2>I. Thông tin ứng viên</h2>
<table>");

            foreach (var r in BuildRows(uv, uvLabel))
                sb.Append($"<tr><td class='label'>{H(r.Label)}</td><td>{H(r.Value)}</td></tr>");

            sb.Append("</table><h2>II. Thông tin hợp đồng</h2><table>");

            foreach (var r in BuildRows(don, hdLabel, "TrangThai"))
                sb.Append($"<tr><td class='label'>{H(r.Label)}</td><td>{H(r.Value)}</td></tr>");

            sb.Append($"<tr><td class='label'>Trạng thái</td><td><span class='badge {statusCss}'>{H(statusLabel)}</span></td></tr>");
            sb.Append("</table></body></html>");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "application/msword", $"HopDong_{don.HopDongId}.doc");
        }

        // GET: edit
        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> Edit(int id) // id = HopDongId
        {
            var hd = await _context.HopDongNguoiLaoDongs
                        .Include(h => h.UngVien)
                        .Include(h => h.KhoaPhongCongTac)
                        .FirstOrDefaultAsync(h => h.HopDongId == id);

            if (hd == null) return NotFound();

            var vm = new UngTuyenNguoiLaoDongViewModel
            {
                UngVien = hd.UngVien,
                HopDongNguoiLaoDong = hd
            };

            await LoadSelects();
            return View(vm);
        }

        // POST: edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> Edit(UngTuyenNguoiLaoDongViewModel vm)
        {
            // Bỏ validate các navigation / field không sửa
            string[] ignore = {
                "HopDongNguoiLaoDong.UngVien",
                "HopDongNguoiLaoDong.KhoaPhongCongTac",
                "HopDongNguoiLaoDong.MaTraCuu",
                "HopDongNguoiLaoDong.NgayNop",
                "HopDongNguoiLaoDong.TrangThai",
                "HopDongNguoiLaoDong.Loai"
            };
            foreach (var k in ignore) ModelState.Remove(k);

            if (!ModelState.IsValid)
            {
                if (IsAjax())
                    return BadRequest(new { ok = false, message = "Dữ liệu chưa hợp lệ.", errors = JoinModelErrors() });

                await LoadSelects();
                return View(vm);
            }

            var hd = await _context.HopDongNguoiLaoDongs
                        .Include(h => h.UngVien)
                        .FirstOrDefaultAsync(h => h.HopDongId == vm.HopDongNguoiLaoDong.HopDongId);

            if (hd == null)
            {
                if (IsAjax())
                    return NotFound(new { ok = false, message = "Không tìm thấy hợp đồng." });
                return NotFound();
            }

            try
            {
                // Cập nhật Ứng viên
                var s = vm.UngVien;
                var d = hd.UngVien;
                d.Email = s.Email?.Trim();
                d.HoTen = s.HoTen?.Trim();
                d.GioiTinh = s.GioiTinh;
                d.NgaySinh = s.NgaySinh;
                d.SoDienThoai = s.SoDienThoai?.Trim();
                d.CCCD = s.CCCD?.Trim();
                d.NgayCapCCCD = s.NgayCapCCCD;
                d.NoiCapCCCD = s.NoiCapCCCD?.Trim();
                d.DiaChiThuongTru = s.DiaChiThuongTru?.Trim();
                d.DiaChiCuTru = s.DiaChiCuTru?.Trim();
                d.MaSoThue = s.MaSoThue?.Trim();
                d.SoTaiKhoan = s.SoTaiKhoan?.Trim();
                d.TinhTrangSucKhoe = s.TinhTrangSucKhoe?.Trim();
                d.TrinhDoChuyenMon = s.TrinhDoChuyenMon?.Trim();

                // Cập nhật HĐ (chỉ các field cho phép sửa)
                var src = vm.HopDongNguoiLaoDong;
                hd.KhoaPhongCongTacId = src.KhoaPhongCongTacId;
                hd.NoiSinh = src.NoiSinh?.Trim();
                hd.ChuyenNganhDaoTao = src.ChuyenNganhDaoTao?.Trim();
                hd.NamTotNghiep = src.NamTotNghiep?.Trim();
                hd.TrinhDoTinHoc = src.TrinhDoTinHoc?.Trim();
                hd.TrinhDoNgoaiNgu = src.TrinhDoNgoaiNgu?.Trim();
                hd.ChungChiHanhNghe = src.ChungChiHanhNghe?.Trim();
                hd.NgheNghiepTruocTuyenDung = src.NgheNghiepTruocTuyenDung?.Trim();
                // KHÔNG đụng tới: hd.Loai, hd.MaTraCuu, hd.NgayNop, hd.TrangThai, hd.UngVienId

                await _context.SaveChangesAsync();

                if (IsAjax())
                    return Ok(new { ok = true, message = "Đã lưu thành công." });

                TempData["SavedOk"] = true;
                return RedirectToAction("Index", "Candidates", new { area = "Admin" });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (IsAjax())
                    return StatusCode(409, new { ok = false, message = "Xung đột dữ liệu. Vui lòng tải lại trang.", detail = ex.Message });

                ModelState.AddModelError(string.Empty, "Xung đột dữ liệu. Vui lòng tải lại trang.");
                await LoadSelects();
                return View(vm);
            }
            catch (Exception ex)
            {
                if (IsAjax())
                    return StatusCode(500, new { ok = false, message = "Có lỗi khi lưu.", detail = ex.Message });

                ModelState.AddModelError(string.Empty, "Có lỗi khi lưu: " + ex.Message);
                await LoadSelects();
                return View(vm);
            }
        }
    }
}
