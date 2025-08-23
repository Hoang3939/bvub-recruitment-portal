using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.Data.SqlClient;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;
using System.Globalization;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class CandidatesController : Controller
    {
        private readonly AdminDbContext _context;
        public CandidatesController(AdminDbContext context) => _context = context;

        // Chuẩn hoá nhãn Loại đơn để hiển thị đẹp (dùng ở ExportWord, ...)
        private static string FormatLoaiDonLabel(string? loaiDon)
        {
            if (string.IsNullOrWhiteSpace(loaiDon)) return "-";
            loaiDon = loaiDon.Trim();
            return loaiDon switch
            {
                "VienChuc" or "Viên chức" => "Đơn viên chức",
                "NguoiLaoDong" or "Người lao động" => "Hợp đồng lao động",
                _ => loaiDon
            };
        }

        // Map trạng thái
        private static (string Label, string Css) MapStatus(int? stt) => stt switch
        {
            1 => ("Chờ xử lý", "pending"),
            2 => ("Đã duyệt", "approved"),
            3 => ("Đã hủy", "cancelled"),
            _ => (stt?.ToString() ?? "", "")
        };

        // Danh sách ứng viên
        [Authorize]
        public async Task<IActionResult> Index(string q, string type, int page = 1, int pageSize = 20)
        {
            // VC
            var vcQuery =
                from d in _context.DonVienChucs.AsNoTracking()
                join uv in _context.UngViens.AsNoTracking() on d.UngVienId equals uv.UngVienId
                select new
                {
                    uv.UngVienId,
                    uv.HoTen,
                    uv.Email,
                    uv.NgayUngTuyen,   
                    DonType = "VC",
                    LoaiDon = "Đơn viên chức",
                    DonId = d.VienChucId,
                    TrangThai = (int?)d.TrangThai
                };

            // HĐLĐ
            var hdQuery =
                from d in _context.HopDongNguoiLaoDongs.AsNoTracking()
                join uv in _context.UngViens.AsNoTracking() on d.UngVienId equals uv.UngVienId
                select new
                {
                    uv.UngVienId,
                    uv.HoTen,
                    uv.Email,
                    uv.NgayUngTuyen,
                    DonType = "HD",
                    LoaiDon = "Hợp đồng lao động",
                    DonId = d.HopDongId,
                    TrangThai = (int?)d.TrangThai
                };

            var baseQuery = vcQuery.Concat(hdQuery);

            // Lọc theo loại đơn (nếu chọn)
            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim().ToUpperInvariant();
                if (t == "VC" || t == "HD")
                    baseQuery = baseQuery.Where(x => x.DonType == t);
            }

            // Lọc theo 1 ô duy nhất "q"
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim();

                // 1) Nếu là số -> Mã ứng viên
                if (int.TryParse(qq, out var uid))
                {
                    baseQuery = baseQuery.Where(x => x.UngVienId == uid);
                }
                // 2) Nếu là ngày -> lọc theo ngày ứng tuyển
                else if (DateTime.TryParseExact(
                            qq,
                            new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var d))
                {
                    var from = d.Date;
                    var to = from.AddDays(1);
                    baseQuery = baseQuery.Where(x => x.NgayUngTuyen >= from && x.NgayUngTuyen < to);
                }
                else
                {
                    // 3) Nếu là từ khoá trạng thái
                    var s = qq.ToLowerInvariant();
                    int? st = s switch
                    {
                        "cho" or "chờ" or "cho xu ly" or "chờ xử lý" or "pending" => 1,
                        "duyet" or "da duyet" or "đã duyệt" or "approved" => 2,
                        "huy" or "da huy" or "đã hủy" or "cancelled" or "canceled" => 3,
                        _ => null
                    };

                    if (st.HasValue)
                    {
                        baseQuery = baseQuery.Where(x => x.TrangThai == st.Value);
                    }
                    else
                    {
                        // 4) Tìm tên hoặc email
                        baseQuery = baseQuery.Where(x =>
                            EF.Functions.Like(x.HoTen, $"%{qq}%") ||
                            EF.Functions.Like(x.Email, $"%{qq}%"));
                    }
                }
            }

            var total = await baseQuery.CountAsync();

            var pageRows = await baseQuery
                .OrderByDescending(x => x.NgayUngTuyen)
                .ThenBy(x => x.UngVienId)
                .ThenBy(x => x.DonType)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var list = pageRows.Select(x =>
            {
                var (label, css) = MapStatus(x.TrangThai);
                return new CandidateListItemVm
                {
                    UngVienId = x.UngVienId,
                    HoTen = x.HoTen,
                    Email = x.Email,
                    NgayUngTuyen = x.NgayUngTuyen,
                    DonType = x.DonType,
                    DonId = x.DonId,
                    LoaiDon = x.LoaiDon,
                    TrangThai = label,
                    TrangThaiClass = css
                };
            }).ToList();

            ViewBag.Total = total;
            ViewBag.Page = page; ViewBag.PageSize = pageSize;
            ViewBag.q = q; ViewBag.type = type;

            return View(list);
        }

        // GET: fragment cho modal xem chi tiết
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var uv = await _context.UngViens.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.UngVienId == id);
            if (uv == null) return NotFound();

            return PartialView("_DetailsCard", uv);
        }

        // GET: Xuất CSV 
        [HttpGet]
        public async Task<IActionResult> ExportCsv(string q)
        {
            var query = _context.UngViens.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim();
                if (int.TryParse(qq, out var id))
                {
                    query = query.Where(uv =>
                        uv.UngVienId == id ||
                        EF.Functions.Like(uv.HoTen, $"%{qq}%") ||
                        EF.Functions.Like(uv.Email, $"%{qq}%"));
                }
                else
                {
                    query = query.Where(uv =>
                        EF.Functions.Like(uv.HoTen, $"%{qq}%") ||
                        EF.Functions.Like(uv.Email, $"%{qq}%"));
                }
            }

            var ungViens = await query.OrderBy(uv => uv.UngVienId).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Họ tên,Ngày sinh,Giới tính,Số điện thoại,Email,CCCD,Ngày cấp CCCD,Nơi cấp CCCD,Địa chỉ thường trú,Địa chỉ cư trú,Mã số thuế,Số tài khoản,Tình trạng sức khỏe,Trình độ chuyên môn");

            foreach (var uv in ungViens)
            {
                csv.AppendLine($"{uv.UngVienId}," +
                               $"{uv.HoTen}," +
                               $"{uv.NgaySinh:dd/MM/yyyy}," +
                               $"{(uv.GioiTinh == 1 ? "Nữ" : "Nam")}," +
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

        // GET: Xuất Word
        [HttpGet]
        public async Task<IActionResult> ExportWord(int id)
        {
            var uv = await _context.UngViens.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UngVienId == id);
            if (uv == null) return NotFound();

            var vcRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join dvc in _context.DonVienChucs.AsNoTracking() on a.DonId equals dvc.VienChucId
                where dvc.UngVienId == id && (a.LoaiDon == "VienChuc" || a.LoaiDon == "Viên chức")
                select new
                {
                    a.AuditTrailId,
                    Date = (DateTime?)a.NgayCapNhatMoi ?? (DateTime?)a.NgayTao,
                    LoaiDon = "VienChuc",
                    dvc.TrangThai
                }
            ).ToListAsync();

            var hdRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join hd in _context.HopDongNguoiLaoDongs.AsNoTracking() on a.DonId equals hd.HopDongId
                where hd.UngVienId == id && (a.LoaiDon == "NguoiLaoDong" || a.LoaiDon == "Người lao động")
                select new
                {
                    a.AuditTrailId,
                    Date = (DateTime?)a.NgayCapNhatMoi ?? (DateTime?)a.NgayTao,
                    LoaiDon = "NguoiLaoDong",
                    hd.TrangThai
                }
            ).ToListAsync();

            var vcLatest = vcRows.OrderByDescending(x => x.Date ?? DateTime.MinValue)
                                 .ThenByDescending(x => x.AuditTrailId).FirstOrDefault();
            var hdLatest = hdRows.OrderByDescending(x => x.Date ?? DateTime.MinValue)
                                 .ThenByDescending(x => x.AuditTrailId).FirstOrDefault();

            string loaiDon = "-";
            int? stt = null;
            if (vcLatest == null && hdLatest == null) { }
            else if (hdLatest == null) { loaiDon = FormatLoaiDonLabel(vcLatest!.LoaiDon); stt = vcLatest.TrangThai; }
            else if (vcLatest == null) { loaiDon = FormatLoaiDonLabel(hdLatest!.LoaiDon); stt = hdLatest.TrangThai; }
            else
            {
                var vcDate = vcLatest.Date ?? DateTime.MinValue;
                var hdDate = hdLatest.Date ?? DateTime.MinValue;
                if (vcDate > hdDate || (vcDate == hdDate && vcLatest.AuditTrailId > hdLatest.AuditTrailId))
                {
                    loaiDon = FormatLoaiDonLabel(vcLatest!.LoaiDon); stt = vcLatest.TrangThai;
                }
                else
                {
                    loaiDon = FormatLoaiDonLabel(hdLatest!.LoaiDon); stt = hdLatest.TrangThai;
                }
            }

            string statusLabel = MapStatus(stt).Label;

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
            <tr><td class='label'>Giới tính</td><td>{(uv.GioiTinh == 1 ? "Nữ" : "Nam")}</td></tr>
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
            var fileName = $"UngVien_{uv.UngVienId}.doc";
            return File(bytes, "application/msword", fileName);
        }

        // GET: Trả về link (URL) đến đơn ứng tuyển mới nhất của ứng viên (VC hoặc HĐLĐ)
        [HttpGet]
        public async Task<IActionResult> GetApplicationLink(int id)
        {
            // VC
            var vcRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join dvc in _context.DonVienChucs.AsNoTracking() on a.DonId equals dvc.VienChucId
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

            // HĐLĐ
            var hdRows = await (
                from a in _context.AuditTrail.AsNoTracking()
                join hd in _context.HopDongNguoiLaoDongs.AsNoTracking() on a.DonId equals hd.HopDongId
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

            string? url = null; string label = "";
            if (vcLatest == null && hdLatest == null) { }
            else if (hdLatest == null) { url = vcLatest!.Url; label = vcLatest.Label; }
            else if (vcLatest == null) { url = hdLatest!.Url; label = hdLatest.Label; }
            else
            {
                var vcDate = vcLatest.Date ?? DateTime.MinValue;
                var hdDate = hdLatest.Date ?? DateTime.MinValue;
                var takeVC = vcDate > hdDate || (vcDate == hdDate && vcLatest.AuditTrailId > hdLatest.AuditTrailId);
                if (takeVC) { url = vcLatest.Url; label = vcLatest.Label; }
                else { url = hdLatest.Url; label = hdLatest.Label; }
            }

            return Json(new { url, label });
        }

        // POST: Duyệt ngay từ danh sách hoặc trang chi tiết (CHỈ ADMIN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "1,Admin")]
        public async Task<IActionResult> ApproveNow(string donType, int id)
        {
            try
            {
                const int DA_DUYET = 2;
                donType = donType?.Trim().ToUpperInvariant();
                if (donType == "VC")
                {
                    var d = await _context.DonVienChucs.FirstOrDefaultAsync(x => x.VienChucId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy đơn viên chức." });
                    if ((int)d.TrangThai == DA_DUYET) return Json(new { ok = true, already = true });

                    d.TrangThai = DA_DUYET;
                    await _context.SaveChangesAsync();
                    return Json(new { ok = true, newStatusLabel = "Đã duyệt", newStatusClass = "approved" });
                }
                if (donType == "HD")
                {
                    var d = await _context.HopDongNguoiLaoDongs.FirstOrDefaultAsync(x => x.HopDongId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy hợp đồng NLĐ." });
                    if ((int)d.TrangThai == DA_DUYET) return Json(new { ok = true, already = true });

                    d.TrangThai = DA_DUYET;
                    await _context.SaveChangesAsync();
                    return Json(new { ok = true, newStatusLabel = "Đã duyệt", newStatusClass = "approved" });
                }
                return Json(new { ok = false, message = "Loại đơn không hợp lệ." });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { ok = false, message = "Lỗi CSDL: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }

        }

        // POST: Hủy đơn 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelNow(string donType, int id)
        {
            try
            {
                const int DA_HUY = 3;
                donType = donType?.Trim().ToUpperInvariant();

                if (donType == "VC")
                {
                    var d = await _context.DonVienChucs.FirstOrDefaultAsync(x => x.VienChucId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy đơn viên chức." });
                    if ((int?)d.TrangThai == DA_HUY) return Json(new { ok = true, already = true });

                    d.TrangThai = DA_HUY;
                    await _context.SaveChangesAsync();
                    return Json(new { ok = true, newStatusLabel = "Đã hủy", newStatusClass = "cancelled" });
                }
                if (donType == "HD")
                {
                    var d = await _context.HopDongNguoiLaoDongs.FirstOrDefaultAsync(x => x.HopDongId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy hợp đồng NLĐ." });
                    if ((int?)d.TrangThai == DA_HUY) return Json(new { ok = true, already = true });

                    d.TrangThai = DA_HUY;
                    await _context.SaveChangesAsync();
                    return Json(new { ok = true, newStatusLabel = "Đã hủy", newStatusClass = "cancelled" });
                }

                return Json(new { ok = false, message = "Loại đơn không hợp lệ." });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { ok = false, message = "Lỗi CSDL: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // POST: Khôi phục đơn về trạng thái ĐÃ DUYỆT 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreNow(string donType, int id)
        {
            try
            {
                const int DA_DUYET = 2;
                donType = donType?.Trim().ToUpperInvariant();

                if (donType == "VC")
                {
                    var d = await _context.DonVienChucs.FirstOrDefaultAsync(x => x.VienChucId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy đơn viên chức." });

                    d.TrangThai = DA_DUYET;
                    await _context.SaveChangesAsync();
                    return Json(new { ok = true, newStatusLabel = "Đã duyệt", newStatusClass = "approved" });
                }
                if (donType == "HD")
                {
                    var d = await _context.HopDongNguoiLaoDongs.FirstOrDefaultAsync(x => x.HopDongId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy hợp đồng NLĐ." });

                    d.TrangThai = DA_DUYET;
                    await _context.SaveChangesAsync();
                    return Json(new { ok = true, newStatusLabel = "Đã duyệt", newStatusClass = "approved" });
                }

                return Json(new { ok = false, message = "Loại đơn không hợp lệ." });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { ok = false, message = "Lỗi CSDL: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // POST: Xóa thông tin đơn ứng tuyển
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "1,Admin")]
        public async Task<IActionResult> DeleteApplication(string donType, int id)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(donType))
                    return Json(new { ok = false, message = "Thiếu loại đơn." });

                donType = donType.Trim().ToUpperInvariant();

                if (donType == "VC")
                {
                    // Xóa AuditTrail của đơn VC này
                    var audits = _context.AuditTrail.Where(a =>
                        (a.LoaiDon == "VienChuc" || a.LoaiDon == "Viên chức") &&
                        a.DonId == id);
                    _context.AuditTrail.RemoveRange(audits);

                    // Xóa đơn VC 
                    var d = await _context.DonVienChucs
                                          .Include(x => x.VanBangs) 
                                          .FirstOrDefaultAsync(x => x.VienChucId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy đơn viên chức." });

                    if (d.VanBangs?.Any() == true) _context.RemoveRange(d.VanBangs);
                    _context.DonVienChucs.Remove(d);
                }
                else if (donType == "HD")
                {
                    // Xóa AuditTrail của đơn HĐLĐ này
                    var audits = _context.AuditTrail.Where(a =>
                        (a.LoaiDon == "NguoiLaoDong" || a.LoaiDon == "Người lao động") &&
                        a.DonId == id);
                    _context.AuditTrail.RemoveRange(audits);

                    // Xóa đơn HĐLĐ
                    var d = await _context.HopDongNguoiLaoDongs
                                          .FirstOrDefaultAsync(x => x.HopDongId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy hợp đồng NLĐ." });

                    _context.HopDongNguoiLaoDongs.Remove(d);
                }
                else
                {
                    return Json(new { ok = false, message = "Loại đơn không hợp lệ." });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Json(new { ok = true });
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                return Json(new { ok = false, message = "Lỗi CSDL: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { ok = false, message = ex.Message });
            }
        }
    }
}
