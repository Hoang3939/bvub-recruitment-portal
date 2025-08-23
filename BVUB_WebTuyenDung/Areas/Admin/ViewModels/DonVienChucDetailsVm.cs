using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class DonVienChucDetailsVm
    {
        public UngVien UngVien { get; set; } = default!;
        public DonVienChuc Don { get; set; } = default!;
        public string TrangThaiLabel { get; set; } = "";
        public string TrangThaiClass { get; set; } = "";
    }
}
