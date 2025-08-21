using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class HopDongNguoiLaoDong
    {
        [Key]
        public int HopDongId { get; set; }
        [Required]
        public int UngVienId { get; set; }

        [Required, StringLength(30)]
        public string Loai { get; set; }
        [Required]
        public int KhoaPhongCongTacId { get; set; }
        [Required, StringLength(70)]
        public string NoiSinh { get; set; }

        public string ChuyenNganhDaoTao { get; set; }
        public string NamTotNghiep { get; set; }
        public string TrinhDoTinHoc { get; set; }
        public string TrinhDoNgoaiNgu { get; set; }
        public string ChungChiHanhNghe { get; set; }
        public string NgheNghiepTruocTuyenDung { get; set; }
        [Required, DataType(DataType.Date)]
        public DateTime NgayNop { get; set; }
        public int TrangThai { get; set; } = 0;
        [Required, StringLength(50)]
        public string MaTraCuu { get; set; }

        public virtual UngVien? UngVien { get; set; }
    }

}
