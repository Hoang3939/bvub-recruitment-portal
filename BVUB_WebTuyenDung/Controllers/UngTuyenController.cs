using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BVUB_WebTuyenDung.Data;
using BVUB_WebTuyenDung.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
// Dùng Email sender theo hướng DI qua Services (đọc từ DB settings)
using BVUB_WebTuyenDung.Areas.Admin.Services;

namespace BVUB_WebTuyenDung.Controllers
{
    public class UngTuyenController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UngTuyenController> _logger;
        private readonly IEmailSender _email;
        private readonly ISettingsStore _settings;

        private static readonly char[] _maChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

        public UngTuyenController(
            ApplicationDbContext context,
            IEmailSender email,
            ILogger<UngTuyenController> logger,
            ISettingsStore settings)
        {
            _context = context;
            _email = email;
            _logger = logger;
            _settings = settings;
        }

        // ===== Helpers sinh mã tra cứu =====
        private string NewMa(int len = 6)
        {
            var rnd = Random.Shared;
            var buf = new char[len];
            for (int i = 0; i < len; i++) buf[i] = _maChars[rnd.Next(_maChars.Length)];
            return new string(buf);
        }

        private async Task<string> GenerateUniqueMaTraCuuAsync(int len = 6)
        {
            for (int i = 0; i < 50; i++)
            {
                var code = NewMa(len);
                var existsVC = await _context.DonVienChuc.AnyAsync(d => d.MaTraCuu == code);
                var existsNLD = await _context.HopDongNguoiLaoDong.AnyAsync(h => h.MaTraCuu == code);
                if (!existsVC && !existsNLD) return code;
            }
            return Guid.NewGuid().ToString("N")[..len].ToUpperInvariant();
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.LinkVCOpen = await _settings.IsLinkOpenAsync("VC");
            ViewBag.LinkNLDOpen = await _settings.IsLinkOpenAsync("NLD");
            return View();
        }

        // ===== Viên chức (GET form) =====
        public async Task<IActionResult> VienChuc(int? selectedChucDanhId = null)
        {
            if (!await _settings.IsLinkOpenAsync("VC"))
                return View("LinkKhoa", (object)"Viên chức");

            ViewBag.LoaiVanBangOptions = GetLoaiVanBangOptions();

            ViewBag.ChucDanhList = new SelectList(
                _context.DanhMucChucDanhDuTuyen
                        .Where(x => x.TamNgung == 0)
                        .AsNoTracking().ToList(),
                "ChucDanhId", "TenChucDanh", selectedChucDanhId);

            ViewBag.ViTriList = selectedChucDanhId.HasValue
                ? new SelectList(
                    _context.DanhMucViTriDuTuyen
                            .Where(v => v.ChucDanhId == selectedChucDanhId && v.TamNgung == 0)
                            .AsNoTracking().ToList(),
                    "ViTriId", "TenViTri")
                : new SelectList(Enumerable.Empty<SelectListItem>());

            ViewBag.KhoaPhongList = new SelectList(Enumerable.Empty<SelectListItem>());

            var model = new UngTuyenVienChucViewModel
            {
                SelectedChucDanhId = selectedChucDanhId,
                UngVien = new UngVien(),
                DonVienChuc = new DonVienChuc(),
                VanBangs = new List<VanBang>()
            };
            return View(model);
        }

        // ===== Người lao động (GET form) =====
        public async Task<IActionResult> NguoiLaoDong()
        {
            if (!await _settings.IsLinkOpenAsync("NLD"))
                return View("LinkKhoa", (object)"Người lao động");

            var model = new UngTuyenNguoiLaoDongViewModel();
            ViewBag.KhoaPhongList = new SelectList(
                _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                "KhoaPhongId", "Ten");
            ViewBag.LoaiVanBangOptions = GetLoaiVanBangOptions();
            return View(model);
        }

