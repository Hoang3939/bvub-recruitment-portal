using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace BVUB_WebTuyenDung.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "StaffAndAdmin")]
    [Route("Admin/[controller]/[action]")]
    public class UploadsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] _allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public UploadsController(IWebHostEnvironment env) => _env = env;

        /// <summary>
        /// CKEditor SimpleUploadAdapter endpoint
        /// Nhận file ở field "upload" (theo CKEditor) hoặc file đầu tiên trong form.
        /// Trả JSON dạng { url: "/uploads/huongdan/xxx.jpg" }
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Image()
        {
            try
            {
                var file = Request.Form.Files["upload"] ?? Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = new { message = "Không có tệp tải lên." } });

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExt.Contains(ext))
                    return BadRequest(new { error = new { message = "Chỉ cho phép ảnh (jpg, jpeg, png, gif, webp)." } });

                var folder = Path.Combine(_env.WebRootPath, "uploads", "huongdan");
                Directory.CreateDirectory(folder);

                var safeName = Path.GetFileNameWithoutExtension(file.FileName);
                var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeName}{ext}";
                var path = Path.Combine(folder, fileName);

                using (var fs = new FileStream(path, FileMode.Create))
                    await file.CopyToAsync(fs);

                var url = $"/uploads/huongdan/{fileName}";
                return Json(new { url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { message = "Tải ảnh thất bại: " + ex.Message } });
            }
        }

        /// <summary>
        /// Trả danh sách URL ảnh có sẵn trong thư mục /wwwroot/uploads/huongdan
        /// </summary>
        [HttpGet]
        public IActionResult List()
        {
            try
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "huongdan");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var urls = Directory.EnumerateFiles(folder)
                                    .Where(p => _allowedExt.Contains(Path.GetExtension(p).ToLowerInvariant()))
                                    .OrderByDescending(p => System.IO.File.GetCreationTimeUtc(p))
                                    .Select(p => "/uploads/huongdan/" + Path.GetFileName(p))
                                    .ToArray();

                return Json(urls);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { message = "Không đọc được thư viện ảnh: " + ex.Message } });
            }
        }
    }
}
