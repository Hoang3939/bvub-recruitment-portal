namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class KhoaPhongViTri
    {
        public int KhoaPhongId { get; set; }
        public int ViTriId { get; set; }

        public DanhMucKhoaPhong KhoaPhong { get; set; }
        public DanhMucViTriDuTuyen ViTri { get; set; }
    }
}
