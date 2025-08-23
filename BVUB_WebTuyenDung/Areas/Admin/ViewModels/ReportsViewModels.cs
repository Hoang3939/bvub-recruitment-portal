using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class SummaryVm
    {
        public int PendingApplications { get; set; }
        public int ActiveRecruitments { get; set; }
        public int ExpiringSoonRecruitments { get; set; }
        public int TotalRecruitments { get; set; }
        public int ByTypeVC { get; set; }
        public int ByTypeHD { get; set; }
        public int NewCandidates { get; set; }
        public List<ExpiringItemVm> ExpiringTop { get; set; } = new();
    }

    public class ExpiringItemVm
    {
        public int Id { get; set; }
        public string? TieuDe { get; set; }
        public string? Loai { get; set; }
        public DateTime Han { get; set; }
        public int DaysLeft { get; set; }
    }

    // Giữ tên key JSON là "labels" và "data"
    public class SeriesVm
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("data")]
        public List<int> Data { get; set; } = new();
    }

    // Giữ tên key JSON là "labels", "a", "b"
    public class PairSeriesVm
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("a")]
        public List<int> A { get; set; } = new();

        [JsonPropertyName("b")]
        public List<int> B { get; set; } = new();
    }

    // Giữ tên key JSON là "labels", "total", "vc", "hd"
    public class CandidateSeriesVm
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("total")]
        public List<int> Total { get; set; } = new();

        [JsonPropertyName("vc")]
        public List<int> VC { get; set; } = new();

        [JsonPropertyName("hd")]
        public List<int> HD { get; set; } = new();
    }

    // Giữ tên key JSON là "pending", "approved", "cancelled", "total", "vc", "hd"
    public class CandidateBreakdownVm
    {
        [JsonPropertyName("pending")]
        public int Pending { get; set; }

        [JsonPropertyName("approved")]
        public int Approved { get; set; }

        [JsonPropertyName("cancelled")]
        public int Cancelled { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("vc")]
        public int VC { get; set; }

        [JsonPropertyName("hd")]
        public int HD { get; set; }
    }
}
