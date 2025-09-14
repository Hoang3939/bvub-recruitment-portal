using System;
using System.Threading.Tasks;
using BVUB_WebTuyenDung.Areas.Admin.Data;   // chứa AdminDbContext
using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.Services
{
    public interface IAuditTrailService
    {
        Task LogAsync(string userName, string action);
    }

    public class AuditTrailService : IAuditTrailService
    {
        private readonly AdminDbContext _context;
        public AuditTrailService(AdminDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userName, string action)
        {
            var log = new AuditTrail
            {
                UserName = userName,
                Action = action,
                ActionDate = DateTime.Now
            };

            _context.AuditTrails.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}