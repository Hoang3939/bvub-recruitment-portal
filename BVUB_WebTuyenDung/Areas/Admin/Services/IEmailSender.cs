using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Areas.Admin.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string toName, string subject, string plainTextBody);
    }
}
