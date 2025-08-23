using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class DanhMucChucDanhDuTuyen
    {
        [Key]
        public int ChucDanhId { get; set; }

        [Required]
        public string TenChucDanh { get; set; }
        public int TamNgung { get; set; }

        public ICollection<DanhMucViTriDuTuyen> ViTriDuTuyens { get; set; }
    }
}

