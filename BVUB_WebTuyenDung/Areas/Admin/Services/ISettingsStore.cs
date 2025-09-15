using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Areas.Admin.Services
{
    public interface ISettingsStore
    {
        Task<EmailSettings> GetEmailSettingsAsync();
        Task UpdateEmailSettingsAsync(EmailSettings settings, string updatedBy);
    }
}
