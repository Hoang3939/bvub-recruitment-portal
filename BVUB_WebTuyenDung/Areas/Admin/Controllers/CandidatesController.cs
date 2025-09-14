using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.Services;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class CandidatesController : Controller
    {
        private readonly AdminDbContext _context;
        private readonly IWebHostEnvironment _env; 
        private readonly IAuditTrailService _audit;

        public CandidatesController(AdminDbContext context, IWebHostEnvironment env, IAuditTrailService audit)
        {
            _context = context;
            _env = env;
            _audit = audit;
        }

        // == Utils ==
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

        private static (string Label, string Css) MapStatus(int? stt) => stt switch
        {
            1 => ("Chờ xử lý", "pending"),
            2 => ("Đã duyệt", "approved"),
            3 => ("Đã hủy", "cancelled"),
            _ => (stt?.ToString() ?? "", "")
        };

        // Ghi nhật ký (AuditTrail)
        private async Task AddAuditAsync(string action)
        {
            try
            {
                var user = User?.Identity?.Name ?? "unknown";
                _context.AuditTrail.Add(new AuditTrail
                {
                    UserName = user,
                    Action = action,
                    ActionDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            catch
            {
                // giữ luồng nghiệp vụ, không throw
            }
        }

        // == Danh sách ==
        [Authorize]
        public async Task<IActionResult> Index(string q, string type, string status, int page = 1, int pageSize = 20)
        {
            var vcQuery =
                from d in _context.DonVienChucs.AsNoTracking()
                join uv in _context.UngViens.AsNoTracking() on d.UngVienId equals uv.UngVienId
                select new
                {
                    uv.UngVienId,
                    uv.HoTen,
                    uv.NgaySinh,
                    NgayUngTuyen = d.NgayNop,
                    DonType = "VC",
                    LoaiDon = "Đơn viên chức",
                    DonId = d.VienChucId,
                    TrangThai = (int?)d.TrangThai
                };

            var hdQuery =
                from h in _context.HopDongNguoiLaoDongs.AsNoTracking()
                join uv in _context.UngViens.AsNoTracking() on h.UngVienId equals uv.UngVienId
                select new
                {
                    uv.UngVienId,
                    uv.HoTen,
                    uv.NgaySinh,
                    NgayUngTuyen = h.NgayNop,
                    DonType = "HD",
                    LoaiDon = "Hợp đồng lao động",
                    DonId = h.HopDongId,
                    TrangThai = (int?)h.TrangThai
                };

            var baseQuery = vcQuery.Concat(hdQuery);

            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim().ToUpperInvariant();
                if (t == "VC" || t == "HD")
                    baseQuery = baseQuery.Where(x => x.DonType == t);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                int st = status.Trim().ToLowerInvariant() switch
                {
                    "pending" => 1,
                    "approved" => 2,
                    "cancelled" => 3,
                    _ => 0
                };
                if (st != 0) baseQuery = baseQuery.Where(x => x.TrangThai == st);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim();

                if (int.TryParse(qq, out var uid))
                {
                    baseQuery = baseQuery.Where(x => x.UngVienId == uid);
                }
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
                        baseQuery = baseQuery.Where(x => EF.Functions.Like(x.HoTen, $"%{qq}%"));
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
                    NgaySinh = x.NgaySinh,
                    NgayUngTuyen = x.NgayUngTuyen,
                    DonType = x.DonType,
                    DonId = x.DonId,
                    LoaiDon = x.LoaiDon,
                    TrangThai = label,
                    TrangThaiClass = css
                };
            }).ToList();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.q = q;
            ViewBag.type = type;
            ViewBag.status = status;
            ViewBag.hasFilter = !string.IsNullOrWhiteSpace(q) || !string.IsNullOrWhiteSpace(type) || !string.IsNullOrWhiteSpace(status);

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var uv = await _context.UngViens.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.UngVienId == id);
            if (uv == null) return NotFound();

            return PartialView("_DetailsCard", uv);
        }

        private sealed class ExportRow
        {
            public string DonType { get; set; } = "";
            public int DonId { get; set; }
            public DateTime? NgayNop { get; set; }
            public int TrangThai { get; set; }

            public int UngVienId { get; set; }
            public string HoTen { get; set; } = "";
            public DateTime NgaySinh { get; set; }
            public int GioiTinh { get; set; }
            public string SoDienThoai { get; set; } = "";
            public string MaSoThue { get; set; } = "";
            public string SoTaiKhoan { get; set; } = "";
            public string Email { get; set; } = "";
            public string CCCD { get; set; } = "";
            public DateTime NgayCapCCCD { get; set; }
            public string NoiCapCCCD { get; set; } = "";
            public string DiaChiThuongTru { get; set; } = "";
            public string DiaChiCuTru { get; set; } = "";

            public string? KhoaPhongTen { get; set; }
            public string? NoiSinh { get; set; }
            public string? ChuyenNganhDaoTao { get; set; }
            public string? NamTotNghiep { get; set; }
            public string? TrinhDoTinHoc { get; set; }
            public string? TrinhDoNgoaiNgu { get; set; }
            public string? ChungChiHanhNghe { get; set; }
            public string? NgheNghiepTruoc { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? q, string? type, string? status)
        {
            var templatePath = Path.Combine(_env.WebRootPath, "templates", "Thong tin Hop dong.xlsx");
            if (!System.IO.File.Exists(templatePath))
                return BadRequest("Không tìm thấy template Excel: /wwwroot/templates/Thong tin Hop dong.xlsx");

            // VC
            var vc = _context.DonVienChucs.AsNoTracking()
                .Select(d => new ExportRow
                {
                    DonType = "VC",
                    DonId = d.VienChucId,
                    NgayNop = d.NgayNop,
                    TrangThai = d.TrangThai,
                    UngVienId = d.UngVien.UngVienId,
                    HoTen = d.UngVien.HoTen,
                    NgaySinh = d.UngVien.NgaySinh,
                    GioiTinh = d.UngVien.GioiTinh,
                    SoDienThoai = d.UngVien.SoDienThoai,
                    MaSoThue = d.UngVien.MaSoThue,
                    SoTaiKhoan = d.UngVien.SoTaiKhoan,
                    Email = d.UngVien.Email,
                    CCCD = d.UngVien.CCCD,
                    NgayCapCCCD = d.UngVien.NgayCapCCCD,
                    NoiCapCCCD = d.UngVien.NoiCapCCCD,
                    DiaChiThuongTru = d.UngVien.DiaChiThuongTru,
                    DiaChiCuTru = d.UngVien.DiaChiCuTru,
                    KhoaPhongTen = d.KhoaPhong.Ten,
                    NoiSinh = null,
                    ChuyenNganhDaoTao = null,
                    NamTotNghiep = null,
                    TrinhDoTinHoc = null,
                    TrinhDoNgoaiNgu = null,
                    ChungChiHanhNghe = null,
                    NgheNghiepTruoc = null
                });

            // HD
            var hd = _context.HopDongNguoiLaoDongs.AsNoTracking()
                .Select(h => new ExportRow
                {
                    DonType = "HD",
                    DonId = h.HopDongId,
                    NgayNop = h.NgayNop,
                    TrangThai = h.TrangThai,
                    UngVienId = h.UngVien.UngVienId,
                    HoTen = h.UngVien.HoTen,
                    NgaySinh = h.UngVien.NgaySinh,
                    GioiTinh = h.UngVien.GioiTinh,
                    SoDienThoai = h.UngVien.SoDienThoai,
                    MaSoThue = h.UngVien.MaSoThue,
                    SoTaiKhoan = h.UngVien.SoTaiKhoan,
                    Email = h.UngVien.Email,
                    CCCD = h.UngVien.CCCD,
                    NgayCapCCCD = h.UngVien.NgayCapCCCD,
                    NoiCapCCCD = h.UngVien.NoiCapCCCD,
                    DiaChiThuongTru = h.UngVien.DiaChiThuongTru,
                    DiaChiCuTru = h.UngVien.DiaChiCuTru,
                    KhoaPhongTen = h.KhoaPhongCongTac.Ten,
                    NoiSinh = h.NoiSinh,
                    ChuyenNganhDaoTao = h.ChuyenNganhDaoTao,
                    NamTotNghiep = h.NamTotNghiep,
                    TrinhDoTinHoc = h.TrinhDoTinHoc,
                    TrinhDoNgoaiNgu = h.TrinhDoNgoaiNgu,
                    ChungChiHanhNghe = h.ChungChiHanhNghe,
                    NgheNghiepTruoc = h.NgheNghiepTruocTuyenDung
                });

            IQueryable<ExportRow> query = vc.Concat(hd);

            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim().ToUpperInvariant();
                query = query.Where(x => x.DonType == t);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                int st = status.Trim().ToLowerInvariant() switch
                {
                    "pending" => 1,
                    "approved" => 2,
                    "cancelled" => 3,
                    _ => 0
                };
                if (st != 0) query = query.Where(x => x.TrangThai == st);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();

                if (int.TryParse(kw, out var idNum))
                {
                    query = query.Where(x => x.UngVienId == idNum || x.DonId == idNum);
                }
                else if (DateTime.TryParseExact(kw,
                         new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                         CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    var from = d.Date; var to = from.AddDays(1);
                    query = query.Where(x => x.NgayNop >= from && x.NgayNop < to);
                }
                else
                {
                    query = query.Where(x =>
                        x.HoTen.Contains(kw) ||
                        x.Email.Contains(kw) ||
                        x.CCCD.Contains(kw));
                }
            }

            var items = await query
                .OrderByDescending(x => x.NgayNop)
                .ThenByDescending(x => x.DonId)
                .ToListAsync();

            var uvIds = items.Select(i => i.UngVienId).Distinct().ToList();

            var vbLookup = await _context.VanBangs.AsNoTracking()
                .Where(v => uvIds.Contains(v.UngVienId))
                .GroupBy(v => v.UngVienId)
                .ToDictionaryAsync(g => g.Key, g => g
                    .OrderByDescending(v => v.NgayCap ?? DateTime.MinValue)
                    .ThenBy(v => v.VanBangId)
                    .ToList());

            XLWorkbook wb;
            try
            {
                var fi = new FileInfo(templatePath);
                if (!fi.Exists || fi.Length == 0)
                    throw new InvalidOperationException("File template rỗng hoặc không tồn tại.");

                using var fs = System.IO.File.OpenRead(templatePath);
                wb = new XLWorkbook(fs);
            }
            catch
            {
                wb = new XLWorkbook();
            }

            var ws = wb.Worksheets.Count > 0 ? wb.Worksheet(1) : wb.AddWorksheet("Sheet1");

            int headerRow = 1;

            if (ws.LastRowUsed() == null && ws.LastColumnUsed() == null)
            {
                ws.Cell(headerRow, 1).Value = "STT";
            }

            var lastCol = ws.LastColumnUsed()?.ColumnNumber()
                         ?? ws.FirstColumnUsed()?.ColumnNumber()
                         ?? 1;
            var lastRow = ws.LastRowUsed()?.RowNumber()
                         ?? headerRow;

            var headerToCol = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var name = (ws.Cell(headerRow, c).GetString() ?? "").Trim();
                if (!string.IsNullOrEmpty(name) && !headerToCol.ContainsKey(name))
                    headerToCol[name] = c;
            }

            void Set(IXLWorksheet s, int row, string header, object? val)
            {
                if (!headerToCol.TryGetValue(header, out var col))
                {
                    col = (++lastCol);
                    headerToCol[header] = col;
                    var headCell = s.Cell(headerRow, col);
                    headCell.Value = header;
                    headCell.Style.Font.Bold = true;
                    headCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    headCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEEEEE");
                }

                var cell = s.Cell(row, col);
                if (val is DateTime dt)
                {
                    cell.SetValue(dt);
                    cell.Style.DateFormat.Format = "dd/MM/yyyy";
                }
                else if (val is DateTimeOffset dto)
                {
                    cell.SetValue(dto.DateTime);
                    cell.Style.DateFormat.Format = "dd/MM/yyyy";
                }
                else if (val is null)
                {
                    cell.SetValue(string.Empty);
                }
                else
                {
                    cell.SetValue(val.ToString());
                }
            }

            string GenderVN(int g) => g == 0 ? "Nam" : g == 1 ? "Nữ" : "";
            string StatusLabel(int s) => s switch
            {
                1 => "Đang duyệt",
                2 => "Đã duyệt",
                3 => "Đã hủy",
                _ => "Không rõ"
            };

            var startRow = Math.Max(lastRow + 1, headerRow + 1);
            int r = startRow, stt = 1;

            foreach (var x in items)
            {
                Set(ws, r, "STT", stt++);
                Set(ws, r, "Loại nhân viên", x.DonType == "VC" ? "Viên chức" : "Người lao động");

                Set(ws, r, "Họ và tên", x.HoTen);
                Set(ws, r, "Ngày tháng năm sinh", x.NgaySinh);
                Set(ws, r, "Giới tính", GenderVN(x.GioiTinh));
                Set(ws, r, "Số điện thoại", x.SoDienThoai);
                Set(ws, r, "Mã số thuế", x.MaSoThue);
                Set(ws, r, "Số tài khoản ngân hàng Vietinbank Chi nhánh 7", x.SoTaiKhoan);
                Set(ws, r, "Email", x.Email);
                Set(ws, r, "Số Thẻ căn cước công dân hoặc số thẻ căn cước", x.CCCD);
                Set(ws, r, "Ngày cấp Thẻ căn cước công dân hoặc thẻ căn cước", x.NgayCapCCCD);
                Set(ws, r, "Nơi cấp Thẻ căn cước công dân hoặc thẻ căn cước", x.NoiCapCCCD);
                Set(ws, r, "Địa chỉ thường trú ", x.DiaChiThuongTru);
                Set(ws, r, "Địa chỉ nơi cư trú/Nơi ở hiện tại", x.DiaChiCuTru);
                Set(ws, r, "Trạng thái hệ thống", StatusLabel(x.TrangThai));

                if (x.NgayNop.HasValue)
                    Set(ws, r, "Ngày nộp đơn (Hệ thống)", x.NgayNop.Value);

                Set(ws, r, "Khoa hiện đang công tác", x.KhoaPhongTen ?? "");

                if (x.DonType == "HD")
                {
                    Set(ws, r, "Nơi sinh", x.NoiSinh);
                    Set(ws, r, "Nghề nghiệp trước khi được tuyển dụng", x.NgheNghiepTruoc);
                    Set(ws, r, "Chuyên ngành đào tạo", x.ChuyenNganhDaoTao);
                    Set(ws, r, "Năm tốt nghiệp", x.NamTotNghiep);
                    Set(ws, r, "Trình độ tin học", x.TrinhDoTinHoc);
                    Set(ws, r, "Trình độ ngoại ngữ", x.TrinhDoNgoaiNgu);
                    Set(ws, r, "[Chứng chỉ hành nghề] Số CCHN (nếu có)", x.ChungChiHanhNghe);
                }

                if (vbLookup.TryGetValue(x.UngVienId, out var listVb))
                {
                    DateTime? dt;
                    string? cn;

                    void FillByLoai(string loai, string colCN, string colDate, string? colLevel = null)
                    {
                        var vb = listVb.FirstOrDefault(v =>
                            !string.IsNullOrEmpty(v.LoaiVanBang) &&
                            v.LoaiVanBang.Trim().Equals(loai, StringComparison.OrdinalIgnoreCase));

                        if (vb == null) return;
                        cn = vb.ChuyenNganhDaoTao;
                        dt = vb.NgayCap;

                        if (!string.IsNullOrEmpty(colLevel))
                            Set(ws, r, colLevel, vb.LoaiVanBang);

                        Set(ws, r, colCN, cn);
                        if (dt.HasValue) Set(ws, r, colDate, dt.Value);
                    }

                    FillByLoai("Tiến sĩ",
                        "[Tiến sĩ] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)",
                        "[Tiến sĩ] Ngày, tháng, năm cấp văn bằng (nếu có)");

                    FillByLoai("Thạc sĩ",
                        "[Thạc sĩ] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)",
                        "[Thạc sĩ] Ngày, tháng, năm cấp văn bằng (nếu có)");

                    FillByLoai("Chuyên khoa cấp II",
                        "[Chuyên khoa cấp II] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)",
                        "[Chuyên khoa cấp II] Ngày, tháng, năm cấp văn bằng (nếu có)");

                    FillByLoai("Chuyên khoa cấp I",
                        "[Chuyên khoa cấp I] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)",
                        "[Chuyên khoa cấp I] Ngày, tháng, năm cấp văn bằng (nếu có)");

                    FillByLoai("Bác sĩ nội trú",
                        "[Bác sĩ nội trú] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)",
                        "[Bác sĩ nội trú] Ngày, tháng, năm cấp văn bằng (nếu có)");

                    FillByLoai("Đại học",
                        "[Đại học] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)",
                        "[Đại học] Ngày, tháng, năm cấp văn bằng (nếu có)");

                    FillByLoai("Cao đẳng",
                        "[Cao đẳng] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)",
                        "[Cao đẳng] Ngày, tháng, năm cấp văn bằng (nếu có)");

                    FillByLoai("Trung cấp",
                        "[Trung cấp] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)",
                        "[Trung cấp] Ngày, tháng, năm cấp văn bằng (nếu có)");

                    var vbKhac = listVb.FirstOrDefault(v =>
                        !string.IsNullOrWhiteSpace(v.LoaiVanBang) &&
                        v.LoaiVanBang.Trim().Equals("Khác", StringComparison.OrdinalIgnoreCase) == false &&
                        !new[] { "Tiến sĩ", "Thạc sĩ", "Chuyên khoa cấp II", "Chuyên khoa cấp I",
                                 "Bác sĩ nội trú", "Đại học", "Cao đẳng", "Trung cấp" }
                            .Contains(v.LoaiVanBang.Trim(), StringComparer.OrdinalIgnoreCase));

                    if (vbKhac != null)
                    {
                        Set(ws, r, "[Văn bằng khác] Trình độ đào tạo", vbKhac.LoaiVanBang);
                        Set(ws, r, "[Văn bằng khác] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)", vbKhac.ChuyenNganhDaoTao);
                        if (vbKhac.NgayCap.HasValue)
                            Set(ws, r, "[Văn bằng khác] Ngày, tháng, năm cấp văn bằng (nếu có)", vbKhac.NgayCap.Value);
                    }

                    var vbTin = listVb.FirstOrDefault(v =>
                        !string.IsNullOrEmpty(v.NganhDaoTao) &&
                        v.NganhDaoTao.Trim().Equals("Tin học", StringComparison.OrdinalIgnoreCase));
                    if (vbTin != null)
                    {
                        Set(ws, r, "[Tin học] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)\nVí dụ: Ứng dụng công nghệ thông tin cơ bản; Cử nhân Công nghệ thông tin", vbTin.ChuyenNganhDaoTao);
                        if (vbTin.NgayCap.HasValue)
                            Set(ws, r, "[Tin học] Ngày, tháng, năm cấp văn bằng (nếu có)", vbTin.NgayCap.Value);
                    }

                    var vbNgoaiNgu = listVb.FirstOrDefault(v =>
                        !string.IsNullOrEmpty(v.NganhDaoTao) &&
                        v.NganhDaoTao.Trim().Equals("Ngoại ngữ", StringComparison.OrdinalIgnoreCase));
                    if (vbNgoaiNgu != null)
                    {
                        Set(ws, r, "[Ngoại ngữ] Chuyên ngành đào tạo ghi theo bảng điểm (nếu có)\nVí dụ: Tiếng Anh trình độ B; IELTS 7.5; TOEIC 990", vbNgoaiNgu.ChuyenNganhDaoTao);
                        if (vbNgoaiNgu.NgayCap.HasValue)
                            Set(ws, r, "[Ngoại ngữ] Ngày, tháng, năm cấp văn bằng (nếu có)", vbNgoaiNgu.NgayCap.Value);
                    }
                }

                r++;
            }

            int lastDataRow = Math.Max(r - 1, headerRow);
            var allRange = ws.Range(headerRow, 1, lastDataRow, ws.LastColumnUsed().ColumnNumber());

            allRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            allRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            allRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            allRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            allRange.Style.Alignment.WrapText = true;

            ws.Row(headerRow).Style.Font.Bold = true;

            if (lastDataRow >= headerRow + 1)
            {
                var dataRange = ws.Range(headerRow + 1, 1, lastDataRow, ws.LastColumnUsed().ColumnNumber());
                dataRange.Style.Font.Bold = false;
            }

            ws.SheetView.FreezeRows(1);

            // Zebra
            var dataStart = headerRow + 1;
            var dataEnd = lastDataRow;
            for (int row = dataStart; row <= dataEnd; row++)
            {
                if ((row - dataStart) % 2 == 1)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F7F7F7");
                else
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.White;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            var fileName = $"UngVien_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // == In Word: lấy đơn mới nhất trực tiếp từ VC/HD ==
        [HttpGet]
        public async Task<IActionResult> ExportWord(int id)
        {
            var uv = await _context.UngViens.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UngVienId == id);
            if (uv == null) return NotFound();

            // Lấy đơn VC/HD mới nhất theo Ngày nộp (nếu null thì -∞), rồi theo Id
            var latestVC = await _context.DonVienChucs.AsNoTracking()
                .Where(d => d.UngVienId == id)
                .OrderByDescending(x => x.NgayNop)
                .ThenByDescending(d => d.VienChucId)
                .Select(d => new { Type = "VC", Ten = "Đơn viên chức", TrangThai = (int?)d.TrangThai, Ngay = d.NgayNop })
                .FirstOrDefaultAsync();

            var latestHD = await _context.HopDongNguoiLaoDongs.AsNoTracking()
                .Where(h => h.UngVienId == id)
                .OrderByDescending(x => x.NgayNop)
                .ThenByDescending(h => h.HopDongId)
                .Select(h => new { Type = "HD", Ten = "Hợp đồng lao động", TrangThai = (int?)h.TrangThai, Ngay = h.NgayNop })
                .FirstOrDefaultAsync();

            string loaiDon = "-";
            int? stt = null;

            if (latestVC == null && latestHD == null) { }
            else if (latestHD == null) { loaiDon = latestVC!.Ten; stt = latestVC.TrangThai; }
            else if (latestVC == null) { loaiDon = latestHD!.Ten; stt = latestHD.TrangThai; }
            else
            {
                var takeVC = latestVC.Ngay > latestHD.Ngay;
                loaiDon = takeVC ? latestVC.Ten : latestHD.Ten;
                stt = takeVC ? latestVC.TrangThai : latestHD.TrangThai;
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

        // == Link tới đơn mới nhất =
        [HttpGet]
        public async Task<IActionResult> GetApplicationLink(int id)
        {
            var latestVC = await _context.DonVienChucs.AsNoTracking()
                .Where(d => d.UngVienId == id)
                .OrderByDescending(d => d.NgayNop)
                .ThenByDescending(d => d.VienChucId)
                .Select(d => new {
                    Ngay = d.NgayNop,
                    Url = Url.Action("Details", "DonVienChuc", new { area = "Admin", id = d.VienChucId }),
                    Label = "Đơn viên chức"
                })
                .FirstOrDefaultAsync();

            var latestHD = await _context.HopDongNguoiLaoDongs.AsNoTracking()
                .Where(h => h.UngVienId == id)
                .OrderByDescending(h => h.NgayNop)
                .ThenByDescending(h => h.HopDongId)
                .Select(h => new {
                    Ngay = h.NgayNop,
                    Url = Url.Action("Details", "HopDongNguoiLaoDong", new { area = "Admin", id = h.HopDongId }),
                    Label = "Hợp đồng lao động"
                })
                .FirstOrDefaultAsync();

            string? url = null; string label = "";
            if (latestVC == null && latestHD == null) { }
            else if (latestHD == null) { url = latestVC!.Url; label = latestVC.Label; }
            else if (latestVC == null) { url = latestHD!.Url; label = latestHD.Label; }
            else
            {
                var takeVC = latestVC.Ngay > latestHD.Ngay;
                url = takeVC ? latestVC.Url : latestHD.Url;
                label = takeVC ? latestVC.Label : latestHD.Label;
            }

            return Json(new { url, label });
        }

        // == Actions thay đổi trạng thái ==
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
                    d.TrangThai = DA_DUYET;
                    await _context.SaveChangesAsync();

                    var userName = User?.Identity?.Name ?? "system";                                
                    await _audit.LogAsync(userName, $"Duyệt đơn viên chức #{id}");

                    return Json(new { ok = true, newStatusLabel = "Đã duyệt", newStatusClass = "approved" });
                }
                if (donType == "HD")
                {
                    var d = await _context.HopDongNguoiLaoDongs.FirstOrDefaultAsync(x => x.HopDongId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy hợp đồng NLĐ." });
                    d.TrangThai = DA_DUYET;
                    await _context.SaveChangesAsync();

                    var userName = User?.Identity?.Name ?? "system";                                
                    await _audit.LogAsync(userName, $"Duyệt hợp đồng NLĐ #{id}");

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "1,Admin")]
        public async Task<IActionResult> UnapproveNow(string donType, int id)
        {
            try
            {
                const int CHO_XU_LY = 1;
                donType = donType?.Trim().ToUpperInvariant();
                if (donType == "VC")
                {
                    var d = await _context.DonVienChucs.FirstOrDefaultAsync(x => x.VienChucId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy đơn viên chức." });
                    d.TrangThai = CHO_XU_LY;
                    await _context.SaveChangesAsync();

                    var userName = User?.Identity?.Name ?? "system";
                    await _audit.LogAsync(userName, $"Bỏ duyệt đơn viên chức #{id}");

                    return Json(new { ok = true, newStatusLabel = "Chờ xử lý", newStatusClass = "pending" });
                }
                if (donType == "HD")
                {
                    var d = await _context.HopDongNguoiLaoDongs.FirstOrDefaultAsync(x => x.HopDongId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy hợp đồng NLĐ." });
                    d.TrangThai = CHO_XU_LY;
                    await _context.SaveChangesAsync();

                    var userName = User?.Identity?.Name ?? "system";
                    await _audit.LogAsync(userName, $"Bỏ duyệt hợp đồng NLĐ #{id}");

                    return Json(new { ok = true, newStatusLabel = "Chờ xử lý", newStatusClass = "pending" });
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
                    d.TrangThai = DA_HUY;
                    await _context.SaveChangesAsync();

                    var userName = User?.Identity?.Name ?? "system";
                    await _audit.LogAsync(userName, $"Hủy đơn viên chức #{id}");

                    return Json(new { ok = true, newStatusLabel = "Đã hủy", newStatusClass = "cancelled" });
                }
                if (donType == "HD")
                {
                    var d = await _context.HopDongNguoiLaoDongs.FirstOrDefaultAsync(x => x.HopDongId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy hợp đồng NLĐ." });
                    d.TrangThai = DA_HUY;
                    await _context.SaveChangesAsync();

                    var userName = User?.Identity?.Name ?? "system";
                    await _audit.LogAsync(userName, $"Hủy hợp đồng NLĐ #{id}");

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

                    var userName = User?.Identity?.Name ?? "system";
                    await _audit.LogAsync(userName, $"Khôi phục đơn viên chức #{id}");

                    return Json(new { ok = true, newStatusLabel = "Đã duyệt", newStatusClass = "approved" });
                }
                if (donType == "HD")
                {
                    var d = await _context.HopDongNguoiLaoDongs.FirstOrDefaultAsync(x => x.HopDongId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy hợp đồng NLĐ." });
                    d.TrangThai = DA_DUYET;
                    await _context.SaveChangesAsync();

                    var userName = User?.Identity?.Name ?? "system";
                    await _audit.LogAsync(userName, $"Khôi phục cho hợp đồng NLĐ #{id}");

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

        // == Xoá đơn ==
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
                    var d = await _context.DonVienChucs
                                          .FirstOrDefaultAsync(x => x.VienChucId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy đơn viên chức." });
                    _context.DonVienChucs.Remove(d);
                    await _context.SaveChangesAsync();
                    await AddAuditAsync($"Xoá đơn viên chức #{id}");
                }
                else if (donType == "HD")
                {
                    var d = await _context.HopDongNguoiLaoDongs
                                          .FirstOrDefaultAsync(x => x.HopDongId == id);
                    if (d == null) return Json(new { ok = false, message = "Không tìm thấy hợp đồng NLĐ." });
                    _context.HopDongNguoiLaoDongs.Remove(d);
                    await _context.SaveChangesAsync();
                    await AddAuditAsync($"Xoá hợp đồng NLĐ #{id}");
                }
                else
                {
                    return Json(new { ok = false, message = "Loại đơn không hợp lệ." });
                }

                await tx.CommitAsync();

                var userName = User?.Identity?.Name ?? "system";
                var label = donType == "VC" ? "đơn viên chức" : "hợp đồng NLĐ";
                await _audit.LogAsync(userName, $"Xóa {label} #{id}");

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
