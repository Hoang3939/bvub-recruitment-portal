// Areas/Admin/Services/DbSmtpEmailSender.cs
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Areas.Admin.Services
{
    public class DbSmtpEmailSender : IEmailSender
    {
        private readonly ISettingsStore _store;
        public DbSmtpEmailSender(ISettingsStore store) => _store = store;

        public async Task SendAsync(string toEmail, string toName, string subject, string body, bool isHtml = false)
        {
            var s = await _store.GetEmailSettingsAsync(); // đọc Username, Password, FromEmail, FromName, SmtpHost, SmtpPort, EnableSsl

            using var client = new SmtpClient(s.SmtpHost, s.SmtpPort)
            {
                EnableSsl = s.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(s.Username, s.Password),
                Timeout = 15000
            };

            var fromAddr = new MailAddress(
                string.IsNullOrWhiteSpace(s.FromEmail) ? s.Username : s.FromEmail,
                string.IsNullOrWhiteSpace(s.FromName) ? s.Username : s.FromName
            );

            var msg = new MailMessage
            {
                From = fromAddr,
                Subject = subject ?? "",
                Body = body ?? "",
                IsBodyHtml = isHtml
            };
            msg.To.Add(new MailAddress(toEmail, string.IsNullOrWhiteSpace(toName) ? toEmail : toName));

            await client.SendMailAsync(msg);
        }
    }
}