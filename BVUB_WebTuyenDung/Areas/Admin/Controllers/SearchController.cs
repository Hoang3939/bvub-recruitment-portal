using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BVUB_WebTuyenDung.Areas.Admin.Data;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class SearchController : Controller
    {
        private readonly AdminDbContext _ctx;
        public SearchController(AdminDbContext ctx) => _ctx = ctx;

        public class ResultItem
        {
            public string Type { get; set; } = "";
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Subtitle { get; set; } = "";
            public string Url { get; set; } = "";
        }

        public class SearchVm
        {
            public string Query { get; set; } = "";
            public List<ResultItem> Results { get; set; } = new();
            public Dictionary<string, List<ResultItem>> Grouped =>
                Results.GroupBy(r => r.Type)
                       .OrderBy(g => g.Key)
                       .ToDictionary(g => g.Key, g => g.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Go(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return NotFound();

            q = q.Trim();
            bool looksEmail = q.Contains("@");
            bool isId = int.TryParse(q, out var idVal);

            // ===== 1) NHÂN VIÊN (AdminUsers) =====
            try
            {
                if (isId)
                {
                    var empById = await _ctx.AdminUsers.FirstOrDefaultAsync(x => x.AdminId == idVal);
                    if (empById != null)
                        return Json(new { url = Url.Action("Index", "Employee", new { area = "Admin", q }) });
                }

                var emp = await _ctx.AdminUsers
                    .Where(x =>
                        EF.Functions.Like(x.Username, $"%{q}%")
                    // || (looksEmail && EF.Functions.Like(x.Email ?? "", $"%{q}%")) // có Email thì bật
                    )
                    .OrderBy(x => x.Username)
                    .FirstOrDefaultAsync();

                if (emp != null)
                    return Json(new { url = Url.Action("Index", "Employee", new { area = "Admin", q }) });
            }
            catch { /* Đổi DbSet/thuộc tính theo dự án nếu khác */ }

            // ===== 2) ỨNG VIÊN =====
            try
            {
                if (isId)
                {
                    var uvById = await _ctx.UngViens.FirstOrDefaultAsync(x => x.UngVienId == idVal);
                    if (uvById != null)
                        return Json(new { url = Url.Action("Index", "Candidates", new { area = "Admin", q }) });
                }

                var uv = await _ctx.UngViens
                    .Where(x =>
                        EF.Functions.Like(x.HoTen, $"%{q}%") ||
                        (looksEmail && EF.Functions.Like(x.Email ?? "", $"%{q}%")) ||
                        EF.Functions.Like(x.SoDienThoai ?? "", $"%{q}%") ||
                        EF.Functions.Like(x.CCCD ?? "", $"%{q}%"))
                    .OrderBy(x => x.HoTen)
                    .FirstOrDefaultAsync();

                if (uv != null)
                    return Json(new { url = Url.Action("Index", "Candidates", new { area = "Admin", q }) });
            }
            catch { }

            // ===== 3) THÔNG TIN TUYỂN DỤNG =====
            try
            {
                if (isId)
                {
                    var recById = await _ctx.ThongTinTuyenDungs.FirstOrDefaultAsync(x => x.TuyenDungId == idVal);
                    if (recById != null)
                        return Json(new { url = Url.Action("Index", "Recruitments", new { area = "Admin", q }) });
                }

                var rec = await _ctx.ThongTinTuyenDungs
                    .Where(x =>
                        EF.Functions.Like(x.TieuDe, $"%{q}%") ||
                        EF.Functions.Like(x.NoiDung ?? "", $"%{q}%"))
                    .OrderBy(x => x.TieuDe)
                    .FirstOrDefaultAsync();

                if (rec != null)
                    return Json(new { url = Url.Action("Index", "Recruitments", new { area = "Admin", q }) });
            }
            catch { }

            // ===== 4) VỊ TRÍ DỰ TUYỂN =====
            try
            {
                if (isId)
                {
                    var posById = await _ctx.DanhMucViTriDuTuyens.FirstOrDefaultAsync(x => x.ViTriId == idVal);
                    if (posById != null)
                        return Json(new { url = Url.Action("Index", "Positions", new { area = "Admin", q }) });
                }

                var pos = await _ctx.DanhMucViTriDuTuyens
                    .Where(x => EF.Functions.Like(x.TenViTri, $"%{q}%"))
                    .OrderBy(x => x.TenViTri)
                    .FirstOrDefaultAsync();

                if (pos != null)
                    return Json(new { url = Url.Action("Index", "Positions", new { area = "Admin", q }) });
            }
            catch { }

            // ===== 5) KHOA PHÒNG =====
            try
            {
                if (isId)
                {
                    var depById = await _ctx.DanhMucKhoaPhongs.FirstOrDefaultAsync(x => x.KhoaPhongId == idVal);
                    if (depById != null)
                        return Json(new { url = Url.Action("Index", "Departments", new { area = "Admin", q }) });
                }

                var dep = await _ctx.DanhMucKhoaPhongs
                    .Where(x => EF.Functions.Like(x.Ten, $"%{q}%"))
                    .OrderBy(x => x.Ten)
                    .FirstOrDefaultAsync();

                if (dep != null)
                    return Json(new { url = Url.Action("Index", "Departments", new { area = "Admin", q }) });
            }
            catch { }

            // ===== 6) CHỨC DANH DỰ TUYỂN =====
            try
            {
                if (isId)
                {
                    var titleById = await _ctx.DanhMucChucDanhDuTuyens.FirstOrDefaultAsync(x => x.ChucDanhId == idVal);
                    if (titleById != null)
                        return Json(new { url = Url.Action("Index", "Titles", new { area = "Admin", q }) });
                }

                var title = await _ctx.DanhMucChucDanhDuTuyens
                    .Where(x => EF.Functions.Like(x.TenChucDanh, $"%{q}%"))
                    .OrderBy(x => x.TenChucDanh)
                    .FirstOrDefaultAsync();

                if (title != null)
                    return Json(new { url = Url.Action("Index", "Titles", new { area = "Admin", q }) });
            }
            catch { }

            // Không thấy gì
            return NotFound();
        }
    }
}
