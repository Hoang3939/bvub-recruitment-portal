using BVUB_WebTuyenDung.Data;
using BVUB_WebTuyenDung.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class HuongDanController : Controller
{
    private readonly ApplicationDbContext _db;
    public HuongDanController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Xem(string loai)
    {
        if (string.IsNullOrWhiteSpace(loai)) return RedirectToAction(nameof(Index));
        var up = loai.Trim().ToUpperInvariant();

        // map tham số sang giá trị trong DB
        IQueryable<HuongDanDangKy> q = _db.HuongDanDangKy.AsNoTracking();
        if (up is "VC" or "VIENCHUC" or "VIÊN CHỨC")
            q = q.Where(x => x.LoaiHuongDan == "Viên chức");
        else if (up is "NLD" or "NGUOILAODONG" or "NGƯỜI LAO ĐỘNG")
            q = q.Where(x => x.LoaiHuongDan == "Người lao động");
        else
            return RedirectToAction(nameof(Index));

        var model = await q.OrderByDescending(x => x.NgayCapNhat).FirstOrDefaultAsync();
        return View("~/Views/HuongDan/Xem.cshtml", model);
    }
}
