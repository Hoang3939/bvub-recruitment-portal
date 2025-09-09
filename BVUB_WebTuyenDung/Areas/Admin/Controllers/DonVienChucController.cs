// Areas/Admin/Controllers/DonVienChucController.cs
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Collections.Generic;
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
    public class DonVienChucController : Controller
    {
        private readonly AdminDbContext _context;
        public DonVienChucController(AdminDbContext context) => _context = context;

        // Map trạng thái
        private static (string Label, string Css) MapStatus(int? stt) => stt switch
        {
            1 => ("Chờ xử lý", "pending"),
            2 => ("Đã duyệt", "approved"),
            3 => ("Đã hủy", "cancelled"),
            _ => ("", "")
        };

        private bool IsAjax() =>
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

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

                var label = labelMap.TryGetValue(p.Name, out var lbl) ? lbl : p.Name;
                var val = p.GetValue(obj);
                yield return (label, FormatValue(p.Name, val));
            }
        }

        // GET: Chi tiết
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var don = await _context.DonVienChucs
                .AsNoTracking()
                .Include(d => d.ViTriDuTuyen)
                .Include(d => d.ChucDanhDuTuyen)
                .Include(d => d.KhoaPhong)
                .FirstOrDefaultAsync(x => x.VienChucId == id);

            if (don == null) return NotFound();

            var uv = await _context.UngViens.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UngVienId == don.UngVienId);
            if (uv == null) return NotFound();

            // Văn bằng theo Ứng viên
            var vbs = await _context.VanBangs.AsNoTracking()
                .Where(v => v.UngVienId == uv.UngVienId)
                .OrderByDescending(v => v.NgayCap ?? DateTime.MinValue)
                .ThenBy(v => v.VanBangId)
                .ToListAsync();

            var (label, css) = MapStatus(don.TrangThai);

            var vm = new DonVienChucDetailsVm
            {
                Don = don,
                UngVien = uv,
                TrangThaiLabel = label,
                TrangThaiClass = css,
                VanBangs = vbs
            };
            return View(vm);
        }

        // GET: Xuất Word đơn VC
        [HttpGet]
        public async Task<IActionResult> ExportWord(int id)
        {
            var don = await _context.DonVienChucs.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.VienChucId == id);
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

            var donLabel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["VienChucId"] = "Mã đơn",
                ["UngVienId"] = "Mã ứng viên",
                ["ViTriDuTuyenId"] = "Vị trí dự tuyển",
                ["ChucDanhDuTuyenId"] = "Chức danh dự tuyển",
                ["KhoaPhongId"] = "Khoa phòng",
                ["DanToc"] = "Dân tộc",
                ["TonGiao"] = "Tôn giáo",
                ["QueQuan"] = "Quê quán",
                ["HoKhau"] = "Hộ khẩu",
                ["ChieuCao"] = "Chiều cao",
                ["CanNang"] = "Cân nặng",
                ["TrinhDoVanHoa"] = "Trình độ văn hóa",
                ["LoaiHinhDaoTao"] = "Loại hình đào tạo",
                ["DoiTuongUuTien"] = "Đối tượng ưu tiên",
                ["NgayNop"] = "Ngày nộp",
                ["MaTraCuu"] = "Mã tra cứu"
            };

            var (statusLabel, statusCss) = MapStatus(don.TrangThai);

            var sb = new StringBuilder();
            // <-- HTML Word -->
            sb.Append(@"
<html><head><meta charset='utf-8'/><title>");
            sb.Append($"DonVienChuc_{don.VienChucId}");
            sb.Append(@"</title>
<style>
body { font-family:'Times New Roman', serif; font-size:12pt }
h1 { text-align:center; margin:0 0 16px 0; font-weight:800 }
h2 { margin:18px 0 8px 0; font-weight:800 }
table { width:100%; border-collapse:collapse; margin-bottom:12px }
td { border:1px solid #999; padding:6px 8px; vertical-align:top }
td.label { width:35%; font-weight:bold; background:#f5f5f5 }
.badge { display:inline-block; padding:4px 8px; border-radius:12px; color:#fff }
.badge.pending { background:#f0ad4e }
.badge.approved { background:#5cb85c }
.badge.cancelled { background:#dc3545 }
</style></head><body>
<h1>ĐƠN VIÊN CHỨC</h1>

<h2>I. Thông tin ứng viên</h2>
<table>");

            foreach (var r in BuildRows(uv, uvLabel))
                sb.Append($"<tr><td class='label'>{H(r.Label)}</td><td>{H(r.Value)}</td></tr>");

            sb.Append("</table>");
            sb.Append("<h2>II. Thông tin đơn viên chức</h2><table>");

            foreach (var r in BuildRows(don, donLabel, "TrangThai"))
                sb.Append($"<tr><td class='label'>{H(r.Label)}</td><td>{H(r.Value)}</td></tr>");

            sb.Append($"<tr><td class='label'>Trạng thái</td><td><span class='badge {statusCss}'>{H(statusLabel)}</span></td></tr>");
            sb.Append("</table></body></html>");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "application/msword", $"DonVienChuc_{don.VienChucId}.doc");
        }

        // ================== EDIT ==================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var don = await _context.DonVienChucs
                .Include(d => d.UngVien)
                .Include(d => d.KhoaPhong)
                .Include(d => d.ChucDanhDuTuyen)
                .Include(d => d.ViTriDuTuyen)
                .FirstOrDefaultAsync(d => d.VienChucId == id);
            if (don == null) return NotFound();

            // Văn bằng theo Ứng viên
            var vbs = await _context.VanBangs.AsNoTracking()
                .Where(v => v.UngVienId == don.UngVienId)
                .OrderByDescending(v => v.NgayCap ?? DateTime.MinValue)
                .ThenBy(v => v.VanBangId)
                .ToListAsync();

            var vm = new UngTuyenVienChucViewModel
            {
                UngVien = new UngVien
                {
                    UngVienId = don.UngVien.UngVienId,
                    Email = don.UngVien.Email,
                    HoTen = don.UngVien.HoTen,
                    GioiTinh = don.UngVien.GioiTinh,
                    NgaySinh = don.UngVien.NgaySinh,
                    SoDienThoai = don.UngVien.SoDienThoai,
                    CCCD = don.UngVien.CCCD,
                    NgayCapCCCD = don.UngVien.NgayCapCCCD,
                    NoiCapCCCD = don.UngVien.NoiCapCCCD,
                    DiaChiThuongTru = don.UngVien.DiaChiThuongTru,
                    DiaChiCuTru = don.UngVien.DiaChiCuTru,
                    MaSoThue = don.UngVien.MaSoThue,
                    SoTaiKhoan = don.UngVien.SoTaiKhoan,
                    TinhTrangSucKhoe = don.UngVien.TinhTrangSucKhoe,
                    TrinhDoChuyenMon = don.UngVien.TrinhDoChuyenMon,
                    NgayUngTuyen = don.UngVien.NgayUngTuyen
                },
                DonVienChuc = don,
                VanBangs = vbs
            };

            // Dropdowns
            ViewBag.ChucDanhList = await _context.DanhMucChucDanhDuTuyens
                .Select(x => new SelectListItem { Value = x.ChucDanhId.ToString(), Text = x.TenChucDanh })
                .ToListAsync();

            ViewBag.ViTriList = new List<SelectListItem> {
                new SelectListItem {
                    Value = don.ViTriDuTuyenId.ToString(),
                    Text  = don.ViTriDuTuyen?.TenViTri
                }
            };

            ViewBag.LoaiVanBangOptions = new List<string> { "Đại học", "Cao đẳng", "Trung cấp", "Sau đại học", "Khác" };

            return View("Edit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UngTuyenVienChucViewModel vm)
        {
            // -- Bỏ validate các navigation/field hệ thống --
            string[] ignoreKeys =
            {
                "DonVienChuc.UngVien",
                "DonVienChuc.KhoaPhong",
                "DonVienChuc.ViTriDuTuyen",
                "DonVienChuc.ChucDanhDuTuyen",
                "DonVienChuc.MaTraCuu",
                "DonVienChuc.NgayNop",
                "DonVienChuc.TrangThai"
            };
            foreach (var k in ignoreKeys) ModelState.Remove(k);

            if (vm.VanBangs != null)
            {
                for (int i = 0; i < vm.VanBangs.Count; i++)
                {
                    ModelState.Remove($"VanBangs[{i}].UngVien");
                    ModelState.Remove($"VanBangs[{i}].UngVienId");
                }
            }

            if (!ModelState.IsValid)
            {
                if (IsAjax())
                    return BadRequest(new
                    {
                        ok = false,
                        message = "Dữ liệu chưa hợp lệ.",
                        errors = string.Join(" | ", ModelState.Where(x => x.Value.Errors.Any()).Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}"))
                    });
                return View("Edit", vm);
            }

            var don = await _context.DonVienChucs
                .Include(d => d.UngVien)
                .FirstOrDefaultAsync(d => d.VienChucId == vm.DonVienChuc.VienChucId);

            if (don == null)
            {
                if (IsAjax()) return NotFound(new { ok = false, message = "Không tìm thấy đơn viên chức." });
                return NotFound();
            }

            try
            {
                // -- Cập nhật UngVien --
                var src = vm.UngVien;
                var dst = don.UngVien;
                dst.Email = src.Email?.Trim();
                dst.HoTen = src.HoTen?.Trim();
                dst.GioiTinh = src.GioiTinh;
                dst.NgaySinh = src.NgaySinh;
                dst.SoDienThoai = src.SoDienThoai?.Trim();
                dst.CCCD = src.CCCD?.Trim();
                dst.NgayCapCCCD = src.NgayCapCCCD;
                dst.NoiCapCCCD = src.NoiCapCCCD?.Trim();
                dst.DiaChiThuongTru = src.DiaChiThuongTru?.Trim();
                dst.DiaChiCuTru = src.DiaChiCuTru?.Trim();
                dst.MaSoThue = src.MaSoThue?.Trim();
                dst.SoTaiKhoan = src.SoTaiKhoan?.Trim();
                dst.TinhTrangSucKhoe = src.TinhTrangSucKhoe?.Trim();
                dst.TrinhDoChuyenMon = src.TrinhDoChuyenMon?.Trim();
                // Không đụng NgayUngTuyen

                // -- Cập nhật DonVienChuc --
                don.ViTriDuTuyenId = vm.DonVienChuc.ViTriDuTuyenId;
                don.ChucDanhDuTuyenId = vm.DonVienChuc.ChucDanhDuTuyenId;
                don.KhoaPhongId = vm.DonVienChuc.KhoaPhongId;
                don.DanToc = vm.DonVienChuc.DanToc?.Trim();
                don.TonGiao = vm.DonVienChuc.TonGiao?.Trim();
                don.QueQuan = vm.DonVienChuc.QueQuan?.Trim();
                don.HoKhau = vm.DonVienChuc.HoKhau?.Trim();
                don.ChieuCao = vm.DonVienChuc.ChieuCao;
                don.CanNang = vm.DonVienChuc.CanNang;
                don.TrinhDoVanHoa = vm.DonVienChuc.TrinhDoVanHoa?.Trim();
                don.LoaiHinhDaoTao = vm.DonVienChuc.LoaiHinhDaoTao?.Trim();
                don.DoiTuongUuTien = vm.DonVienChuc.DoiTuongUuTien?.Trim();
                // Không chỉnh: MaTraCuu, NgayNop, TrangThai, UngVienId

                // -- Đồng bộ Văn bằng --
                var uid = don.UngVienId;
                var oldVbs = await _context.VanBangs.Where(v => v.UngVienId == uid).ToListAsync();
                _context.VanBangs.RemoveRange(oldVbs);

                var newVbs = vm.VanBangs ?? new List<VanBang>();
                foreach (var vb in newVbs)
                {
                    vb.VanBangId = 0;
                    vb.UngVienId = uid; 
                    vb.TenCoSo = vb.TenCoSo?.Trim();
                    vb.SoHieu = vb.SoHieu?.Trim();
                    vb.ChuyenNganhDaoTao = vb.ChuyenNganhDaoTao?.Trim();
                    vb.NganhDaoTao = vb.NganhDaoTao?.Trim();
                    vb.HinhThucDaoTao = vb.HinhThucDaoTao?.Trim();
                    vb.XepLoai = vb.XepLoai?.Trim();
                    vb.LoaiVanBang = vb.LoaiVanBang?.Trim();
                }
                await _context.VanBangs.AddRangeAsync(newVbs);

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
                return View("Edit", vm);
            }
            catch (Exception ex)
            {
                if (IsAjax())
                    return StatusCode(500, new { ok = false, message = "Có lỗi khi lưu.", detail = ex.Message });
                ModelState.AddModelError(string.Empty, "Có lỗi khi lưu: " + ex.Message);
                return View("Edit", vm);
            }
        }

        // ================= AJAX cascades =================
        [HttpGet]
        public async Task<IActionResult> GetViTriByChucDanh(int chucDanhId)
        {
            var list = await _context.DanhMucViTriDuTuyens.AsNoTracking()
                .Where(v => v.TamNgung == 0 && v.ChucDanhId == chucDanhId)
                .Select(v => new { v.ViTriId, v.TenViTri })
                .ToListAsync();

            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetKhoaPhongByViTri(int viTriId)
        {
            if (viTriId <= 0) return Json(Array.Empty<object>());

            var list = await _context.DanhMucViTriDuTuyens
                .AsNoTracking()
                .Where(v => v.ViTriId == viTriId)
                .SelectMany(v => v.KhoaPhongViTris)
                .Where(link => link.KhoaPhong.TamNgung == 0)
                .Select(link => new
                {
                    link.KhoaPhong.KhoaPhongId,
                    TenKhoaPhong = link.KhoaPhong.Ten
                })
                .Distinct()
                .ToListAsync();

            return Json(list);
        }
    }
}
