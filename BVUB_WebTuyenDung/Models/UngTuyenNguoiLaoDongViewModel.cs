namespace BVUB_WebTuyenDung.Models
{
    public class UngTuyenNguoiLaoDongViewModel
    {
        public UngVien UngVien { get; set; }
        public HopDongNguoiLaoDong HopDongNguoiLaoDong { get; set; }
        public List<VanBang> VanBangs { get; set; } = new();
        public UngTuyenNguoiLaoDongViewModel()
        {
            UngVien = new UngVien();
            HopDongNguoiLaoDong = new HopDongNguoiLaoDong();
        }
    }
}
