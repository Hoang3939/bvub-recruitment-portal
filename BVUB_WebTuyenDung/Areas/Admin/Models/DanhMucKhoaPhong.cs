using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class DanhMucKhoaPhong
    {
        [Key]
        public int KhoaPhongId { get; set; }

        [Required, MaxLength(70)]
        public string Ten { get; set; }

        [Required, MaxLength(20)]
        public string Loai { get; set; } // "Phòng"/"Khoa"

        public int TamNgung { get; set; }

        // Navigation
        public ICollection<DonVienChuc> DonVienChucs { get; set; } = new List<DonVienChuc>();
        public ICollection<KhoaPhongViTri> KhoaPhongViTris { get; set; }
    }
}
