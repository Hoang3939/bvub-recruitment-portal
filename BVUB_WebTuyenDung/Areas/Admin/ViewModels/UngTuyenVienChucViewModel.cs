using System.Collections.Generic;
using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class UngTuyenVienChucViewModel
    {
        public UngVien UngVien { get; set; } = new();
        public DonVienChuc DonVienChuc { get; set; } = new();
        public List<VanBang> VanBangs { get; set; } = new();

        public int? SelectedChucDanhId { get; set; }
        public string? TinhTrangSucKhoeKhac { get; set; }
        public string? TrinhDoVanHoaKhac { get; set; }
    }
}
