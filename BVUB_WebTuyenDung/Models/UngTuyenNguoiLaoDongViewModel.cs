namespace BVUB_WebTuyenDung.Models
{
    public class UngTuyenNguoiLaoDongViewModel
    {
        public UngVien UngVien { get; set; }
        public HopDongNguoiLaoDong HopDongNguoiLaoDong { get; set; }

        public UngTuyenNguoiLaoDongViewModel()
        {
            UngVien = new UngVien();
            HopDongNguoiLaoDong = new HopDongNguoiLaoDong();
        }
    }
}
