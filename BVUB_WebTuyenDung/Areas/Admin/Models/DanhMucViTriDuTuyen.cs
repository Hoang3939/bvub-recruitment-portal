using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class DanhMucViTriDuTuyen
    {
        [Key]
        public int ViTriId { get; set; }

        [Required, MaxLength(70)]
        public string TenViTri { get; set; }

        public int ChucDanhId { get; set; }
        public DanhMucChucDanhDuTuyen ChucDanh { get; set; }
        public int TamNgung { get; set; }
        public ICollection<KhoaPhongViTri> KhoaPhongViTris { get; set; } = new List<KhoaPhongViTri>();
        public ICollection<DonVienChuc> DonVienChucs { get; set; } = new List<DonVienChuc>();
    }
}
