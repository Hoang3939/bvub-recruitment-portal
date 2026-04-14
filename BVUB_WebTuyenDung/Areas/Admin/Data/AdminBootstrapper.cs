using System;
using System.Threading.Tasks;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BVUB_WebTuyenDung.Areas.Admin.Data
{
    public sealed class BootstrapAdminOptions
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
    }

    public static class AdminBootstrapper
    {
        public static async Task SeedDefaultAdminAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            await SeedDefaultEmailSettingsAsync(db, logger);

            var options = configuration.GetSection("BootstrapAdmin").Get<BootstrapAdminOptions>();
            if (!HasValidOptions(options))
            {
                logger.LogInformation("Bootstrap admin skipped because BootstrapAdmin config is missing or incomplete.");
                return;
            }

            if (await db.AdminUsers.AnyAsync(x => x.Role == 1))
            {
                logger.LogInformation("Bootstrap admin skipped because an admin account already exists.");
                return;
            }

            var normalizedUsername = options!.Username!.Trim();
            var normalizedEmail = options.Email!.Trim();

            if (await db.AdminUsers.AnyAsync(x => x.Username == normalizedUsername || x.Email == normalizedEmail))
            {
                logger.LogWarning("Bootstrap admin skipped because username or email already exists for a non-admin account.");
                return;
            }

            var admin = new AdminUser
            {
                Username = normalizedUsername,
                Email = normalizedEmail,
                PasswordHash = PasswordHasher.Hash(options.Password!.Trim()),
                Role = 1
            };

            db.AdminUsers.Add(admin);
            await db.SaveChangesAsync();

            logger.LogInformation("Bootstrap admin created successfully with username {Username}.", normalizedUsername);
        }

        private static async Task SeedDefaultEmailSettingsAsync(AdminDbContext db, ILogger logger)
        {
            var updated = false;
            updated |= await EnsureSystemSettingAsync(db, "Email", "FromName", "Hệ thống Bệnh viện Ung Bướu TPHCM");
            updated |= await EnsureSystemSettingAsync(db, "Email", "SmtpHost", "smtp.gmail.com");
            updated |= await EnsureSystemSettingAsync(db, "Email", "SmtpPort", "587");
            updated |= await EnsureSystemSettingAsync(db, "Email", "EnableSsl", "true");

            if (!updated)
                return;

            await db.SaveChangesAsync();
            logger.LogInformation("Default email settings seeded successfully.");
        }

        private static async Task<bool> EnsureSystemSettingAsync(AdminDbContext db, string section, string key, string value)
        {
            var exists = await db.SystemSettings.AnyAsync(x => x.Section == section && x.Key == key);
            if (exists)
                return false;

            db.SystemSettings.Add(new SystemSetting
            {
                Section = section,
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "bootstrap"
            });

            return true;
        }

        private static bool HasValidOptions(BootstrapAdminOptions? options)
        {
            return options != null
                && !string.IsNullOrWhiteSpace(options.Username)
                && !string.IsNullOrWhiteSpace(options.Password)
                && !string.IsNullOrWhiteSpace(options.Email);
        }
    }
}
