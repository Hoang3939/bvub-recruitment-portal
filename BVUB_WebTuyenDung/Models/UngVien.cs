using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class UngVien
    {
        [Key]
        public int UngVienId { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Họ tên chỉ được chứa chữ và khoảng trắng, không chứa số hoặc ký tự đặc biệt.")]
        public string HoTen { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [Required]
        public int GioiTinh { get; set; }

        [Required]
        [StringLength(11)]
        [RegularExpression(@"^\d+$", ErrorMessage = "Số điện thoại chỉ được chứa ký tự số.")]
        public string SoDienThoai { get; set; }

        [Required, EmailAddress, StringLength(255)]
        public string Email { get; set; }

        [MaxLength(12, ErrorMessage = "CCCD tối đa 12 số.")]
        [RegularExpression(@"^\d{0,12}$", ErrorMessage = "CCCD chỉ chứa số.")]
        public string CCCD { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? NgayCapCCCD { get; set; }

        [Required, StringLength(255)]
        public string NoiCapCCCD { get; set; }

        [Required, StringLength(255)]
        public string DiaChiThuongTru { get; set; }

        [Required, StringLength(255)]
        public string DiaChiCuTru { get; set; }

        [Required, StringLength(13)]
        public string MaSoThue { get; set; }

        [Required, StringLength(12)]
        public string SoTaiKhoan { get; set; }

        [Required, StringLength(50)]
        public string TinhTrangSucKhoe { get; set; }

        [Required, StringLength(100)]
        public string TrinhDoChuyenMon { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime NgayUngTuyen { get; set; }


        public DonVienChuc? DonVienChuc { get; set; }
        public HopDongNguoiLaoDong? HopDongNguoiLaoDong { get; set; }
    }
}
