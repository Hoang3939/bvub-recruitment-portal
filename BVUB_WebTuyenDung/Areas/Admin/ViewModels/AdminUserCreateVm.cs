using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class AdminUserCreateVm
    {
        [Required, StringLength(100)]
        public string Username { get; set; }

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; }

        [Required, StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }

        public List<int> SelectedPerms { get; set; } = new();

        // chỉ để xác nhận, không map DB
        [Required(ErrorMessage = "Hãy nhập mật khẩu quản trị để xác nhận.")]
        [DataType(DataType.Password)]
        public string AdminPassword { get; set; }
    }
}
