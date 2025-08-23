using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    public class HomeAdminController : Controller
    {
        private readonly AdminDbContext _context;  

        public HomeAdminController(AdminDbContext context) 
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role);

            ViewBag.Username = username;
            ViewBag.Role = role;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);
            var lastDayLastMonth = firstDayThisMonth.AddDays(-1);
            var from30 = today.AddDays(-29); // gồm cả hôm nay => 30 điểm

            // ===== Cards =====
            var totalCandidates = await _context.UngViens.CountAsync();

            var newToday = await _context.UngViens
                .CountAsync(u => u.NgayUngTuyen >= today && u.NgayUngTuyen < tomorrow);

            var newYesterday = await _context.UngViens
                .CountAsync(u => u.NgayUngTuyen >= today.AddDays(-1) && u.NgayUngTuyen < today);

            var newThisMonth = await _context.UngViens
                .CountAsync(u => u.NgayUngTuyen >= firstDayThisMonth && u.NgayUngTuyen < tomorrow);

            var newLastMonth = await _context.UngViens
                .CountAsync(u => u.NgayUngTuyen >= firstDayLastMonth && u.NgayUngTuyen <= lastDayLastMonth);

            decimal DoD(decimal cur, decimal prev)
                => prev <= 0 ? (cur > 0 ? 100m : 0m) : Math.Round((cur - prev) * 100m / prev, 1);

            var dod = DoD(newToday, newYesterday);
            var mom = DoD(newThisMonth, newLastMonth);

            // ===== 30 ngày gần nhất =====
            var raw30 = await _context.UngViens
                .Where(u => u.NgayUngTuyen >= from30 && u.NgayUngTuyen <= today)
                .GroupBy(u => u.NgayUngTuyen)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            var series = new List<DayPoint>();
            for (int i = 0; i < 30; i++)
            {
                var d = from30.AddDays(i).Date;
                var item = raw30.FirstOrDefault(x => x.Day == d);
                series.Add(new DayPoint { Day = d.ToString("dd/MM"), Count = item?.Count ?? 0 });
            }

            // ===== Giới tính =====
            var female = await _context.UngViens.CountAsync(u => u.GioiTinh == 1);
            var male = totalCandidates - female;

            // ===== Nhóm tuổi =====
            var dobList = await _context.UngViens.Select(u => u.NgaySinh).ToListAsync();

            int Age(DateTime dob)
            {
                var a = today.Year - dob.Year;
                if (dob.Date > today.AddYears(-a)) a--;
                return a;
            }

            var ages = dobList.Select(Age).ToList();
            var buckets = new List<Bucket>
            {
                new Bucket { Label = "≤25",   Count = ages.Count(a => a <= 25) },
                new Bucket { Label = "26–35", Count = ages.Count(a => a >= 26 && a <= 35) },
                new Bucket { Label = "36–45", Count = ages.Count(a => a >= 36 && a <= 45) },
                new Bucket { Label = "46–60", Count = ages.Count(a => a >= 46 && a <= 60) },
                new Bucket { Label = ">60",   Count = ages.Count(a => a > 60) },
            };

            // ===== 10 ứng viên gần đây =====
            var recents = await _context.UngViens
                .OrderByDescending(u => u.NgayUngTuyen)
                .Take(10)
                .Select(u => new RecentCandidateVm
                {
                    UngVienId = u.UngVienId,
                    HoTen = u.HoTen,
                    Email = u.Email,
                    NgayUngTuyen = u.NgayUngTuyen
                })
                .ToListAsync();

            var vm = new DashboardVm
            {
                TotalCandidates = totalCandidates,
                NewCandidatesToday = newToday,
                NewCandidatesThisMonth = newThisMonth,
                DoDNewCandidatesPct = dod,
                MoMNewCandidatesPct = mom,
                Last30DaysSeries = series,
                MaleCount = male,
                FemaleCount = female,
                AgeBuckets = buckets,
                RecentCandidates = recents
            };

            return View(vm);
        }
    }
}
