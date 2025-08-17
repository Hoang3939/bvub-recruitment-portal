using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class DanhMucKhoaPhong
    {
        [Key]
        public int KhoaPhongId { get; set; }
        [Required]
        public string Ten { get; set; }
        public string Loai { get; set; } // Phòng hoặc Khoa
    }

}
