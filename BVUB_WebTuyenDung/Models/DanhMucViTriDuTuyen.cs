using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class DanhMucViTriDuTuyen
    {
        [Key]
        public int ViTriId { get; set; }
        [Required]
        public string TenViTri { get; set; }

        public int ChucDanhId { get; set; }
        public DanhMucChucDanhDuTuyen ChucDanh { get; set; }
    }

}
