using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class HuongDanDangKy
    {
        [Key]
        public int HuongDanId { get; set; }

        [Required, StringLength(255)]
        public string TieuDe { get; set; } = string.Empty;

        [Required]
        public string NoiDung { get; set; } = string.Empty;

        [StringLength(255)]
        public string? FileHuongDan { get; set; }

        [DataType(DataType.Date)]
        public DateTime NgayCapNhat { get; set; }

        [Required, StringLength(50)]
        public string LoaiHuongDan { get; set; } = string.Empty; // "Người lao động" | "Viên chức"
    }
}
