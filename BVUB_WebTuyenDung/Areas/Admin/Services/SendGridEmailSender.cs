using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

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

        public async Task SendAsync(string toEmail, string toName, string subject, string plainTextBody)
        {
            // ĐỌC & TRIM cấu hình để tránh lỗi copy kèm dấu cách/ngoặc kép
            var apiKey = (_config["SendGrid:ApiKey"] ?? "").Trim();
            var from = (_config["SendGrid:From"] ?? "").Trim();
            var fromName = (_config["SendGrid:FromName"] ?? "Hệ thống").Trim();
            var host = (_config["SendGrid:Host"] ?? "https://api.sendgrid.com").Trim(); // EU: https://api.eu.sendgrid.com

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Thiếu cấu hình SendGrid:ApiKey");
            if (string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Thiếu cấu hình SendGrid:From (phải là Sender đã verify)");

            // Cho phép chọn region (EU/US)
            var options = new SendGridClientOptions { ApiKey = apiKey, Host = host };
            var client = new SendGridClient(options);

            var msg = new SendGridMessage
            {
                From = new EmailAddress(from, fromName),
                Subject = subject,
                PlainTextContent = plainTextBody
            };
            msg.AddTo(new EmailAddress(toEmail, toName));

            var resp = await client.SendEmailAsync(msg);
            if ((int)resp.StatusCode >= 400)
            {
                var body = await resp.Body.ReadAsStringAsync();
                // Log chẩn đoán nhưng KHÔNG lộ API key
                _logger.LogError("SendGrid failed {Status}. Host={Host}, From={From}, To={To}. Detail={Detail}",
                    (int)resp.StatusCode, host, from, toEmail, body);

                // Ném chi tiết để controller hiển thị ở Dev
                throw new InvalidOperationException($"SendGrid {(int)resp.StatusCode}: {body}");
            }
        }
    }
}
