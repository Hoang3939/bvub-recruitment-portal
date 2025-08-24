// Controllers/TraCuuController.cs
using BVUB_WebTuyenDung.Data;
using BVUB_WebTuyenDung.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.Fonts;
// ImageSharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Controllers
{
    public class TraCuuController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string CaptchaCodeKey = "TC_CAPTCHA_CODE";
        private const string CaptchaTimeKey = "TC_CAPTCHA_TIME";

        public TraCuuController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Index() => View(new TraCuuViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TraCuuViewModel vm)
        {
            // 1) Validate captcha (hết hạn 2 phút)
            var code = HttpContext.Session.GetString(CaptchaCodeKey);
            var ticks = HttpContext.Session.GetString(CaptchaTimeKey);
            var expired = true;
            if (!string.IsNullOrEmpty(ticks) && long.TryParse(ticks, out var t))
            {
                var genAt = new DateTimeOffset(t, TimeSpan.Zero);
                expired = (DateTimeOffset.UtcNow - genAt) > TimeSpan.FromMinutes(2);
            }
            if (string.IsNullOrEmpty(code) || expired ||
                !string.Equals(vm.CaptchaInput?.Trim(), code, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(vm.CaptchaInput),
                    expired ? "Captcha đã hết hạn, vui lòng đổi mã." : "Captcha không đúng.");
            }
            if (!ModelState.IsValid) return View(vm);

            // 2) Chuẩn hoá input
            var ma = (vm.MaTraCuu ?? string.Empty).Trim();
            var gt = (vm.GiaTri ?? string.Empty).Trim();
            if (ma.Length == 0 || gt.Length == 0) return View(vm);

            // 3) Lọc ứng viên theo định danh
            IQueryable<UngVien> ungVienMatch = _db.UngVien.AsNoTracking();
            switch (vm.DinhDanh)
            {
                case DinhDanhType.CCCD: ungVienMatch = ungVienMatch.Where(u => u.CCCD == gt); break;
                case DinhDanhType.Gmail: ungVienMatch = ungVienMatch.Where(u => u.Email == gt); break;
                case DinhDanhType.SDT: ungVienMatch = ungVienMatch.Where(u => u.SoDienThoai == gt); break;
            }

            // 4) Tra cứu DonVienChuc theo UngVienId + MaTraCuu
            var vcQuery =
                from d in _db.DonVienChuc.AsNoTracking()
                join u in ungVienMatch on d.UngVienId equals u.UngVienId
                where d.MaTraCuu == ma
                select new
                {
                    Id = d.VienChucId,
                    TenDon = "Đơn ứng tuyển viên chức",
                    TrangThai = d.TrangThai,   // giữ là int
                    Loai = "VC"
                };

            // 5) Tra cứu HopDongNguoiLaoDong theo UngVienId + MaTraCuu
            var nldQuery =
                from h in _db.HopDongNguoiLaoDong.AsNoTracking()
                join u in ungVienMatch on h.UngVienId equals u.UngVienId
                where h.MaTraCuu == ma
                select new
                {
                    Id = h.HopDongId,
                    TenDon = "Đơn ứng tuyển người lao động",
                    TrangThai = h.TrangThai,   // giữ là int
                    Loai = "NLD"
                };

            var raw = await vcQuery.Concat(nldQuery).ToListAsync();
            vm.KetQua = raw.Select(x => new TraCuuRow
            {
                Id = x.Id,
                TenDon = x.TenDon,
                TrangThai = TrangThaiText(x.TrangThai),
                Loai = x.Loai
            }).ToList();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string loai, int id)
        {
            if (string.Equals(loai, "VC", StringComparison.OrdinalIgnoreCase))
            {
                var d = await _db.DonVienChuc
                    .Include(x => x.UngVien)
                    .Include(x => x.VanBangs)
                    .Include(x => x.ChucDanhDuTuyen)
                    .Include(x => x.ViTriDuTuyen)
                    .Include(x => x.KhoaPhong)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.VienChucId == id);

                if (d == null) return NotFound();

                var vm = new VienChucDetailsVM
                {
                    VienChucId = d.VienChucId,
                    MaTraCuu = d.MaTraCuu,
                    TrangThai = d.TrangThai,
                    NgayNop = d.NgayNop,
                    TenChucDanh = d.ChucDanhDuTuyen?.TenChucDanh ?? "(Chưa rõ tên chức danh)",
                    TenViTri = d.ViTriDuTuyen?.TenViTri ?? "(Chưa rõ tên vị trí)", // nếu model ViTri dùng tên property khác, sửa tại đây
                    TenKhoaPhong = d.KhoaPhong?.Ten ?? "(Chưa rõ tên khoa/phòng)",
                    UngVien = d.UngVien!,
                    VanBangs = d.VanBangs?.Select(v => new VanBangRow
                    {
                        TenCoSo = v.TenCoSo,
                        NgayCap = v.NgayCap,
                        SoHieu = v.SoHieu,
                        ChuyenNganhDaoTao = v.ChuyenNganhDaoTao,
                        NganhDaoTao = v.NganhDaoTao,
                        HinhThucDaoTao = v.HinhThucDaoTao,
                        XepLoai = v.XepLoai
                    }).ToList() ?? new()
                };

                return View("ChiTietVienChuc", vm);
            }
            else if (string.Equals(loai, "NLD", StringComparison.OrdinalIgnoreCase))
            {
                var h = await _db.HopDongNguoiLaoDong
                    .Include(x => x.UngVien)
                    .Include(x => x.KhoaPhongCongTac)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.HopDongId == id);

                if (h == null) return NotFound();

                var vm = new NguoiLaoDongDetailsVM
                {
                    HopDongId = h.HopDongId,
                    MaTraCuu = h.MaTraCuu,
                    TrangThai = h.TrangThai,
                    NgayNop = h.NgayNop,
                    LoaiHopDong = h.Loai,
                    TenKhoaPhongCongTac = h.KhoaPhongCongTac?.Ten ?? "(Chưa rõ tên khoa/phòng)",
                    ChuyenNganhDaoTao = h.ChuyenNganhDaoTao,
                    NamTotNghiep = h.NamTotNghiep,
                    TrinhDoTinHoc = h.TrinhDoTinHoc,
                    TrinhDoNgoaiNgu = h.TrinhDoNgoaiNgu,
                    ChungChiHanhNghe = h.ChungChiHanhNghe,
                    NgheNghiepTruocTuyenDung = h.NgheNghiepTruocTuyenDung,
                    UngVien = h.UngVien!
                };

                return View("ChiTietNguoiLaoDong", vm);
            }

            return NotFound();
        }

        // Ảnh Captcha
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Captcha()
        {
            // sinh mã 4–5 ký tự (bỏ I/O/0/1)
            var rnd = new Random();
            var len = rnd.Next(4, 6);
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var sb = new StringBuilder();
            for (int i = 0; i < len; i++) sb.Append(alphabet[rnd.Next(alphabet.Length)]);
            var cap = sb.ToString();

            HttpContext.Session.SetString(CaptchaCodeKey, cap);
            HttpContext.Session.SetString(CaptchaTimeKey, DateTimeOffset.UtcNow.Ticks.ToString());

            // vẽ PNG
            const int W = 160, H = 56;
            using var img = new Image<Rgba32>(W, H, Color.White);
            var baseFont = SystemFonts.CreateFont("Arial", 28, FontStyle.Bold); // nếu máy chủ thiếu Arial: bundle .ttf riêng

            img.Mutate(ctx =>
            {
                // nhiễu đường
                for (int i = 0; i < 16; i++)
                {
                    var p1 = new PointF(rnd.Next(W), rnd.Next(H));
                    var p2 = new PointF(rnd.Next(W), rnd.Next(H));
                    ctx.Draw(Color.FromRgb((byte)rnd.Next(160, 220), (byte)rnd.Next(160, 220), (byte)rnd.Next(160, 220)),
                             1f, new PathBuilder().AddLine(p1, p2).Build());
                }

                // chữ lệch vị trí
                float x = 15;
                for (int i = 0; i < cap.Length; i++)
                {
                    var ch = cap[i].ToString();
                    var size = rnd.Next(24, 32);
                    var f = new Font(baseFont, size);
                    var y = rnd.Next(6, 18);
                    ctx.DrawText(ch, f,
                        Color.FromRgb((byte)rnd.Next(0, 120), (byte)rnd.Next(0, 120), (byte)rnd.Next(0, 120)),
                        new PointF(x, y));
                    x += size - 2;
                }

                // chấm nhiễu
                for (int i = 0; i < 200; i++)
                    img[rnd.Next(W), rnd.Next(H)] = Color.FromRgb(
                        (byte)rnd.Next(200, 240), (byte)rnd.Next(200, 240), (byte)rnd.Next(200, 240));
            });

            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return File(ms.ToArray(), "image/png");
        }

        private static string TrangThaiText(int trangThai) => trangThai switch
        {
            1 => "Đang duyệt",
            2 => "Đã duyệt",
            3 => "Đã Hủy",
            _ => "Không xác định"
        };
    }

    public enum DinhDanhType { CCCD, Gmail, SDT }

    public class TraCuuRow
    {
        public int Id { get; set; }
        public string TenDon { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public string Loai { get; set; } = ""; // "VC" | "NLD"
    }

    public class TraCuuViewModel
    {
        public DinhDanhType DinhDanh { get; set; } = DinhDanhType.CCCD;
        public string GiaTri { get; set; } = "";
        public string MaTraCuu { get; set; } = "";
        public string CaptchaInput { get; set; } = "";
        public System.Collections.Generic.List<TraCuuRow> KetQua { get; set; } = new();
    }
}
