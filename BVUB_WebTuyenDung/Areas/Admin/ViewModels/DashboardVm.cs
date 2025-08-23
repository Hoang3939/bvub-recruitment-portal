using System;
using System.Collections.Generic;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class DashboardVm
    {
        // Cards
        public int TotalCandidates { get; set; }
        public int NewCandidatesToday { get; set; }
        public int NewCandidatesThisMonth { get; set; }
        public decimal DoDNewCandidatesPct { get; set; } // Today vs Yesterday
        public decimal MoMNewCandidatesPct { get; set; } // This month vs Last month

        // Line chart (30 days)
        public List<DayPoint> Last30DaysSeries { get; set; } = new();

        // Gender breakdown (from UngVien.GioiTinh: 1 = Nữ, else = Nam)
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }

        // Age buckets (tính trong code)
        public List<Bucket> AgeBuckets { get; set; } = new();

        // Recent table
        public List<RecentCandidateVm> RecentCandidates { get; set; } = new();
    }

    public class DayPoint
    {
        public string Day { get; set; } = ""; // "dd/MM"
        public int Count { get; set; }
    }

    public class Bucket
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
    }

    public class RecentCandidateVm
    {
        public int UngVienId { get; set; }
        public string? HoTen { get; set; }
        public string? Email { get; set; }
        public DateTime NgayUngTuyen { get; set; }
    }
}
