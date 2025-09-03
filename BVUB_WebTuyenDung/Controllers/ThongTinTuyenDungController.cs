using BVUB_WebTuyenDung.Data;
using BVUB_WebTuyenDung.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BVUB_WebTuyenDung.Controllers
{
    public class ThongTinTuyenDungController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ThongTinTuyenDungController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index(string? loai, string? q, string? sort, int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var query = _db.ThongTinTuyenDung
                           .AsNoTracking()
                           .Where(x => x.TrangThai == 1);               // chỉ đang tuyển

            // lọc loại
            if (!string.IsNullOrWhiteSpace(loai))
            {
                if (loai.Equals("VC", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(x => x.LoaiTuyenDung == "VC" || x.LoaiTuyenDung == "VienChuc" || x.LoaiTuyenDung == "Viên chức");
                else if (loai.Equals("NLD", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(x => x.LoaiTuyenDung == "NLD" || x.LoaiTuyenDung == "NguoiLaoDong" || x.LoaiTuyenDung == "Người lao động");
            }

            // tìm kiếm
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(x =>
                    (x.TieuDe ?? "").Contains(kw) || (x.NoiDung ?? "").Contains(kw)
                );
            }

            // sắp xếp
            sort = string.IsNullOrWhiteSpace(sort) ? "moi-nhat" : sort;
            query = sort switch
            {
                "sap-het-han" => query.OrderBy(x => x.HanNopHoSo).ThenByDescending(x => x.NgayDang),
                _ => query.OrderByDescending(x => x.NgayDang)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            var vm = new TuyenDungListViewModel
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Loai = loai,
                Q = q,
                Sort = sort
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var item = await _db.ThongTinTuyenDung
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x => x.TuyenDungId == id);
            if (item == null) return NotFound();

            return View("ChiTiet", item);
        }

    }
}
