using BVUB_WebTuyenDung.Data;
using BVUB_WebTuyenDung.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BVUB_WebTuyenDung.Controllers
{
    public class UngTuyenController : Controller
    {
        private readonly ApplicationDbContext _context;
        public UngTuyenController(ApplicationDbContext context) => _context = context;
        private static readonly char[] _maChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

        private string NewMa(int len = 6)
        {
            var rnd = Random.Shared;
            var buf = new char[len];
            for (int i = 0; i < len; i++) buf[i] = _maChars[rnd.Next(_maChars.Length)];
            return new string(buf);
        }

        private async Task<string> GenerateUniqueMaTraCuuAsync(int len = 6)
        {
            // Giới hạn vòng lặp để tránh đợi vô hạn; xác suất trùng rất thấp
            for (int i = 0; i < 50; i++)
            {
                var code = NewMa(len);
                var exists = await _context.DonVienChuc.AnyAsync(d => d.MaTraCuu == code);
                if (!exists) return code;
            }
            // fallback – gần như không bao giờ tới
            return Guid.NewGuid().ToString("N")[..len].ToUpperInvariant();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult VienChuc(int? selectedChucDanhId = null)
        {
            ViewBag.LoaiVanBangOptions = GetLoaiVanBangOptions();

            ViewBag.ChucDanhList = new SelectList(
                _context.DanhMucChucDanhDuTuyen.Where(x => x.TamNgung == 0).AsNoTracking().ToList(),
                "ChucDanhId", "TenChucDanh", selectedChucDanhId
            );

            ViewBag.ViTriList = selectedChucDanhId.HasValue
                ? new SelectList(
                    _context.DanhMucViTriDuTuyen.Where(v => v.ChucDanhId == selectedChucDanhId && v.TamNgung == 0)
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

        public IActionResult NguoiLaoDong()
        {
            var model = new UngTuyenNguoiLaoDongViewModel();
            ViewBag.KhoaPhongList = new SelectList(
                _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                "KhoaPhongId", "Ten"
            );
            return View(model); // ==> Views/UngTuyen/NguoiLaoDong.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UngTuyenNguoiLaoDong(UngTuyenNguoiLaoDongViewModel model)
        {
            ModelState.Remove("UngVien.NgayUngTuyen");
            ModelState.Remove("HopDongNguoiLaoDong.NgayNop");
            ModelState.Remove("HopDongNguoiLaoDong.UngVienId");
            ModelState.Remove("HopDongNguoiLaoDong.MaTraCuu");
            ModelState.Remove("HopDongNguoiLaoDong.Loai");
            ModelState.Remove("HopDongNguoiLaoDong.UngVien"); // tránh validate navigation

            if (!ModelState.IsValid)
            {
                ViewBag.KhoaPhongList = new SelectList(
                    _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                    "KhoaPhongId", "Ten", model.HopDongNguoiLaoDong?.KhoaPhongCongTacId
                );
                return View("NguoiLaoDong", model);
            }

            try
            {
                // 1) Lấy hoặc tạo UngVien theo Email (KHÔNG tạo trùng)
                var ungVien = await GetOrCreateUngVienByEmailAsync(model.UngVien);

                // 2) Không cho tạo 2 hợp đồng cho cùng 1 ứng viên
                if (await _context.HopDongNguoiLaoDong.AnyAsync(h => h.UngVienId == ungVien.UngVienId))
                {
                    ModelState.AddModelError("", "Email này đã có hồ sơ người lao động.");
                    ViewBag.KhoaPhongList = new SelectList(
                        _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                        "KhoaPhongId", "Ten", model.HopDongNguoiLaoDong?.KhoaPhongCongTacId
                    );
                    return View("NguoiLaoDong", model);
                }

                // 3) Lưu hợp đồng
                var hd = model.HopDongNguoiLaoDong;
                hd.UngVienId = ungVien.UngVienId;
                hd.NgayNop = DateTime.Now;
                hd.TrangThai = 1;                       // mới nộp
                hd.Loai = "Người lao động";
                hd.MaTraCuu = await GenerateUniqueMaTraCuuAsync(6);

                _context.HopDongNguoiLaoDong.Add(hd);
                await _context.SaveChangesAsync();

                return RedirectToAction("ThanhCong");
            }
            catch (DbUpdateException ex)
            {
                // Nếu lỡ race-condition tạo trùng, rớt vào đây:
                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                ViewBag.KhoaPhongList = new SelectList(
                    _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                    "KhoaPhongId", "Ten", model.HopDongNguoiLaoDong?.KhoaPhongCongTacId
                );
                return View("NguoiLaoDong", model);
            }
        }

        // Trang thông báo nộp đơn thành công
        public IActionResult ThanhCong()
        {
            return View();
        }

        // Controller (POST): Xử lý form ứng tuyển viên chức

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UngTuyenVienChuc(UngTuyenVienChucViewModel model)
        {
            // --- Sinh các giá trị server-generated TRƯỚC khi validate ---
            var now = DateTime.Now;
            if (model.UngVien == null) model.UngVien = new UngVien();
            if (model.DonVienChuc == null) model.DonVienChuc = new DonVienChuc();

            model.UngVien.NgayUngTuyen = now;                             // [Required]
            model.DonVienChuc.NgayNop = now;                             // [Required]
            model.DonVienChuc.TrangThai = 1;                               // 1: mới nộp
            model.DonVienChuc.MaTraCuu = await GenerateUniqueMaTraCuuAsync(6); // [Required]

            // Nếu người dùng chọn "Khác" cho TĐVH/TTSK thì ép giá trị từ textbox
            var tdvhKhac = (Request.Form["TrinhDoVanHoaKhac"].ToString() ?? "").Trim();
            if (string.Equals(model.DonVienChuc.TrinhDoVanHoa, "Khác", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(tdvhKhac))
                model.DonVienChuc.TrinhDoVanHoa = tdvhKhac;

            var skKhac = (Request.Form["TinhTrangSucKhoeKhac"].ToString() ?? "").Trim();
            if (string.Equals(model.UngVien.TinhTrangSucKhoe, "Khác", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(skKhac))
                model.UngVien.TinhTrangSucKhoe = skKhac;

            // --- Loại các key server-generated khỏi ModelState để không bị Required phàn nàn ---
            ModelState.Remove("UngVien.NgayUngTuyen");
            ModelState.Remove("DonVienChuc.NgayNop");
            ModelState.Remove("DonVienChuc.MaTraCuu");
            ModelState.Remove("DonVienChuc.UngVienId"); // sẽ set sau khi lưu UngVien

            // Nếu bạn có đổi từ ChucDanhDuTuyen -> ChucDanhDuTuyenId, nhớ remove key cũ nếu tồn tại
            ModelState.Remove("DonVienChuc.ChucDanhDuTuyen");

            // --- (Tuỳ chọn) re-validate toàn model sau khi đã gán các giá trị ---
            // ModelState.Clear();
            // TryValidateModel(model);

            // Validate bổ sung cho các Id chọn từ combobox
            if (model.DonVienChuc.ChucDanhDuTuyenId <= 0)
                ModelState.AddModelError("DonVienChuc.ChucDanhDuTuyenId", "Vui lòng chọn chức danh.");
            if (model.DonVienChuc.ViTriDuTuyenId <= 0)
                ModelState.AddModelError("DonVienChuc.ViTriDuTuyenId", "Vui lòng chọn vị trí.");
            if (model.DonVienChuc.KhoaPhongId <= 0)
                ModelState.AddModelError("DonVienChuc.KhoaPhongId", "Vui lòng chọn khoa/phòng.");

            if (!ModelState.IsValid)
            {
                // Refill ViewBag... (giữ nguyên như bạn đang làm)
                ViewBag.LoaiVanBangOptions = GetLoaiVanBangOptions();
                ViewBag.ChucDanhList = new SelectList(
                    _context.DanhMucChucDanhDuTuyen.AsNoTracking().ToList(),
                    "ChucDanhId", "TenChucDanh", model.DonVienChuc?.ChucDanhDuTuyenId);

                var cdId = model.DonVienChuc?.ChucDanhDuTuyenId;
                ViewBag.ViTriList = cdId.HasValue
                    ? new SelectList(
                        _context.DanhMucViTriDuTuyen.Where(v => v.ChucDanhId == cdId.Value).AsNoTracking().ToList(),
                        "ViTriId", "TenViTri", model.DonVienChuc?.ViTriDuTuyenId)
                    : new SelectList(Enumerable.Empty<SelectListItem>());

                ViewBag.KhoaPhongList = new SelectList(Enumerable.Empty<SelectListItem>());
                return View("VienChuc", model);
            }

            // --- LƯU ---
            var danhSachVanBang = model.VanBangs ?? new List<VanBang>();

            // (1) Lấy hoặc tạo UngVien theo Email (KHÔNG tạo trùng)
            var ungVien = await GetOrCreateUngVienByEmailAsync(model.UngVien);

            // (2) Không cho nộp ĐƠN VIÊN CHỨC lần 2 cho cùng Ứng viên
            if (await _context.DonVienChuc.AnyAsync(d => d.UngVienId == ungVien.UngVienId))
            {
                ModelState.AddModelError("", "Email này đã có hồ sơ VIÊN CHỨC.");
                // Refill ViewBag như nhánh !ModelState.IsValid
                ViewBag.LoaiVanBangOptions = GetLoaiVanBangOptions();
                ViewBag.ChucDanhList = new SelectList(
                    _context.DanhMucChucDanhDuTuyen.AsNoTracking().ToList(),
                    "ChucDanhId", "TenChucDanh", model.DonVienChuc?.ChucDanhDuTuyenId);

                var cdId = model.DonVienChuc?.ChucDanhDuTuyenId;
                ViewBag.ViTriList = cdId.HasValue
                    ? new SelectList(
                        _context.DanhMucViTriDuTuyen.Where(v => v.ChucDanhId == cdId.Value)
                                .AsNoTracking().ToList(),
                        "ViTriId", "TenViTri", model.DonVienChuc?.ViTriDuTuyenId)
                    : new SelectList(Enumerable.Empty<SelectListItem>());

                ViewBag.KhoaPhongList = new SelectList(Enumerable.Empty<SelectListItem>());
                return View("VienChuc", model);
            }

            // (3) Lưu Đơn Viên Chức
            model.DonVienChuc.UngVienId = ungVien.UngVienId;
            _context.DonVienChuc.Add(model.DonVienChuc);
            await _context.SaveChangesAsync();

            // (4) Lưu Văn Bằng (nếu có)
            if (danhSachVanBang.Count > 0)
            {
                foreach (var vb in danhSachVanBang)
                    vb.DonVienChucId = model.DonVienChuc.VienChucId;
                _context.VanBang.AddRange(danhSachVanBang);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ThanhCong");
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

        private List<string> GetLoaiVanBangOptions()
        {
            return new List<string>
            {
                "Tiến sĩ",
                "Thạc sĩ",
                "Cử nhân",
                "Tin học",
                "Ngoại ngữ",
                "Chứng chỉ chuyên môn",
                "Khác"
            };
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { ok = false, message = "Vui lòng nhập Email." });

            email = email.Trim();
            var pattern = @"^[^@\s]+@gmail\.com$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return Json(new { ok = false, message = "Sai định dạng (chỉ chấp nhận @gmail.com)." });

            var ungVien = await _context.UngVien
                .Include(u => u.HopDongNguoiLaoDong)
                .Include(u => u.DonVienChuc) // <<< thêm cho trang Viên chức
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (ungVien == null)
                return Json(new { ok = true, exists = false, hasUngVien = false });

            return Json(new
            {
                ok = true,
                exists = true,
                hasUngVien = true,                                      // cho VC
                hasHopDong = ungVien.HopDongNguoiLaoDong != null,       // cho NLD
                hasDonVienChuc = ungVien.DonVienChuc != null,           // cho VC
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
                }
            });
        }

        private async Task<UngVien> GetOrCreateUngVienByEmailAsync(UngVien posted)
        {
            var email = (posted.Email ?? "").Trim();
            var existing = await _context.UngVien.FirstOrDefaultAsync(u => u.Email == email);

            if (existing != null)
            {
                // (tuỳ chọn) cập nhật mềm vài trường
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
    }
}
