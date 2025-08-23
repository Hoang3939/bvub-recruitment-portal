using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Infrastructure.Email
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}