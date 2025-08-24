using System.Collections.Generic;

namespace BVUB_WebTuyenDung.Models
{
    public class TuyenDungListViewModel
    {
        public IReadOnlyList<ThongTinTuyenDung> Items { get; set; } = new List<ThongTinTuyenDung>();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Total { get; set; } = 0;

        public string? Loai { get; set; }      // "", "VC", "NLD"
        public string? Q { get; set; }         // từ khóa
        public string? Sort { get; set; }      // "moi-nhat" | "sap-het-han"

        public int TotalPages => (int)System.Math.Ceiling((double)Total / PageSize);
    }
}