using System;
using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Areas.Admin.Services
{
    public interface ISettingsStore
    {
        Task<EmailSettings> GetEmailSettingsAsync();
        Task UpdateEmailSettingsAsync(EmailSettings settings, string updatedBy);

        /// <summary>Kiểm tra link ứng tuyển đang mở hay khóa. type = "VC" hoặc "NLD". Default = true (mở).</summary>
        Task<bool> IsLinkOpenAsync(string type);

        /// <summary>Upsert trạng thái mở/khóa link ứng tuyển.</summary>
        Task SetLinkStatusAsync(string type, bool open, string updatedBy);
    }
}
