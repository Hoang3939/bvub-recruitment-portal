namespace BVUB_WebTuyenDung.Models
{
    public class KhoaPhongViTri
    {
        public int KhoaPhongId { get; set; }
        public DanhMucKhoaPhong KhoaPhong { get; set; }

        public int ViTriId { get; set; }
        public DanhMucViTriDuTuyen ViTri { get; set; }
    }
}
