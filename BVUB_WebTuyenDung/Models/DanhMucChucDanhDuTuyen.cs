namespace BVUB_WebTuyenDung.Models
{
    public class DanhMucChucDanhDuTuyen
    {
        public int ChucDanhId { get; set; }
        public string TenChucDanh { get; set; }

        public ICollection<DanhMucViTriDuTuyen> ViTriDuTuyens { get; set; }
    }
}

