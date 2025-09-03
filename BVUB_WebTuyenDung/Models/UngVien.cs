using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class UngVien
    {
        [Key]
        public int UngVienId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Họ và tên.")]
        [StringLength(50, ErrorMessage = "Họ tên không được vượt quá 50 ký tự.")]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Họ tên chỉ được chứa chữ và khoảng trắng, không chứa số hoặc ký tự đặc biệt.")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Ngày sinh.")]
        [DataType(DataType.Date, ErrorMessage = "Ngày sinh không hợp lệ.")]
        public DateTime? NgaySinh { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Giới tính.")]
        public int GioiTinh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại.")]
        [StringLength(11, ErrorMessage = "Số điện thoại tối đa 11 số.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Số điện thoại chỉ được chứa ký tự số.")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(255, ErrorMessage = "Email tối đa 255 ký tự.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập CCCD.")]
        [MaxLength(12, ErrorMessage = "CCCD tối đa 12 số.")]
        [RegularExpression(@"^\d{0,12}$", ErrorMessage = "CCCD chỉ chứa số.")]
        public string CCCD { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Ngày cấp CCCD.")]
        [DataType(DataType.Date, ErrorMessage = "Ngày cấp CCCD không hợp lệ.")]
        public DateTime? NgayCapCCCD { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Nơi cấp CCCD.")]
        [StringLength(255, ErrorMessage = "Nơi cấp CCCD tối đa 255 ký tự.")]
        public string NoiCapCCCD { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ thường trú.")]
        [StringLength(255, ErrorMessage = "Địa chỉ thường trú tối đa 255 ký tự.")]
        public string DiaChiThuongTru { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ cư trú.")]
        [StringLength(255, ErrorMessage = "Địa chỉ cư trú tối đa 255 ký tự.")]
        public string DiaChiCuTru { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Mã số thuế.")]
        [StringLength(13, ErrorMessage = "Mã số thuế tối đa 13 ký tự.")]
        public string MaSoThue { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số tài khoản.")]
        [StringLength(12, ErrorMessage = "Số tài khoản tối đa 12 số.")]
        public string SoTaiKhoan { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Tình trạng sức khỏe.")]
        [StringLength(50, ErrorMessage = "Tình trạng sức khỏe tối đa 50 ký tự.")]
        public string TinhTrangSucKhoe { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Trình độ chuyên môn.")]
        [StringLength(100, ErrorMessage = "Trình độ chuyên môn tối đa 100 ký tự.")]
        public string TrinhDoChuyenMon { get; set; }

        [Required(ErrorMessage = "Ngày ứng tuyển bắt buộc.")]
        [DataType(DataType.Date, ErrorMessage = "Ngày ứng tuyển không hợp lệ.")]
        public DateTime NgayUngTuyen { get; set; }

        public ICollection<VanBang>? VanBangs { get; set; }
        public DonVienChuc? DonVienChuc { get; set; }
        public HopDongNguoiLaoDong? HopDongNguoiLaoDong { get; set; }
    }
}
