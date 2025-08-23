using BVUB_WebTuyenDung.Areas.Admin.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class CreateCandidateVm
    {
        public CreateCandidateVm()
        {
            // Giá trị mặc định khi mở form
            NgayUngTuyen = DateTime.Today;
            NgayCapCCCD = DateTime.Today;
            NgaySinh = new DateTime(1990, 1, 1);
            GioiTinh = 1; // Nam
        }

        private string _hoTen;
        /// <summary>
        /// Chuẩn hóa: trim và gộp nhiều khoảng trắng thành 1 khoảng.
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên từ 2–100 ký tự.")]
        [RegularExpression(@"^(?!.*\s{2,})[\p{L}\s'’\-]{2,100}$",
            ErrorMessage = "Họ tên chỉ được chứa chữ cái và khoảng trắng (có thể có dấu ’ hoặc -), không chứa số/ký tự đặc biệt, không có 2 khoảng trắng liền nhau.")]
        [Display(Name = "Họ và tên")]
        public string HoTen
        {
            get => _hoTen;
            set => _hoTen = string.IsNullOrWhiteSpace(value)
                ? value
                : Regex.Replace(value.Trim(), @"\s+", " ");
        }

        [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [DateNotInFuture(ErrorMessage = "Ngày không hợp lệ.")]
        [Display(Name = "Ngày sinh")]
        public DateTime NgaySinh { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        [Range(0, 1, ErrorMessage = "Giới tính không hợp lệ.")]
        [Display(Name = "Giới tính")] // 1 = Nữ, 0 = Nam
        public int GioiTinh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (10–11 số và bắt đầu bằng 0).")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số CCCD.")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD phải gồm 12 chữ số.")]
        [Display(Name = "CCCD")]
        public string CCCD { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày cấp CCCD.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [DateNotInFuture(ErrorMessage = "Ngày không hợp lệ.")]
        [Display(Name = "Ngày cấp CCCD")]
        public DateTime NgayCapCCCD { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nơi cấp CCCD.")]
        [StringLength(200, ErrorMessage = "Nơi cấp tối đa 200 ký tự.")]
        [RegularExpression(
            @"^(Bộ Công An|Cục Cảnh sát quản lý hành chính về trật tự xã hội|Cục Cảnh sát đăng ký quản lý cư trú và dữ liệu Quốc gia về dân(?: cư)?)$",
            ErrorMessage = "Nơi cấp CCCD không hợp lệ.")]
        [Display(Name = "Nơi cấp CCCD")]
        public string NoiCapCCCD { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ thường trú.")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        [Display(Name = "Địa chỉ thường trú")]
        public string DiaChiThuongTru { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cư trú.")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        [Display(Name = "Địa chỉ cư trú")]
        public string DiaChiCuTru { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã số thuế.")]
        [RegularExpression(@"^\d{10}(\d{3})?$", ErrorMessage = "Mã số thuế 10 hoặc 13 chữ số.")]
        [Display(Name = "Mã số thuế")]
        public string MaSoThue { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản.")]
        [RegularExpression(@"^\d{6,20}$", ErrorMessage = "Số tài khoản chỉ gồm số (6–20 chữ số).")]
        [Display(Name = "Số tài khoản")]
        public string SoTaiKhoan { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hoặc nhập tình trạng sức khỏe.")]
        [StringLength(100, ErrorMessage = "Tình trạng sức khỏe tối đa 100 ký tự.")]
        [Display(Name = "Tình trạng sức khỏe")]
        public string TinhTrangSucKhoe { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập trình độ chuyên môn.")]
        [StringLength(100, ErrorMessage = "Trình độ chuyên môn tối đa 100 ký tự.")]
        [Display(Name = "Trình độ chuyên môn")]
        public string TrinhDoChuyenMon { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày ứng tuyển.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày ứng tuyển")]
        public DateTime NgayUngTuyen { get; set; }
    }
}
