using BVUB_WebTuyenDung.Areas.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

[Area("Admin")]
[Authorize(Policy = "StaffAndAdmin")]
public class AuditTrailController : Controller
{
    private readonly AdminDbContext _ctx;
    public AuditTrailController(AdminDbContext ctx) => _ctx = ctx;

    [HttpGet]
    public async Task<IActionResult> Index(string? username, string? from, string? to)
    {
        var q = _ctx.AuditTrails.AsQueryable();
        var today = DateTime.Today;

        // Lọc theo username (nếu có)
        if (!string.IsNullOrWhiteSpace(username))
        {
            var kw = username.Trim();
            q = q.Where(x => x.UserName.Contains(kw));
        }

        // Parse dd/MM/yyyy
        static bool TryParseVn(string? s, out DateTime d) =>
            DateTime.TryParseExact(s ?? "", "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);

        var hasFrom = TryParseVn(from, out var fromDate);
        var hasTo = TryParseVn(to, out var toDate);

        // to không được ở tương lai
        if (hasTo && toDate.Date > today) { toDate = today; hasTo = true; to = toDate.ToString("dd/MM/yyyy"); }

        // Chỉ có from → to = hôm nay
        if (hasFrom && !hasTo) { toDate = today; hasTo = true; to = toDate.ToString("dd/MM/yyyy"); }

        // Chỉ có to → from = to
        if (!hasFrom && hasTo) { fromDate = toDate; hasFrom = true; from = fromDate.ToString("dd/MM/yyyy"); }

        // Cả hai → đảm bảo from <= to
        if (hasFrom && hasTo)
        {
            var f = fromDate.Date;
            var t = toDate.Date;
            if (f > t) (f, t) = (t, f);

            q = q.Where(x => x.ActionDate >= f && x.ActionDate < t.AddDays(1));

            // Ghi lại để hiển thị đúng trong view (dd/MM/yyyy)
            from = f.ToString("dd/MM/yyyy");
            to = t.ToString("dd/MM/yyyy");
        }

        var list = await q.OrderByDescending(x => x.ActionDate).ToListAsync();

        // Trả lại đúng format dd/MM/yyyy để bind ra input (nên dùng input type="text")
        ViewBag.Username = username?.Trim();
        ViewBag.FromDate = from;   // dd/MM/yyyy
        ViewBag.ToDate = to;     // dd/MM/yyyy
        ViewBag.HasFilter = !string.IsNullOrWhiteSpace(username) || hasFrom || hasTo;

        return View(list);
    }
}