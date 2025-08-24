using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Infrastructure.Email
{
    public interface InfrastructureEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}