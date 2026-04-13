using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class HopDongNguoiLaoDong
    {
        [Key]
        public int HopDongId { get; set; }

        [Required]
        public int UngVienId { get; set; }
        public UngVien UngVien { get; set; }

        [Required, MaxLength(20)]
        public string Loai { get; set; } // "NguoiLaoDong" | "VienChuc"

        [Required]
        public int KhoaPhongCongTacId { get; set; }
        public DanhMucKhoaPhong KhoaPhongCongTac { get; set; }

        [Required, MaxLength(255)]
        public string NoiSinh { get; set; }

        [Required, MaxLength(50)]
        public string ChuyenNganhDaoTao { get; set; }

        [Required, MaxLength(50)]
        public string NamTotNghiep { get; set; }

        [Required, MaxLength(100)]
        public string TrinhDoTinHoc { get; set; }

        [Required, MaxLength(100)]
        public string TrinhDoNgoaiNgu { get; set; }

        [Required, MaxLength(100)]
        public string ChungChiHanhNghe { get; set; }

        [Required, MaxLength(100)]
        public string NgheNghiepTruocTuyenDung { get; set; }

        [Required, MaxLength(50)]
        public string DanToc { get; set; }

        [Required, MaxLength(50)]
        public string TonGiao { get; set; }

        public bool DangVien { get; set; } = false;

        [DataType(DataType.Date)]
        public DateTime? NgayVaoDang { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayChinhThuc { get; set; }

        [Column(TypeName = "datetime2(3)")]
        public DateTime NgayNop { get; set; }

        public int TrangThai { get; set; } = 0;

        [Required, MaxLength(50)]
        public string MaTraCuu { get; set; }
    }
}
