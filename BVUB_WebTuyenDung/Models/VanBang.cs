namespace BVUB_WebTuyenDung.Models
{
    public class VanBang
    {
        public int VanBangId { get; set; }
        public int DonVienChucId { get; set; }
        public DonVienChuc DonVienChuc { get; set; }

        public string TenCoSo { get; set; }
        public DateTime? NgayCap { get; set; }
        public string SoHieu { get; set; }
        public string ChuyenNganhDaoTao { get; set; }
        public string NganhDaoTao { get; set; }
        public string HinhThucDaoTao { get; set; }
        public string XepLoai { get; set; }
    }

}
