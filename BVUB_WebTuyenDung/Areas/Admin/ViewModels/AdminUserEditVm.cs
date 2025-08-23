using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class AdminUserEditVm
    {
        [Required]
        public int AdminId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Username")]
        public string Username { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        // Để trống nếu không đổi mật khẩu
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; }

        // Checkbox phân quyền (bitmask)
        public List<int> SelectedPerms { get; set; } = new List<int>();

        // Nhận mật khẩu quản trị (từ popup) – required khi submit
        [Required(ErrorMessage = "Hãy nhập mật khẩu quản trị để xác nhận")]
        public string AdminPassword { get; set; }
    }
}
