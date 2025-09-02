using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class HopDongNguoiLaoDongDetailsVm
    {
        public HopDongNguoiLaoDong Don { get; set; } = default!;
        public UngVien UngVien { get; set; } = default!;
        public string TrangThaiLabel { get; set; } = "";
        public string TrangThaiClass { get; set; } = "";

        public ICollection<VanBang> VanBangs { get; set; } = new List<VanBang>();
    }
}
