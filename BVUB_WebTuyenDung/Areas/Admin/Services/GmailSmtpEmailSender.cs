using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace BVUB_WebTuyenDung.Areas.Admin.Services
{
    public class GmailOptions
    {
        public string User { get; set; } = "";      
        public string AppPass { get; set; } = "";  
        public string? From { get; set; }           
        public string FromName { get; set; } = "Hệ thống BVUB";
        public string Host { get; set; } = "smtp.gmail.com"; 
        public int Port { get; set; } = 587;         
    }

    public class GmailSmtpEmailSender : IEmailSender
    {
        private readonly GmailOptions _opt;
        public GmailSmtpEmailSender(IOptions<GmailOptions> options) => _opt = options.Value ?? new GmailOptions();

        public async Task SendAsync(string toEmail, string toName, string subject, string body, bool isHtml = false)
        {
            if (string.IsNullOrWhiteSpace(_opt.User))
                throw new System.InvalidOperationException("Thiếu Email:Gmail:User");
            if (string.IsNullOrWhiteSpace(_opt.AppPass))
                throw new System.InvalidOperationException("Thiếu Email:Gmail:AppPass");

            using var client = new SmtpClient(_opt.Host, _opt.Port)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_opt.User, _opt.AppPass),
                Timeout = 15000
            };

            var fromAddr = new MailAddress(string.IsNullOrWhiteSpace(_opt.From) ? _opt.User : _opt.From, _opt.FromName);
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