        // ===== Nộp hồ sơ Người lao động =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UngTuyenNguoiLaoDong(UngTuyenNguoiLaoDongViewModel model)
        {
            if (!await _settings.IsLinkOpenAsync("NLD"))
                return View("LinkKhoa", (object)"Người lao động");

            ModelState.Remove("UngVien.NgayUngTuyen");
            ModelState.Remove("HopDongNguoiLaoDong.NgayNop");
            ModelState.Remove("HopDongNguoiLaoDong.UngVienId");
            ModelState.Remove("HopDongNguoiLaoDong.MaTraCuu");
            ModelState.Remove("HopDongNguoiLaoDong.Loai");
            ModelState.Remove("HopDongNguoiLaoDong.UngVien");

            // Map "Khác" cho Dân tộc / Tôn giáo
            var danTocKhac = (Request.Form["DanTocKhac"].ToString() ?? "").Trim();
            if (string.Equals(model.HopDongNguoiLaoDong?.DanToc, "__OTHER__", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(danTocKhac))
                model.HopDongNguoiLaoDong.DanToc = danTocKhac;

            var tonGiaoKhac = (Request.Form["TonGiaoKhac"].ToString() ?? "").Trim();
            if (string.Equals(model.HopDongNguoiLaoDong?.TonGiao, "__OTHER__", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(tonGiaoKhac))
                model.HopDongNguoiLaoDong.TonGiao = tonGiaoKhac;

            // Nếu không phải Đảng viên → bỏ validate ngày
            if (model.HopDongNguoiLaoDong?.DangVien != true)
            {
                ModelState.Remove("HopDongNguoiLaoDong.NgayVaoDang");
                ModelState.Remove("HopDongNguoiLaoDong.NgayChinhThuc");
                if (model.HopDongNguoiLaoDong != null)
                {
                    model.HopDongNguoiLaoDong.NgayVaoDang = null;
                    model.HopDongNguoiLaoDong.NgayChinhThuc = null;
                }
            }

            PruneEmptyVanBangs(model.VanBangs, "VanBangs");
            ClearVBModelStateErrors();

            if (model.UngVien?.NgaySinh is DateTime ns && ns.Date > DateTime.Today)
                ModelState.AddModelError("UngVien.NgaySinh", "Ngày sinh không được lớn hơn ngày hiện tại.");
            if (model.UngVien?.NgayCapCCCD is DateTime nc && nc.Date > DateTime.Today)
                ModelState.AddModelError("UngVien.NgayCapCCCD", "Ngày cấp không được lớn hơn ngày hiện tại.");

            if (!ModelState.IsValid)
            {
                ViewBag.KhoaPhongList = new SelectList(
                    _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                    "KhoaPhongId", "Ten", model.HopDongNguoiLaoDong?.KhoaPhongCongTacId);
                ViewBag.LoaiVanBangOptions = GetLoaiVanBangOptions();
                return View("NguoiLaoDong", model);
            }

            try
            {
                // 1) Lấy hoặc tạo UngVien theo Email (không trùng)
                var ungVien = await GetOrCreateUngVienByEmailAsync(model.UngVien);

                // 2) Không cho tạo 2 hợp đồng cho cùng 1 ứng viên
                if (await _context.HopDongNguoiLaoDong.AnyAsync(h => h.UngVienId == ungVien.UngVienId))
                {
                    ModelState.AddModelError("", "Email này đã có hồ sơ người lao động.");
                    ViewBag.KhoaPhongList = new SelectList(
                        _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                        "KhoaPhongId", "Ten", model.HopDongNguoiLaoDong?.KhoaPhongCongTacId);
                    return View("NguoiLaoDong", model);
                }

                // 3) Bắt buộc có ít nhất 1 văn bằng (tồn tại sẵn hoặc nhập mới)
                var daCoVb = await _context.VanBang.AnyAsync(v => v.UngVienId == ungVien.UngVienId);
                var vbTrongForm = model.VanBangs != null && model.VanBangs.Count > 0;
                if (!daCoVb && !vbTrongForm)
                {
                    ModelState.AddModelError("", "Vui lòng khai tối thiểu 1 văn bằng.");
                    ViewBag.KhoaPhongList = new SelectList(
                        _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                        "KhoaPhongId", "Ten", model.HopDongNguoiLaoDong?.KhoaPhongCongTacId);
                    ViewBag.LoaiVanBangOptions = GetLoaiVanBangOptions();
                    return View("NguoiLaoDong", model);
                }

                // 4) Lưu HĐ
                var hd = model.HopDongNguoiLaoDong;
                hd.UngVienId = ungVien.UngVienId;
                hd.NgayNop = DateTime.Now;
                hd.TrangThai = 1;
                hd.Loai = "Người lao động";
                hd.MaTraCuu = await GenerateUniqueMaTraCuuAsync(6);
                _context.HopDongNguoiLaoDong.Add(hd);
                await _context.SaveChangesAsync();

                // 5) Thêm mới các văn bằng chưa tồn tại (nếu có)
                await AddNewVanBangsAsync(ungVien.UngVienId, model.VanBangs, Request.Form);

                // 6) Gửi email xác nhận
                try
                {
                    var to = ungVien.Email ?? string.Empty;
                    var subject = "Xác nhận nộp hồ sơ Người lao động - BV Ung Bướu TP.HCM";
                    var html = $@"
                        <p>Chào <b>{System.Net.WebUtility.HtmlEncode(ungVien.HoTen)}</b>,</p>
                        <p>Anh/Chị đã nộp <b>Hồ sơ Người lao động</b> thành công.</p>
                        <p>Mã tra cứu hồ sơ của Anh/Chị là: <b style=""font-size:18px"">{hd.MaTraCuu}</b></p>
                        <p>Vui lòng lưu lại mã này để tra cứu tình trạng xử lý.</p>
                        <p>Mọi thắc mắc hay chỉnh sửa hồ sơ vui lòng liên hệ số điện thoại ... phòng nhân sự.</p>
                        <hr/><p>Trân trọng,<br/>Bệnh viện Ung Bướu TP.HCM - cơ sở 2</p>";

                    // SỬA: gọi theo chữ ký 5 tham số (thêm toName, isHtml)
                    await _email.SendAsync(to, ungVien.HoTen ?? to, subject, html, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gửi email NLD thất bại cho {Email}", ungVien.Email);
                }

                return RedirectToAction("ThanhCong");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                ViewBag.KhoaPhongList = new SelectList(
                    _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                    "KhoaPhongId", "Ten", model.HopDongNguoiLaoDong?.KhoaPhongCongTacId);
                return View("NguoiLaoDong", model);
            }
        }

        public IActionResult ThanhCong() => View();

        // ===== Nộp Đơn Viên chức =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UngTuyenVienChuc(UngTuyenVienChucViewModel model)
        {
            if (!await _settings.IsLinkOpenAsync("VC"))
                return View("LinkKhoa", (object)"Viên chức");

            var now = DateTime.Now;
            model.UngVien ??= new UngVien();
            model.DonVienChuc ??= new DonVienChuc();

            model.UngVien.NgayUngTuyen = now;
            model.DonVienChuc.NgayNop = now;
            model.DonVienChuc.TrangThai = 1;
            model.DonVienChuc.MaTraCuu = await GenerateUniqueMaTraCuuAsync(6);

            // Map “Khác”
            var tdvhKhac = (Request.Form["TrinhDoVanHoaKhac"].ToString() ?? "").Trim();
            if (string.Equals(model.DonVienChuc.TrinhDoVanHoa, "Khác", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(tdvhKhac))
                model.DonVienChuc.TrinhDoVanHoa = tdvhKhac;

            var skKhac = (Request.Form["TinhTrangSucKhoeKhac"].ToString() ?? "").Trim();
            if (string.Equals(model.UngVien.TinhTrangSucKhoe, "Khác", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(skKhac))
                model.UngVien.TinhTrangSucKhoe = skKhac;

            var uuTienKhac = (Request.Form["DoiTuongUuTienKhac"].ToString() ?? "").Trim();
            if (string.Equals(model.DonVienChuc.DoiTuongUuTien, "Khác", StringComparison.OrdinalIgnoreCase)
                || string.Equals(model.DonVienChuc.DoiTuongUuTien, "__OTHER__", StringComparison.OrdinalIgnoreCase))
                if (!string.IsNullOrWhiteSpace(uuTienKhac))
                    model.DonVienChuc.DoiTuongUuTien = uuTienKhac;

            var loaiHinhKhac = (Request.Form["LoaiHinhDaoTaoKhac"].ToString() ?? "").Trim();
            if (string.Equals(model.DonVienChuc.LoaiHinhDaoTao, "Khác", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(loaiHinhKhac))
                model.DonVienChuc.LoaiHinhDaoTao = loaiHinhKhac;

            // Bỏ validate các field server-generated
            ModelState.Remove("UngVien.NgayUngTuyen");
            ModelState.Remove("DonVienChuc.NgayNop");
            ModelState.Remove("DonVienChuc.MaTraCuu");
            ModelState.Remove("DonVienChuc.UngVienId");
            ModelState.Remove("DonVienChuc.ChucDanhDuTuyen");
            ModelState.Remove("DonVienChuc.ThamGiaDaoTao");
            PruneEmptyVanBangs(model.VanBangs, "VanBangs");
            ClearVBModelStateErrors();

            // Validate bổ sung
            if (model.DonVienChuc.ChucDanhDuTuyenId <= 0)
                ModelState.AddModelError("DonVienChuc.ChucDanhDuTuyenId", "Vui lòng chọn chức danh.");
            if (model.DonVienChuc.ViTriDuTuyenId <= 0)
                ModelState.AddModelError("DonVienChuc.ViTriDuTuyenId", "Vui lòng chọn vị trí.");
            if (model.DonVienChuc.KhoaPhongId <= 0)
                ModelState.AddModelError("DonVienChuc.KhoaPhongId", "Vui lòng chọn khoa/phòng.");
            if (model.UngVien?.NgaySinh is DateTime ns && ns.Date > DateTime.Today)
                ModelState.AddModelError("UngVien.NgaySinh", "Ngày sinh không được lớn hơn ngày hiện tại.");
            if (model.UngVien?.NgayCapCCCD is DateTime nc && nc.Date > DateTime.Today)
                ModelState.AddModelError("UngVien.NgayCapCCCD", "Ngày cấp không được lớn hơn ngày hiện tại.");

            if (!ModelState.IsValid)
            {
                RefillVienChucViewBags(model);
                return View("VienChuc", model);
            }

            try
            {
                // 1) Lấy / tạo UngVien theo email (không trùng)
                var ungVien = await GetOrCreateUngVienByEmailAsync(model.UngVien);

                // 2) Chặn nộp đơn VC lần 2
                var daCoDonVC = await _context.DonVienChuc.AnyAsync(d => d.UngVienId == ungVien.UngVienId);
                if (daCoDonVC)
                {
                    ModelState.AddModelError("", "Email này đã có hồ sơ VIÊN CHỨC.");
                    RefillVienChucViewBags(model);
                    return View("VienChuc", model);
                }

                // 3) Bắt buộc có ít nhất 1 văn bằng
                var daCoVb = await _context.VanBang.AnyAsync(v => v.UngVienId == ungVien.UngVienId);
                var vbTrongForm = model.VanBangs != null && model.VanBangs.Count > 0;
                if (!daCoVb && !vbTrongForm)
                {
                    ModelState.AddModelError("", "Vui lòng khai tối thiểu 1 văn bằng.");
                    RefillVienChucViewBags(model);
                    return View("VienChuc", model);
                }

                // 4) Lưu ĐVC
                model.DonVienChuc.UngVienId = ungVien.UngVienId;
                _context.DonVienChuc.Add(model.DonVienChuc);
                await _context.SaveChangesAsync();

                // 5) Thêm mới VB nếu chưa có
                await AddNewVanBangsAsync(ungVien.UngVienId, model.VanBangs, Request.Form);

                // 6) Email xác nhận
                try
                {
                    var to = model.UngVien.Email ?? string.Empty;
                    var subject = "Xác nhận nộp Đơn ứng tuyển Viên chức - BV Ung Bướu TP.HCM";
                    var html = $@"
                        <p>Chào <b>{System.Net.WebUtility.HtmlEncode(model.UngVien.HoTen)}</b>,</p>
                        <p>Anh/Chị đã nộp <b>Đơn ứng tuyển Viên chức</b> thành công.</p>
                        <p>Mã tra cứu hồ sơ của Anh/Chị là: <b style=""font-size:18px"">{model.DonVienChuc.MaTraCuu}</b></p>
                        <p>Vui lòng lưu lại mã này để tra cứu tình trạng xử lý.</p>
                        <p>Mọi thắc mắc hay chỉnh sửa hồ sơ vui lòng liên hệ số điện thoại ... phòng nhân sự.</p>
                        <hr/><p>Trân trọng,<br/>Bệnh viện Ung Bướu TP.HCM</p>";

                    // SỬA: gọi theo chữ ký 5 tham số (thêm toName, isHtml)
                    await _email.SendAsync(to, model.UngVien.HoTen ?? to, subject, html, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gửi email VC thất bại cho {Email}", model.UngVien.Email);
                }

                return RedirectToAction("ThanhCong");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                RefillVienChucViewBags(model);
                return View("VienChuc", model);
            }
        }

        private void RefillVienChucViewBags(UngTuyenVienChucViewModel model)
        {
            ViewBag.LoaiVanBangOptions = GetLoaiVanBangOptions();

            ViewBag.ChucDanhList = new SelectList(
                _context.DanhMucChucDanhDuTuyen.AsNoTracking().Where(x => x.TamNgung == 0).ToList(),
                "ChucDanhId", "TenChucDanh", model.DonVienChuc?.ChucDanhDuTuyenId);

            var cdId = model.DonVienChuc?.ChucDanhDuTuyenId;
            ViewBag.ViTriList = cdId.HasValue
                ? new SelectList(
                    _context.DanhMucViTriDuTuyen.Where(v => v.ChucDanhId == cdId && v.TamNgung == 0)
                        .AsNoTracking().ToList(),
                    "ViTriId", "TenViTri", model.DonVienChuc?.ViTriDuTuyenId)
                : new SelectList(Enumerable.Empty<SelectListItem>());

            ViewBag.KhoaPhongList = new SelectList(Enumerable.Empty<SelectListItem>());
        }

        [HttpGet]
        public IActionResult GetViTriByChucDanh(int chucDanhId)
        {
            var viTriList = _context.DanhMucViTriDuTuyen
                .Where(v => v.ChucDanhId == chucDanhId && v.TamNgung == 0)
                .Select(v => new { ViTriId = v.ViTriId, TenViTri = v.TenViTri })
                .ToList();

            return Json(viTriList);
        }

        [HttpGet]
        public IActionResult GetKhoaPhongByViTri(int viTriId)
        {
            var khoaPhongList = _context.KhoaPhongViTri
                .Where(x => x.ViTriId == viTriId && x.KhoaPhong.TamNgung == 0)
                .Select(x => new { KhoaPhongId = x.KhoaPhongId, TenKhoaPhong = x.KhoaPhong.Ten })
                .AsNoTracking()
                .ToList();

            return Json(khoaPhongList);
        }

        private List<string> GetLoaiVanBangOptions() => new()
        {
            "Tiến sĩ","Thạc sĩ","Chuyên khoa II","Chuyên khoa I","Nội trú",
            "Đại học","Cao đẳng","Trung cấp","Văn bằng 2","Ngoại ngữ",
            "Tin học","Khác","Chứng chỉ bồi dưỡng nghiệp vụ công tác xã hội"
        };

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { ok = false, message = "Vui lòng nhập Email." });

            email = email.Trim();
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                email, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return Json(new { ok = false, message = "Sai định dạng Email." });

            var ungVien = await _context.UngVien
                .Include(u => u.HopDongNguoiLaoDong)
                .Include(u => u.DonVienChuc)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (ungVien == null)
                return Json(new { ok = true, exists = false, hasUngVien = false });

            var vanBangs = await _context.VanBang
                .Where(v => v.UngVienId == ungVien.UngVienId)
                .AsNoTracking()
                .Select(v => new
                {
                    v.VanBangId,
                    v.LoaiVanBang,
                    v.TenCoSo,
                    NgayCap = v.NgayCap.HasValue ? v.NgayCap.Value.ToString("yyyy-MM-dd") : null,
                    v.SoHieu,
                    v.ChuyenNganhDaoTao,
                    v.NganhDaoTao,
                    v.HinhThucDaoTao,
                    v.XepLoai
                })
                .ToListAsync();

            return Json(new
            {
                ok = true,
                exists = true,
                hasUngVien = true,
                hasHopDong = ungVien.HopDongNguoiLaoDong != null,
                hasDonVienChuc = ungVien.DonVienChuc != null,
                ungVien = new
                {
                    ungVien.HoTen,
                    ungVien.GioiTinh,
                    NgaySinh = ungVien.NgaySinh?.ToString("yyyy-MM-dd"),
                    ungVien.SoDienThoai,
                    ungVien.Email,
                    ungVien.CCCD,
                    NgayCapCCCD = ungVien.NgayCapCCCD?.ToString("yyyy-MM-dd"),
                    ungVien.NoiCapCCCD,
                    ungVien.DiaChiThuongTru,
                    ungVien.DiaChiCuTru,
                    ungVien.MaSoThue,
                    ungVien.SoTaiKhoan,
                    ungVien.TinhTrangSucKhoe,
                    ungVien.TrinhDoChuyenMon
                },
                vanBangs
            });
        }

        private async Task<UngVien> GetOrCreateUngVienByEmailAsync(UngVien posted)
        {
            var email = (posted.Email ?? "").Trim().ToLowerInvariant();
            var existing = await _context.UngVien.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (existing != null)
            {
                // cập nhật mềm
                existing.HoTen = string.IsNullOrWhiteSpace(posted.HoTen) ? existing.HoTen : posted.HoTen;
                existing.GioiTinh = posted.GioiTinh;
                existing.NgaySinh = posted.NgaySinh ?? existing.NgaySinh;
                existing.SoDienThoai = string.IsNullOrWhiteSpace(posted.SoDienThoai) ? existing.SoDienThoai : posted.SoDienThoai;
                existing.CCCD = string.IsNullOrWhiteSpace(posted.CCCD) ? existing.CCCD : posted.CCCD;
                existing.NgayCapCCCD = posted.NgayCapCCCD ?? existing.NgayCapCCCD;
                existing.NoiCapCCCD = string.IsNullOrWhiteSpace(posted.NoiCapCCCD) ? existing.NoiCapCCCD : posted.NoiCapCCCD;
                existing.DiaChiThuongTru = string.IsNullOrWhiteSpace(posted.DiaChiThuongTru) ? existing.DiaChiThuongTru : posted.DiaChiThuongTru;
                existing.DiaChiCuTru = string.IsNullOrWhiteSpace(posted.DiaChiCuTru) ? existing.DiaChiCuTru : posted.DiaChiCuTru;
                existing.MaSoThue = string.IsNullOrWhiteSpace(posted.MaSoThue) ? existing.MaSoThue : posted.MaSoThue;
                existing.SoTaiKhoan = string.IsNullOrWhiteSpace(posted.SoTaiKhoan) ? existing.SoTaiKhoan : posted.SoTaiKhoan;
                existing.TinhTrangSucKhoe = string.IsNullOrWhiteSpace(posted.TinhTrangSucKhoe) ? existing.TinhTrangSucKhoe : posted.TinhTrangSucKhoe;
                existing.TrinhDoChuyenMon = string.IsNullOrWhiteSpace(posted.TrinhDoChuyenMon) ? existing.TrinhDoChuyenMon : posted.TrinhDoChuyenMon;

                await _context.SaveChangesAsync();
                return existing;
            }

            // chưa có -> tạo mới
            posted.NgayUngTuyen = DateTime.Now;
            _context.UngVien.Add(posted);
            await _context.SaveChangesAsync();
            return posted;
        }

        private void ClearVBModelStateErrors()
        {
            var keys = ModelState.Keys
                .Where(k => k.Contains("VanBangs") && k.EndsWith(".UngVienId"))
                .ToList();
            foreach (var k in keys) ModelState.Remove(k);
        }

        private static string VanBangKey(VanBang v)
        {
            string d = v.NgayCap?.Date.ToString("yyyy-MM-dd") ?? "";
            return $"{(v.SoHieu ?? "").Trim().ToUpperInvariant()}|{(v.LoaiVanBang ?? "").Trim().ToUpperInvariant()}|{(v.TenCoSo ?? "").Trim().ToUpperInvariant()}|{d}";
        }

        private async Task<int> AddNewVanBangsAsync(int ungVienId, IList<VanBang>? incoming, IFormCollection form)
        {
            if (incoming == null || incoming.Count == 0) return 0;

            // Gán UngVienId + map HinhThucDaoTaoKhac
            for (int i = 0; i < incoming.Count; i++)
            {
                var vb = incoming[i];
                vb.UngVienId = ungVienId;

                var htKhac = (form[$"VanBangs[{i}].HinhThucDaoTaoKhac"].ToString() ?? "").Trim();
                if (string.Equals(vb.HinhThucDaoTao, "Khác", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(htKhac))
                    vb.HinhThucDaoTao = htKhac;
            }

            // Chống trùng
            var existing = await _context.VanBang
                .Where(v => v.UngVienId == ungVienId)
                .AsNoTracking()
                .ToListAsync();

            var existedKeys = new HashSet<string>(existing.Select(VanBangKey));
            var toInsert = incoming
                .Where(v =>
                    v != null &&
                    !string.IsNullOrWhiteSpace(v.LoaiVanBang) &&
                    !string.IsNullOrWhiteSpace(v.TenCoSo) &&
                    !string.IsNullOrWhiteSpace(v.SoHieu) &&
                    !existedKeys.Contains(VanBangKey(v)))
                .ToList();

            if (toInsert.Count > 0)
            {
                _context.VanBang.AddRange(toInsert);
                await _context.SaveChangesAsync();
            }
            return toInsert.Count;
        }

        private void PruneEmptyVanBangs(IList<VanBang>? list, string prefix = "VanBangs")
        {
            if (list == null) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var v = list[i];
                bool empty = string.IsNullOrWhiteSpace(v?.TenCoSo)
                          && string.IsNullOrWhiteSpace(v?.SoHieu)
                          && string.IsNullOrWhiteSpace(v?.LoaiVanBang);
                if (empty)
                {
                    var keys = ModelState.Keys.Where(k => k.StartsWith($"{prefix}[{i}]")).ToList();
                    foreach (var k in keys) ModelState.Remove(k);
                    list.RemoveAt(i);
                }
            }
        }
    }
}