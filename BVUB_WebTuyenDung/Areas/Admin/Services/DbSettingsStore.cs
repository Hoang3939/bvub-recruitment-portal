using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using BVUB_WebTuyenDung.Areas.Admin.Data;
using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.Services
{
    public class DbSettingsStore : ISettingsStore
    {
        private readonly AdminDbContext _ctx;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly IDataProtector _protector;
        private const string CacheKey = "EmailSettingsCache";
        private const string LinkCachePrefix = "LinkStatus_";
        private const string LinkSection = "UngTuyen";

        public DbSettingsStore(AdminDbContext ctx, IConfiguration config, IMemoryCache cache, IDataProtectionProvider dp)
        {
            _ctx = ctx;
            _config = config;
            _cache = cache;
            _protector = dp.CreateProtector("EmailSettingsProtector");
        }

        public async Task<EmailSettings> GetEmailSettingsAsync()
        {
            if (_cache.TryGetValue(CacheKey, out EmailSettings cached)) return cached;

            var dict = await _ctx.SystemSettings
                .Where(s => s.Section == "Email")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            // Đọc từ DB, fallback về giá trị mặc định nếu chưa có row
            string? get(string key, string? fallback = null)
            {
                if (dict.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
                {
                    if (key == "Password")
                    {
                        try { return _protector.Unprotect(v); } catch { return ""; }
                    }
                    return v;
                }
                return fallback;
            }

            var smtpHostValue = get("SmtpHost", "smtp.gmail.com");
            var smtpPortValue = get("SmtpPort", "587");
            var enableSslValue = get("EnableSsl", "true");

            var result = new EmailSettings
            {
                Username = get("Username"),
                Password = get("Password"), // đã giải mã nếu có
                FromEmail = get("FromEmail"),
                FromName = get("FromName"),
                SmtpHost = smtpHostValue ?? "smtp.gmail.com",
                SmtpPort = int.TryParse(smtpPortValue, out var p) ? p : 587,
                EnableSsl = bool.TryParse(enableSslValue, out var ssl) ? ssl : true
            };

            _cache.Set(CacheKey, result, TimeSpan.FromMinutes(5));
            return result ?? new EmailSettings();
        }

        public async Task UpdateEmailSettingsAsync(EmailSettings settings, string updatedBy)
        {
            // Upsert theo Key
            async Task upsert(string key, string? value)
            {
                var entity = await _ctx.SystemSettings
                    .FirstOrDefaultAsync(x => x.Section == "Email" && x.Key == key);

                if (key == "Password" && !string.IsNullOrWhiteSpace(value))
                    value = _protector.Protect(value); // mã hoá khi lưu

                if (entity == null)
                {
                    entity = new SystemSetting
                    {
                        Section = "Email",
                        Key = key,
                        Value = value,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = updatedBy
                    };
                    _ctx.SystemSettings.Add(entity);
                }
                else
                {
                    // Password: nếu để trống => không đổi
                    if (key == "Password" && string.IsNullOrWhiteSpace(settings.Password))
                    {
                        // giữ nguyên
                    }
                    else
                    {
                        entity.Value = value;
                        entity.UpdatedAt = DateTime.UtcNow;
                        entity.UpdatedBy = updatedBy;
                    }
                }
            }

            await upsert("Username", settings.Username);
            await upsert("FromEmail", settings.FromEmail);
            await upsert("FromName", settings.FromName);
            await upsert("Password", settings.Password);
            await upsert("SmtpHost", settings.SmtpHost);
            await upsert("SmtpPort", settings.SmtpPort.ToString());
            await upsert("EnableSsl", settings.EnableSsl.ToString());

            await _ctx.SaveChangesAsync();
            _cache.Remove(CacheKey); // clear cache để lần sau đọc mới
        }

        // ===== Link ứng tuyển (Khóa / Mở) =====

        public async Task<bool> IsLinkOpenAsync(string type)
        {
            var cacheKey = LinkCachePrefix + type;
            if (_cache.TryGetValue(cacheKey, out bool cached)) return cached;

            var setting = await _ctx.SystemSettings
                .FirstOrDefaultAsync(s => s.Section == LinkSection && s.Key == "MoLink" + type);

            var isOpen = setting?.Value != "false"; // default = true (mở) khi chưa có row

            _cache.Set(cacheKey, isOpen, TimeSpan.FromMinutes(5));
            return isOpen;
        }

        public async Task SetLinkStatusAsync(string type, bool open, string updatedBy)
        {
            var key = "MoLink" + type;
            var entity = await _ctx.SystemSettings
                .FirstOrDefaultAsync(x => x.Section == LinkSection && x.Key == key);

            if (entity == null)
            {
                entity = new SystemSetting
                {
                    Section = LinkSection,
                    Key = key,
                    Value = open ? "true" : "false",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = updatedBy
                };
                _ctx.SystemSettings.Add(entity);
            }
            else
            {
                entity.Value = open ? "true" : "false";
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = updatedBy;
            }

            _cache.Remove(LinkCachePrefix + type);
        }
    }
}