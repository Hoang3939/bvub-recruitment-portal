using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BVUB_WebTuyenDung.Areas.Admin.Models;
using BVUB_WebTuyenDung.Areas.Admin.Data; // giả sử DbContext nằm ở đây
using Microsoft.EntityFrameworkCore;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AuditTrailController : Controller
    {
        private readonly AdminDbContext _context;

        public AuditTrailController(AdminDbContext context)
        {
            _context = context;
        }

        // Hàm ghi nhật ký
        [NonAction] // không expose ra route
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
