using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public enum DinhDanhType
    {
        CCCD,
        Gmail,
        SDT
    }

    public class TraCuuRow
    {
        public int Id { get; set; }                 // khóa chính của đơn
        public string TenDon { get; set; } = "";    // ví dụ: "Đơn ứng tuyển viên chức"
        public string TrangThai { get; set; } = ""; // ví dụ: "Đang duyệt", "Đã tiếp nhận", ...
    }

    public class TraCuuViewModel
    {
        [Display(Name = "Xác thực bằng")]
        [Required]
        public DinhDanhType DinhDanh { get; set; } = DinhDanhType.CCCD;

        [Display(Name = "Giá trị (CCCD/Gmail/SĐT)")]
        [Required(ErrorMessage = "Vui lòng nhập giá trị tương ứng")]
        public string GiaTri { get; set; } = "";

        [Display(Name = "Mã tra cứu")]
        [Required(ErrorMessage = "Vui lòng nhập mã tra cứu")]
        public string MaTraCuu { get; set; } = "";

        [Display(Name = "Mã captcha")]
        [Required(ErrorMessage = "Vui lòng nhập mã captcha")]
        public string CaptchaInput { get; set; } = "";

        // Hiển thị câu hỏi captcha: ví dụ "7 + 3 = ?"
        public string CaptchaQuestion { get; set; } = "";

        // Kết quả tra cứu (0..n đơn — nhưng theo mô tả thường là 1)
        public List<TraCuuRow> KetQua { get; set; } = new();
    }
}
