// Areas/Admin/Controllers/SettingsController.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BVUB_WebTuyenDung.Areas.Admin.Services;
using BVUB_WebTuyenDung.Areas.Admin.ViewModels;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,1")]
    public class SettingsController : Controller
    {
        private readonly ISettingsStore _store;
        private readonly IAuditTrailService _audit;

        public SettingsController(ISettingsStore store, IAuditTrailService audit)
        {
            _store = store;
            _audit = audit;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var current = await _store.GetEmailSettingsAsync() ?? new EmailSettings();

            // Form TRỐNG theo yêu cầu (không prefill)
            var vm = new EmailSettingsIndexVM
            {
                Current = current,
                Form = new EmailSettings()
            };

            // Link ứng tuyển status
            ViewBag.LinkVCOpen = await _store.IsLinkOpenAsync("VC");
            ViewBag.LinkNLDOpen = await _store.IsLinkOpenAsync("NLD");

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(EmailSettingsIndexVM vm)
        {
            // Lấy lại Current để hiển thị khi lỗi
            vm.Current = await _store.GetEmailSettingsAsync() ?? new EmailSettings();

            // Chuẩn hóa & validate
            var m = vm.Form ?? new EmailSettings();
            m.Username = m.Username?.Trim();
            m.FromEmail = m.FromEmail?.Trim();
            m.FromName = m.FromName?.Trim();
            m.Password = m.Password?.Trim(); // rỗng = giữ nguyên

            if (string.IsNullOrWhiteSpace(m.Username))
                ModelState.AddModelError("Form.Username", "Username không được để trống.");

            if (string.IsNullOrWhiteSpace(m.FromEmail))
                ModelState.AddModelError("Form.FromEmail", "FromEmail không được để trống.");
            else
            {
                var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(m.FromEmail, pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    ModelState.AddModelError("Form.FromEmail", "FromEmail không đúng định dạng.");
            }

            if (string.IsNullOrWhiteSpace(m.FromName))
                ModelState.AddModelError("Form.FromName", "FromName không được để trống.");

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                // Nếu Password trống -> giữ nguyên
                if (string.IsNullOrWhiteSpace(m.Password))
                {
                    var current = await _store.GetEmailSettingsAsync();
                    m.Password = current?.Password;
                }

                await _store.UpdateEmailSettingsAsync(m, User?.Identity?.Name ?? "system");

                await _audit.LogAsync(User?.Identity?.Name ?? "unknown",
                    "Cập nhật cài đặt Email (Username/FromEmail/FromName; Password " +
                    (string.IsNullOrWhiteSpace(vm.Form?.Password) ? "không đổi" : "đã đổi") + ")");

                TempData["ToastSuccess"] = "Đã lưu cài đặt Email.";
                // PRG: quay lại Index để load DB mới nhất vào cột trái
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Không lưu được cài đặt: " + ex.Message);
                return View(vm);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLink(string type, bool open)
        {
            if (!IsSupportedLinkType(type))
                return BadRequest("Type phải là VC hoặc NLD.");

            await _store.SetLinkStatusAsync(type, open, User?.Identity?.Name ?? "system");

            await _audit.LogAsync(User?.Identity?.Name ?? "unknown",
                $"Toggle link ứng tuyển {type}: {(open ? "MỞ" : "KHÓA")}");

            TempData["ToastSuccess"] = $"Đã {(open ? "mở" : "khóa")} link đăng ký {GetLinkTypeLabel(type)}.";
            return RedirectToAction(nameof(Index));
        }

        private static bool IsSupportedLinkType(string type)
        {
            return type == "VC" || type == "NLD";
        }

        private static string GetLinkTypeLabel(string type)
        {
            return type == "VC" ? "Viên chức" : "Người lao động";
        }

    }
}