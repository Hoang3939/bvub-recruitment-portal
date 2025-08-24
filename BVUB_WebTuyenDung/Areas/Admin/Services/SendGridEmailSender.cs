using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BVUB_WebTuyenDung.Areas.Admin.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SendGridEmailSender> _logger;

        public SendGridEmailSender(IConfiguration config, ILogger<SendGridEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string toName, string subject, string body, bool isHtml = false)
        {
            var apiKey = (_config["SendGrid:ApiKey"] ?? "").Trim();
            var from = (_config["SendGrid:From"] ?? "").Trim();
            var fromName = (_config["SendGrid:FromName"] ?? "Hệ thống").Trim();
            var host = (_config["SendGrid:Host"] ?? "https://api.sendgrid.com").Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Thiếu cấu hình SendGrid:ApiKey");
            if (string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Thiếu cấu hình SendGrid:From (phải là Sender đã verify)");

            var options = new SendGridClientOptions { ApiKey = apiKey, Host = host };
            var client = new SendGridClient(options);

            var msg = new SendGridMessage
            {
                From = new EmailAddress(from, fromName),
                Subject = subject,
                PlainTextContent = isHtml ? StripHtml(body) : body,
                HtmlContent = isHtml ? body : null
            };
            msg.AddTo(new EmailAddress(toEmail, toName));

            var resp = await client.SendEmailAsync(msg);
            if ((int)resp.StatusCode >= 400)
            {
                var detail = await resp.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid failed {Status}. Host={Host}, From={From}, To={To}. Detail={Detail}",
                    (int)resp.StatusCode, host, from, toEmail, detail);

                throw new InvalidOperationException($"SendGrid {(int)resp.StatusCode}: {detail}");
            }
        }
        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            return Regex.Replace(html, "<.*?>", " ").Trim();
        }
    }
}
