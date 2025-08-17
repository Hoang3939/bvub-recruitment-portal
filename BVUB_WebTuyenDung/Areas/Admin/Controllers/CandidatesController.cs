using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class CandidatesController : Controller
    {
        private readonly AdminDbContext _context;
        public CandidatesController(AdminDbContext context) => _context = context;

        // Danh sách: Mã ứng viên, Tên ứng viên, Loại đơn, Trạng thái
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // 1) Ứng viên (chỉ lấy cột cần dùng)
            var uvList = await _context.UngViens
                .AsNoTracking()
                .Select(uv => new { uv.UngVienId, uv.HoTen })
                .ToListAsync();

            // 2) Join AuditTrail với Đơn Viên Chức -> lấy tất cả rows rồi group/order in-memory
            var vcRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join dvc in _context.DonVienChuc.AsNoTracking() on a.DonId equals dvc.VienChucId
                where a.LoaiDon == "VienChuc"
                select new
                {
                    dvc.UngVienId,
                    a.AuditTrailId,
                    Date = (DateTime?)a.NgayCapNhatMoi ?? (DateTime?)a.NgayTao,
                    a.LoaiDon,
                    DonId = a.DonId   // VienChucId
                }
            ).ToListAsync();

            var vcAuditLatest = vcRows
                .GroupBy(x => x.UngVienId)
                .Select(g => g
                    .OrderByDescending(x => x.Date ?? DateTime.MinValue)
                    .ThenByDescending(x => x.AuditTrailId)
                    .First())
                .ToDictionary(x => x.UngVienId,
                              x => new { x.LoaiDon, x.DonId, x.AuditTrailId, Date = x.Date, Type = "VC" });

            // 3) Join AuditTrail với Hợp Đồng NLĐ -> lấy tất cả rows rồi group/order in-memory
            var hdRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join hd in _context.HopDongNguoiLaoDong.AsNoTracking() on a.DonId equals hd.HopDongId
                where a.LoaiDon == "NguoiLaoDong"
                select new
                {
                    hd.UngVienId,
                    a.AuditTrailId,
                    Date = (DateTime?)a.NgayCapNhatMoi ?? (DateTime?)a.NgayTao,
                    a.LoaiDon,
                    DonId = a.DonId   // HopDongId
                }
            ).ToListAsync();

            var hdAuditLatest = hdRows
                .GroupBy(x => x.UngVienId)
                .Select(g => g
                    .OrderByDescending(x => x.Date ?? DateTime.MinValue)
                    .ThenByDescending(x => x.AuditTrailId)
                    .First())
                .ToDictionary(x => x.UngVienId,
                              x => new { x.LoaiDon, x.DonId, x.AuditTrailId, Date = x.Date, Type = "HD" });

            // 4) Chọn bản ghi mới nhất giữa VC và HD cho từng Ứng viên
            var picks = new Dictionary<int, (string LoaiDon, string Type, int DonId)>();
            foreach (var uv in uvList)
            {
                vcAuditLatest.TryGetValue(uv.UngVienId, out var vc);
                hdAuditLatest.TryGetValue(uv.UngVienId, out var hd);

                if (vc == null && hd == null) continue;

                bool takeVC = false;
                if (hd == null) takeVC = true;
                else if (vc != null)
                {
                    var vcDate = vc.Date ?? DateTime.MinValue;
                    var hdDate = hd.Date ?? DateTime.MinValue;

                    if (vcDate > hdDate) takeVC = true;
                    else if (vcDate == hdDate && vc.AuditTrailId > hd.AuditTrailId) takeVC = true;
                }

                if (takeVC)
                    picks[uv.UngVienId] = (vc!.LoaiDon ?? "Viên chức", "VC", vc.DonId);
                else
                    picks[uv.UngVienId] = (hd!.LoaiDon ?? "Hợp đồng lao động", "HD", hd.DonId);
            }

            // 5) Lấy trạng thái theo loại đơn đã chọn (Dictionary<int, int?>)
            var vcIds = picks.Where(p => p.Value.Type == "VC").Select(p => p.Value.DonId).Distinct().ToList();
            var hdIds = picks.Where(p => p.Value.Type == "HD").Select(p => p.Value.DonId).Distinct().ToList();

            var vcStatusDict = new Dictionary<int, int?>();
            if (vcIds.Count > 0)
            {
                vcStatusDict = await _context.DonVienChuc.AsNoTracking()
                    .Where(x => vcIds.Contains(x.VienChucId))
                    .Select(x => new { x.VienChucId, TrangThai = (int?)x.TrangThai })
                    .ToDictionaryAsync(x => x.VienChucId, x => x.TrangThai);
            }

            var hdStatusDict = new Dictionary<int, int?>();
            if (hdIds.Count > 0)
            {
                hdStatusDict = await _context.HopDongNguoiLaoDong.AsNoTracking()
                    .Where(x => hdIds.Contains(x.HopDongId))
                    .Select(x => new { x.HopDongId, TrangThai = (int?)x.TrangThai })
                    .ToDictionaryAsync(x => x.HopDongId, x => x.TrangThai);
            }

            // 6) Map số -> nhãn + CSS
            (string Label, string Css) MapStatus(int? stt) => stt switch
            {
                0 => ("Chờ xử lý", "pending"),
                1 => ("Đã duyệt", "approved"),
                2 => ("Đã hủy", "cancelled"),
                _ => (stt?.ToString() ?? "", "")
            };

            // 7) Build ViewModel cho View
            var vms = new List<CandidateListItemVm>(uvList.Count);
            foreach (var uv in uvList)
            {
                string loaiDon = "-";
                string label = "";
                string css = "";

                if (picks.TryGetValue(uv.UngVienId, out var pick))
                {
                    loaiDon = pick.LoaiDon;

                    if (pick.Type == "VC" && vcStatusDict.TryGetValue(pick.DonId, out var sttVC))
                        (label, css) = MapStatus(sttVC);
                    else if (pick.Type == "HD" && hdStatusDict.TryGetValue(pick.DonId, out var sttHD))
                        (label, css) = MapStatus(sttHD);
                }

                vms.Add(new CandidateListItemVm
                {
                    UngVienId = uv.UngVienId,
                    HoTen = uv.HoTen,
                    LoaiDon = loaiDon,
                    TrangThai = label,
                    TrangThaiClass = css
                });
            }

            return View(vms);
        }

        // GET: Trả về fragment cho popup (modal)
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var uv = await _context.UngViens.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.UngVienId == id);
            if (uv == null) return NotFound();

            // Partial view đặt tại: /Areas/Admin/Views/Candidates/_DetailsCard.cshtml
            return PartialView("_DetailsCard", uv);
        }

        // GET: Thêm mới
        public IActionResult Create() => View();

        // POST: Thêm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UngVien ungVien)
        {
            if (!ModelState.IsValid) return View(ungVien);

            _context.Add(ungVien);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = ungVien.UngVienId });
        }

        // Chi tiết (full page - vẫn giữ nếu cần)
        public async Task<IActionResult> Details(int id)
        {
            var ungVien = await _context.UngViens.AsNoTracking()
                              .FirstOrDefaultAsync(m => m.UngVienId == id);
            if (ungVien == null) return NotFound();
            return View(ungVien);
        }

        // Xuất CSV (giữ nguyên)
        [HttpGet]
        public IActionResult ExportCsv()
        {
            var ungViens = _context.UngViens.AsNoTracking().ToList();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Họ tên,Ngày sinh,Giới tính,Số điện thoại,Email,CCCD,Ngày cấp CCCD,Nơi cấp CCCD,Địa chỉ thường trú,Địa chỉ cư trú,Mã số thuế,Số tài khoản,Tình trạng sức khỏe,Trình độ chuyên môn");

            foreach (var uv in ungViens)
            {
                csv.AppendLine($"{uv.UngVienId}," +
                               $"{uv.HoTen}," +
                               $"{uv.NgaySinh:dd/MM/yyyy}," +
                               $"{(uv.GioiTinh == 1 ? "Nam" : "Nữ")}," +
                               $"{uv.SoDienThoai}," +
                               $"{uv.Email}," +
                               $"{uv.CCCD}," +
                               $"{uv.NgayCapCCCD:dd/MM/yyyy}," +
                               $"{uv.NoiCapCCCD}," +
                               $"{uv.DiaChiThuongTru}," +
                               $"{uv.DiaChiCuTru}," +
                               $"{uv.MaSoThue}," +
                               $"{uv.SoTaiKhoan}," +
                               $"{uv.TinhTrangSucKhoe}," +
                               $"{uv.TrinhDoChuyenMon}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "DanhSachUngVien.csv");
        }

        // Xuất Word (dành cho nút ở cuối popup)
        [HttpGet]
        public async Task<IActionResult> ExportWord(int id)
        {
            var uv = await _context.UngViens.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UngVienId == id);
            if (uv == null) return NotFound();

            // Lấy "Loại đơn" + "Trạng thái" mới nhất cho ứng viên này
            var vcRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join dvc in _context.DonVienChuc.AsNoTracking() on a.DonId equals dvc.VienChucId
                where dvc.UngVienId == id && a.LoaiDon == "VienChuc"
                select new
                {
                    a.AuditTrailId,
                    Date = (DateTime?)a.NgayCapNhatMoi ?? (DateTime?)a.NgayTao,
                    a.LoaiDon,
                    dvc.TrangThai
                }
            ).ToListAsync();

            var hdRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join hd in _context.HopDongNguoiLaoDong.AsNoTracking() on a.DonId equals hd.HopDongId
                where hd.UngVienId == id && a.LoaiDon == "NguoiLaoDong"
                select new
                {
                    a.AuditTrailId,
                    Date = (DateTime?)a.NgayCapNhatMoi ?? (DateTime?)a.NgayTao,
                    a.LoaiDon,
                    hd.TrangThai
                }
            ).ToListAsync();

            var vcLatest = vcRows.OrderByDescending(x => x.Date ?? DateTime.MinValue)
                                 .ThenByDescending(x => x.AuditTrailId).FirstOrDefault();
            var hdLatest = hdRows.OrderByDescending(x => x.Date ?? DateTime.MinValue)
                                 .ThenByDescending(x => x.AuditTrailId).FirstOrDefault();

            string loaiDon = "-";
            int? stt = null;
            if (vcLatest == null && hdLatest == null)
            {
                // giữ mặc định
            }
            else if (hdLatest == null) { loaiDon = vcLatest!.LoaiDon ?? "Viên chức"; stt = vcLatest.TrangThai; }
            else if (vcLatest == null) { loaiDon = hdLatest!.LoaiDon ?? "Hợp đồng lao động"; stt = hdLatest.TrangThai; }
            else
            {
                var vcDate = vcLatest.Date ?? DateTime.MinValue;
                var hdDate = hdLatest.Date ?? DateTime.MinValue;
                if (vcDate > hdDate || (vcDate == hdDate && vcLatest.AuditTrailId > hdLatest.AuditTrailId))
                {
                    loaiDon = vcLatest!.LoaiDon ?? "Viên chức"; stt = vcLatest.TrangThai;
                }
                else
                {
                    loaiDon = hdLatest!.LoaiDon ?? "Hợp đồng lao động"; stt = hdLatest.TrangThai;
                }
            }

            string statusLabel = stt switch
            {
                0 => "Chờ xử lý",
                1 => "Đã duyệt",
                2 => "Đã hủy",
                _ => ""
            };

            string G(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
            string fmtDate(DateTime d) => d.ToString("dd/MM/yyyy");

            var html = $@"
            <html>
            <head>
            <meta charset='utf-8' />
            <title>UngVien_{uv.UngVienId}</title>
            <style>
            body {{ font-family: 'Times New Roman', serif; font-size: 12pt; }}
            h1 {{ text-align: center; margin: 0 0 16px 0; }}
            table {{ width: 100%; border-collapse: collapse; }}
            td {{ border: 1px solid #999; padding: 6px 8px; vertical-align: top; }}
            td.label {{ width: 30%; font-weight: bold; background: #f5f5f5; }}
            </style>
            </head>
            <body>
            <h1>THÔNG TIN ỨNG VIÊN</h1>
            <table>
            <tr><td class='label'>Mã ứng viên</td><td>{uv.UngVienId}</td></tr>
            <tr><td class='label'>Họ và tên</td><td>{G(uv.HoTen)}</td></tr>
            <tr><td class='label'>Ngày sinh</td><td>{fmtDate(uv.NgaySinh)}</td></tr>
            <tr><td class='label'>Giới tính</td><td>{(uv.GioiTinh == 1 ? "Nam" : "Nữ")}</td></tr>
            <tr><td class='label'>Số điện thoại</td><td>{G(uv.SoDienThoai)}</td></tr>
            <tr><td class='label'>Email</td><td>{G(uv.Email)}</td></tr>
            <tr><td class='label'>CCCD</td><td>{G(uv.CCCD)}</td></tr>
            <tr><td class='label'>Ngày cấp CCCD</td><td>{fmtDate(uv.NgayCapCCCD)}</td></tr>
            <tr><td class='label'>Nơi cấp CCCD</td><td>{G(uv.NoiCapCCCD)}</td></tr>
            <tr><td class='label'>Địa chỉ thường trú</td><td>{G(uv.DiaChiThuongTru)}</td></tr>
            <tr><td class='label'>Địa chỉ cư trú</td><td>{G(uv.DiaChiCuTru)}</td></tr>
            <tr><td class='label'>Mã số thuế</td><td>{G(uv.MaSoThue)}</td></tr>
            <tr><td class='label'>Số tài khoản</td><td>{G(uv.SoTaiKhoan)}</td></tr>
            <tr><td class='label'>Tình trạng sức khỏe</td><td>{G(uv.TinhTrangSucKhoe)}</td></tr>
            <tr><td class='label'>Trình độ chuyên môn</td><td>{G(uv.TrinhDoChuyenMon)}</td></tr>
            <tr><td class='label'>Loại đơn</td><td>{G(loaiDon)}</td></tr>
            <tr><td class='label'>Trạng thái</td><td>{G(statusLabel)}</td></tr>
            </table>
            </body>
            </html>";

            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            var fileName = $"UngVien_{uv.UngVienId}.doc"; // .doc (HTML)
            return File(bytes, "application/msword", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> GetApplicationLink(int id)
        {
            // Lấy các bản ghi audit + đơn VC
            var vcRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join dvc in _context.DonVienChuc.AsNoTracking() on a.DonId equals dvc.VienChucId
                where dvc.UngVienId == id && (a.LoaiDon == "VienChuc" || a.LoaiDon == "Viên chức")
                select new
                {
                    a.AuditTrailId,
                    Date = (DateTime?)a.NgayCapNhatMoi ?? (DateTime?)a.NgayTao,
                    DonId = dvc.VienChucId,
                    Label = "Đơn viên chức",
                    Url = Url.Action("Details", "DonVienChuc", new { area = "Admin", id = dvc.VienChucId })
                }
            ).ToListAsync();

            // Lấy các bản ghi audit + đơn HĐLĐ
            var hdRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join hd in _context.HopDongNguoiLaoDong.AsNoTracking() on a.DonId equals hd.HopDongId
                where hd.UngVienId == id && (a.LoaiDon == "NguoiLaoDong" || a.LoaiDon == "Người lao động")
                select new
                {
                    a.AuditTrailId,
                    Date = (DateTime?)a.NgayCapNhatMoi ?? (DateTime?)a.NgayTao,
                    DonId = hd.HopDongId,
                    Label = "Hợp đồng lao động",
                    Url = Url.Action("Details", "HopDongNguoiLaoDong", new { area = "Admin", id = hd.HopDongId })
                }
            ).ToListAsync();

            var vcLatest = vcRows.OrderByDescending(x => x.Date ?? DateTime.MinValue)
                                 .ThenByDescending(x => x.AuditTrailId).FirstOrDefault();
            var hdLatest = hdRows.OrderByDescending(x => x.Date ?? DateTime.MinValue)
                                 .ThenByDescending(x => x.AuditTrailId).FirstOrDefault();

            // Quyết định đơn mới nhất
            string? url = null;
            string label = "";

            if (vcLatest == null && hdLatest == null)
            {
                url = null;
                label = "";
            }
            else if (hdLatest == null)
            {
                url = vcLatest!.Url;
                label = vcLatest.Label;
            }
            else if (vcLatest == null)
            {
                url = hdLatest!.Url;
                label = hdLatest.Label;
            }
            else
            {
                var vcDate = vcLatest.Date ?? DateTime.MinValue;
                var hdDate = hdLatest.Date ?? DateTime.MinValue;

                var takeVC = vcDate > hdDate || (vcDate == hdDate && vcLatest.AuditTrailId > hdLatest.AuditTrailId);

                if (takeVC)
                {
                    url = vcLatest.Url;
                    label = vcLatest.Label;
                }
                else
                {
                    url = hdLatest.Url;
                    label = hdLatest.Label;
                }
            }

            return Json(new { url, label });
        }

    }
}
