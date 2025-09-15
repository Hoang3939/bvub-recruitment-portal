// Areas/Admin/Models/SystemSetting.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Section { get; set; } = "";   // ví dụ: "Email"

        [Required, MaxLength(100)]
        public string Key { get; set; } = "";       // ví dụ: "Username", "Password", "FromEmail", "FromName"

        public string? Value { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
    }
}