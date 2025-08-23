using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class HuongDanDangKy
    {
        [Key]
        public int HuongDanId { get; set; }

        [Required]
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public string FileHuongDan { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public string LoaiHuongDan { get; set; } // Viên chức hoặc Người lao động
    }
}
