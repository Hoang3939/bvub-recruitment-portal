// Areas/Admin/ViewModels/EmailSettingsIndexVM.cs
using BVUB_WebTuyenDung.Areas.Admin.Services;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class EmailSettingsIndexVM
    {
        // Cột trái: luôn hiển thị cái đang có trong DB
        public EmailSettings Current { get; set; } = new EmailSettings();

        // Cột phải (form): để trống khi GET
        // Gợi ý: để trống Password = giữ nguyên; điền mới = thay
        public EmailSettings Form { get; set; } = new EmailSettings();
    }
}