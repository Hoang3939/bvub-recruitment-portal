using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class HomeAdminController : Controller
    {
        private readonly AdminDbContext _ctx;
        public HomeAdminController(AdminDbContext ctx) => _ctx = ctx;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Gộp toàn bộ "đơn còn tồn tại" thành 1 luồng có (UngVienId, AppDate?)
            // Ép NgayNop về DateTime? để có thể nối (concat) an toàn và kiểm tra null.
            var vcApps = _ctx.DonVienChucs.AsNoTracking()
                .Select(d => new { d.UngVienId, AppDate = (DateTime?)d.NgayNop });

            var hdApps = _ctx.HopDongNguoiLaoDongs.AsNoTracking()
                .Select(h => new { h.UngVienId, AppDate = (DateTime?)h.NgayNop });

            // Chỉ giữ những bản ghi có AppDate != null, sau đó project về DateTime (không nullable)
            var apps = vcApps.Concat(hdApps)
                .Where(a => a.AppDate.HasValue)
                .Select(a => new { a.UngVienId, AppDate = a.AppDate!.Value });

            // Tập UngVienId "còn hoạt động" = có ít nhất 1 đơn
            var activeCandidateIdsQuery = apps.Select(a => a.UngVienId).Distinct();

            // -------- Cards --------
            var totalCandidates = await activeCandidateIdsQuery.CountAsync();

            // Đếm distinct UngVien theo ngày dựa trên AppDate
            var todayCount = await apps
                .Where(a => a.AppDate >= today && a.AppDate < tomorrow)
                .Select(a => a.UngVienId)
                .Distinct()
                .CountAsync();

            var yesterday = today.AddDays(-1);
            var yesterdayCount = await apps
                .Where(a => a.AppDate >= yesterday && a.AppDate < today)
                .Select(a => a.UngVienId)
                .Distinct()
                .CountAsync();

            var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayNextMonth = firstDayThisMonth.AddMonths(1);

            var thisMonthCount = await apps
                .Where(a => a.AppDate >= firstDayThisMonth && a.AppDate < firstDayNextMonth)
                .Select(a => a.UngVienId)
                .Distinct()
                .CountAsync();

            var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);
            var lastMonthCount = await apps
                .Where(a => a.AppDate >= firstDayLastMonth && a.AppDate < firstDayThisMonth)
                .Select(a => a.UngVienId)
                .Distinct()
                .CountAsync();

            decimal DoD(decimal curr, decimal prev)
                => prev == 0 ? (curr > 0 ? 100 : 0) : Math.Round((curr - prev) / prev * 100, 2);

            // -------- Biểu đồ 30 ngày gần nhất --------
            var startDate = today.AddDays(-29);

            // Nhóm theo ngày (AppDate.Date) và đếm distinct UngVienId mỗi ngày
            var last30 = await apps
                .Where(a => a.AppDate >= startDate && a.AppDate < tomorrow)
                .GroupBy(a => a.AppDate.Date)
                .Select(g => new { Day = g.Key, Count = g.Select(x => x.UngVienId).Distinct().Count() })
                .ToListAsync();

            var series = Enumerable.Range(0, 30)
                .Select(i => startDate.AddDays(i))
                .Select(d => new DayPoint
                {
                    Day = d.ToString("dd/MM"),
                    Count = last30.FirstOrDefault(x => x.Day == d)?.Count ?? 0
                })
                .ToList();

            // -------- Giới tính (lọc theo activeCandidateIds) --------
            var female = await _ctx.UngViens.AsNoTracking()
                .Where(uv => activeCandidateIdsQuery.Contains(uv.UngVienId) && uv.GioiTinh == 1)
                .CountAsync();

            var male = await _ctx.UngViens.AsNoTracking()
                .Where(uv => activeCandidateIdsQuery.Contains(uv.UngVienId) && uv.GioiTinh != 1)
                .CountAsync();

            // -------- Nhóm tuổi (lọc theo activeCandidateIds) --------
            var todayLocal = today; // giữ nguyên mốc hôm nay
            int Age(DateTime dob)
            {
                var age = todayLocal.Year - dob.Year;
                if (dob.Date > todayLocal.AddYears(-age)) age--;
                return age;
            }

            var ages = await _ctx.UngViens.AsNoTracking()
                .Where(uv => activeCandidateIdsQuery.Contains(uv.UngVienId))
                .Select(uv => uv.NgaySinh)
                .ToListAsync();

            var buckets = new List<Bucket>
            {
                new Bucket { Label = "≤ 22",  Count = ages.Count(a => Age(a) <= 22) },
                new Bucket { Label = "23–30", Count = ages.Count(a => { var v = Age(a); return v >= 23 && v <= 30; }) },
                new Bucket { Label = "31–40", Count = ages.Count(a => { var v = Age(a); return v >= 31 && v <= 40; }) },
                new Bucket { Label = "41–50", Count = ages.Count(a => { var v = Age(a); return v >= 41 && v <= 50; }) },
                new Bucket { Label = "≥ 51",  Count = ages.Count(a => Age(a) >= 51) }
            };

            // -------- Hoạt động gần đây: lấy lần nộp đơn mới nhất của mỗi ứng viên --------
            var latestByCandidate = await apps
                .GroupBy(a => a.UngVienId)
                .Select(g => new { UngVienId = g.Key, Latest = g.Max(x => x.AppDate) })
                .OrderByDescending(x => x.Latest)
                .Take(15)
                .ToListAsync();

            // Join ra thông tin ứng viên
            var recentIds = latestByCandidate.Select(x => x.UngVienId).ToList();

            var recentUv = await _ctx.UngViens.AsNoTracking()
                .Where(uv => recentIds.Contains(uv.UngVienId))
                .Select(uv => new { uv.UngVienId, uv.HoTen, uv.Email })
                .ToListAsync();

            var recent = latestByCandidate
                .Join(recentUv, l => l.UngVienId, u => u.UngVienId, (l, u) => new RecentCandidateVm
                {
                    UngVienId = u.UngVienId,
                    HoTen = u.HoTen,
                    Email = u.Email,
                    NgayUngTuyen = l.Latest // đã là DateTime (không nullable)
                })
                .OrderByDescending(x => x.NgayUngTuyen)
                .ToList();

            // -------- ViewModel --------
            var vm = new DashboardVm
            {
                TotalCandidates = totalCandidates,
                NewCandidatesToday = todayCount,
                NewCandidatesThisMonth = thisMonthCount,
                DoDNewCandidatesPct = DoD(todayCount, yesterdayCount),
                MoMNewCandidatesPct = DoD(thisMonthCount, lastMonthCount),
                Last30DaysSeries = series,
                MaleCount = male,
                FemaleCount = female,
                AgeBuckets = buckets,
                RecentCandidates = recent
            };

            return View(vm);
        }
    }
}
