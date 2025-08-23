using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Data
{
    [Table("DanhMucChucDanhDuTuyen")]
    public class DanhMucChucDanhDuTuyen
    {
        [Key] public int ChucDanhId { get; set; }
        [Required, MaxLength(70)] public string TenChucDanh { get; set; }
        // 0: sử dụng, 1: tạm ngưng
        public int TamNgung { get; set; } = 0;

        public ICollection<DanhMucViTriDuTuyen> ViTris { get; set; }
    }

    [Table("DanhMucViTriDuTuyen")]
    public class DanhMucViTriDuTuyen
    {
        [Key] public int ViTriId { get; set; }
        [Required, MaxLength(70)] public string TenViTri { get; set; }

        [ForeignKey(nameof(ChucDanh))] public int ChucDanhId { get; set; }
        public DanhMucChucDanhDuTuyen ChucDanh { get; set; }

        public int TamNgung { get; set; } = 0;

        public ICollection<KhoaPhongViTri> KhoaPhongViTris { get; set; }
    }

    [Table("DanhMucKhoaPhong")]
    public class DanhMucKhoaPhong
    {
        [Key] public int KhoaPhongId { get; set; }
        [Required, MaxLength(70)] public string Ten { get; set; }
        [Required, MaxLength(20)] public string Loai { get; set; }  // "Phòng" | "Khoa"
        public int TamNgung { get; set; } = 0;

        public ICollection<KhoaPhongViTri> KhoaPhongViTris { get; set; }
    }

    [Table("KhoaPhongViTri")]
    public class KhoaPhongViTri
    {
        public int KhoaPhongId { get; set; }
        public DanhMucKhoaPhong KhoaPhong { get; set; }

        public int ViTriId { get; set; }
        public DanhMucViTriDuTuyen ViTri { get; set; }
    }
}
