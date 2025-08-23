using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class DanhMucChucDanhDuTuyen
    {
        [Key]
        public int ChucDanhId { get; set; }

        [MaxLength(70)]
        [Required]
        public string TenChucDanh { get; set; }

        public int TamNgung { get; set; } // 0/1

        // Navigation
        public ICollection<DanhMucViTriDuTuyen> ViTris { get; set; } = new List<DanhMucViTriDuTuyen>();
        public ICollection<DonVienChuc> DonVienChucs { get; set; } = new List<DonVienChuc>();
    }
}
