// Areas/Admin/Services/EmailSettings.cs
namespace BVUB_WebTuyenDung.Areas.Admin.Services
{
    public class EmailSettings
    {
        public string? Username { get; set; }
        public string? Password { get; set; }   // sẽ mã hoá khi lưu DB
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }

        // Các tham số ít thay đổi vẫn đọc từ appsettings.json (không cho sửa trên UI)
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
    }
}