using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class HuongDanDangKy
    {
        [Key]
        public int HuongDanId { get; set; }


        [Required, StringLength(255)]
        public string TieuDe { get; set; } = string.Empty;


        // Lưu HTML từ trình soạn thảo (kiểu Word)
        [Required]
        public string NoiDung { get; set; } = string.Empty;


        // Tùy chọn: đường dẫn file .pdf/.docx nếu muốn đính kèm
        [StringLength(255)]
        public string? FileHuongDan { get; set; }


        [DataType(DataType.Date)]
        public DateTime NgayCapNhat { get; set; }


        [Required, StringLength(50)]
        public string LoaiHuongDan { get; set; } = string.Empty; // "Người lao động" | "Viên chức"
    }
}