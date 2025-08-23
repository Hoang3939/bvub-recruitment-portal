using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class UngTuyenNguoiLaoDongViewModel
    {
        public UngVien UngVien { get; set; } = new UngVien();
        public HopDongNguoiLaoDong HopDongNguoiLaoDong { get; set; } = new HopDongNguoiLaoDong();
    }
}
