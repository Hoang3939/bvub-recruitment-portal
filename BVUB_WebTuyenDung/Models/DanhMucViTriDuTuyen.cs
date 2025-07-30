namespace BVUB_WebTuyenDung.Models
{
    public class DanhMucViTriDuTuyen
    {
        public int ViTriId { get; set; }
        public string TenViTri { get; set; }

        public int ChucDanhId { get; set; }
        public DanhMucChucDanhDuTuyen ChucDanh { get; set; }
    }

}
