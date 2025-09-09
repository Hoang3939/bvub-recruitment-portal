using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class ReportsController : Controller
    {
        private readonly AdminDbContext _ctx;
        public ReportsController(AdminDbContext ctx) => _ctx = ctx;

        public IActionResult Index() => View();

        // SUMMARY
        [HttpGet]
        public async Task<IActionResult> Summary(DateTime? from, DateTime? to, int soonDays = 14)
        {
            var today = DateTime.Today;
            var start = (from ?? today.AddDays(-30)).Date;
            var end = (to ?? today).Date;

            var qRec = _ctx.ThongTinTuyenDungs.AsNoTracking();

            var totalRec = await qRec.CountAsync();
            var activeRec = await qRec.CountAsync(x => (int)x.TrangThai == 1 && x.HanNopHoSo >= today);
            var soon = await qRec
                .Where(x => (int)x.TrangThai == 1 && x.HanNopHoSo >= today && x.HanNopHoSo <= today.AddDays(soonDays))
                .CountAsync();

            var byTypeVC = await qRec.CountAsync(x =>
                EF.Functions.Like(x.LoaiTuyenDung, "%viên chức%") ||
                EF.Functions.Like(x.LoaiTuyenDung, "%vien chuc%"));
            var byTypeHD = await qRec.CountAsync(x =>
                EF.Functions.Like(x.LoaiTuyenDung, "%hđ%") ||
                EF.Functions.Like(x.LoaiTuyenDung, "%hợp đồng%") ||
                EF.Functions.Like(x.LoaiTuyenDung, "%lao động%"));

            var top = await qRec
                .Where(x => (int)x.TrangThai == 1 && x.HanNopHoSo >= today)
                .OrderBy(x => x.HanNopHoSo)
                .Take(5)
                .Select(x => new ExpiringItemVm
                {
                    Id = x.TuyenDungId,
                    TieuDe = x.TieuDe,
                    Loai = x.LoaiTuyenDung,
                    Han = x.HanNopHoSo,
                    DaysLeft = EF.Functions.DateDiffDay(today, x.HanNopHoSo)
                })
                .ToListAsync();

            // NỐI NGUỒN ỨNG VIÊN TẠI ĐÂY 
            int pending = 0, newCands = 0;

            var cand = _ctx.DonVienChucs.Select(d => new { d.NgayNop, d.TrangThai, DonType = "VC" })
                       .Concat(_ctx.HopDongNguoiLaoDongs.Select(d => new { d.NgayNop, d.TrangThai, DonType = "HD" }));
            pending = await cand.CountAsync(x => x.TrangThai == 1);
            newCands = await cand.CountAsync(x => x.NgayNop >= start && x.NgayNop <= end);

            return Json(new SummaryVm
            {
                PendingApplications = pending,
                ActiveRecruitments = activeRec,
                ExpiringSoonRecruitments = soon,
                TotalRecruitments = totalRec,
                ByTypeVC = byTypeVC,
                ByTypeHD = byTypeHD,
                NewCandidates = newCands,
                ExpiringTop = top
            });
        }

        // RECRUITMENT: daily line
        [HttpGet]
        public async Task<IActionResult> RecruitmentSeries(DateTime? from, DateTime? to)
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date;

            var raw = await _ctx.ThongTinTuyenDungs.AsNoTracking()
                .Where(x => x.NgayDang >= start && x.NgayDang <= end)
                .GroupBy(x => x.NgayDang)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<int>();
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                labels.Add(d.ToString("dd/MM"));
                data.Add(raw.FirstOrDefault(x => x.Date == d)?.Count ?? 0);
            }
            return Json(new SeriesVm { Labels = labels, Data = data });
        }

        // RECRUITMENT: monthly bar VC/HD
        [HttpGet]
        public async Task<IActionResult> RecruitmentByTypeMonthly(DateTime? from, DateTime? to)
        {
            var s = (from ?? DateTime.Today.AddMonths(-5)).Date;
            var e = (to ?? DateTime.Today).Date;

            var rec = await _ctx.ThongTinTuyenDungs.AsNoTracking()
                .Where(x => x.NgayDang >= s && x.NgayDang <= e)
                .Select(x => new
                {
                    Month = new DateTime(x.NgayDang.Year, x.NgayDang.Month, 1),
                    IsVC = EF.Functions.Like(x.LoaiTuyenDung, "%viên chức%") || EF.Functions.Like(x.LoaiTuyenDung, "%vien chuc%")
                })
                .ToListAsync();

            var labels = new List<string>();
            var vc = new List<int>();
            var hd = new List<int>();

            var cur = new DateTime(s.Year, s.Month, 1);
            var end = new DateTime(e.Year, e.Month, 1);
            while (cur <= end)
            {
                labels.Add(cur.ToString("MM/yyyy"));
                var m = rec.Where(x => x.Month == cur);
                vc.Add(m.Count(x => x.IsVC));
                hd.Add(m.Count(x => !x.IsVC));
                cur = cur.AddMonths(1);
            }
            return Json(new PairSeriesVm { Labels = labels, A = vc, B = hd });
        }

        // CANDIDATE: daily line vc/hd 
        [HttpGet]
        public async Task<IActionResult> CandidateSeries(DateTime? from, DateTime? to)
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date;

            var grouped = await _ctx.DonVienChucs
                .Where(d => d.NgayNop >= start && d.NgayNop <= end)
                .Select(d => new { Date = d.NgayNop.Date, DonType = "VC" })
                .Concat(
                    _ctx.HopDongNguoiLaoDongs
                        .Where(d => d.NgayNop >= start && d.NgayNop <= end)
                        .Select(d => new { Date = d.NgayNop.Date, DonType = "HD" })
                )
                .GroupBy(x => x.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    VC = g.Count(x => x.DonType == "VC"),
                    HD = g.Count(x => x.DonType == "HD")
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var labels = new List<string>();
            var vc = new List<int>();
            var hd = new List<int>();

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                labels.Add(d.ToString("dd/MM"));
                var row = grouped.FirstOrDefault(x => x.Date == d);
                vc.Add(row?.VC ?? 0);
                hd.Add(row?.HD ?? 0);
            }

            // Trả về chỉ VC/HD, không có Total
            return Json(new CandidateSeriesVm { Labels = labels, VC = vc, HD = hd });
        }

        // CANDIDATE: breakdown donut + type bars
        [HttpGet]
        public async Task<IActionResult> CandidateBreakdown(DateTime? from, DateTime? to)
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date;

            var baseQ = _ctx.DonVienChucs
                .Select(d => new { d.NgayNop, d.TrangThai, DonType = "VC" })
                .Concat(_ctx.HopDongNguoiLaoDongs
                    .Select(d => new { d.NgayNop, d.TrangThai, DonType = "HD" }))
                .Where(x => x.NgayNop >= start && x.NgayNop <= end);

            // Quy ước trạng thái: 1 = chờ duyệt, 2 = đã duyệt, 3 = đã huỷ 
            var total = await baseQ.CountAsync();
            var pending = await baseQ.CountAsync(x => x.TrangThai == 1);
            var approved = await baseQ.CountAsync(x => x.TrangThai == 2);
            var cancelled = await baseQ.CountAsync(x => x.TrangThai == 3);
            var vc = await baseQ.CountAsync(x => x.DonType == "VC");
            var hd = await baseQ.CountAsync(x => x.DonType == "HD");

            return Json(new CandidateBreakdownVm
            {
                Pending = pending,
                Approved = approved,
                Cancelled = cancelled,
                Total = total,
                VC = vc,
                HD = hd
            });
        }
    }
}
