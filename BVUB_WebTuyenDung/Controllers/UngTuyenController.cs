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
            // các field server-generated: loại bỏ để tránh Required phàn nàn trước khi gán
            ModelState.Remove("UngVien.NgayUngTuyen");
            ModelState.Remove("HopDongNguoiLaoDong.NgayNop");
            ModelState.Remove("HopDongNguoiLaoDong.UngVienId");
            ModelState.Remove("HopDongNguoiLaoDong.MaTraCuu");
            ModelState.Remove("HopDongNguoiLaoDong.Loai");

            if (!ModelState.IsValid)
            {
                // refill dropdown trước khi trả view
                ViewBag.KhoaPhongList = new SelectList(
                    _context.DanhMucKhoaPhong.Where(k => k.TamNgung == 0).AsNoTracking().ToList(),
                    "KhoaPhongId", "Ten", model.HopDongNguoiLaoDong?.KhoaPhongCongTacId
                );
                return View("NguoiLaoDong", model); // <<< QUAN TRỌNG
            }

            var now = DateTime.Now;

            // Ứng viên
            model.UngVien.NgayUngTuyen = now;
            _context.UngVien.Add(model.UngVien);
            await _context.SaveChangesAsync();

            // Hợp đồng NLD
            model.HopDongNguoiLaoDong.UngVienId = model.UngVien.UngVienId;
            model.HopDongNguoiLaoDong.NgayNop = now;
            model.HopDongNguoiLaoDong.TrangThai = 1;                  // mới nộp
            model.HopDongNguoiLaoDong.Loai = "Người lao động";   // ép loại
            model.HopDongNguoiLaoDong.MaTraCuu = await GenerateUniqueMaTraCuuAsync(6);

            _context.HopDongNguoiLaoDong.Add(model.HopDongNguoiLaoDong);
            await _context.SaveChangesAsync();

            return RedirectToAction("ThanhCong");
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

            _context.UngVien.Add(model.UngVien);
            await _context.SaveChangesAsync();

            model.DonVienChuc.UngVienId = model.UngVien.UngVienId;
            _context.DonVienChuc.Add(model.DonVienChuc);
            await _context.SaveChangesAsync();

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

            // chỉ cho @gmail.com (giữ nguyên nếu bạn muốn nới lỏng)
            var pattern = @"^[^@\s]+@gmail\.com$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    email, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return Json(new { ok = false, message = "Sai định dạng (chỉ chấp nhận @gmail.com)." });
            }

            var ungVien = await _context.UngVien
                .Include(u => u.HopDongNguoiLaoDong)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);   // ❗ DÙNG Email

            if (ungVien == null)
                return Json(new { ok = true, exists = false });

            return Json(new
            {
                ok = true,
                exists = true,
                hasHopDong = ungVien.HopDongNguoiLaoDong != null,
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
    }
}
