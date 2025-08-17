using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Models
{
    public class DanhMucKhoaPhong
    {
        [Key]
        public int KhoaPhongId { get; set; }
        public string Ten { get; set; }
        public string Loai { get; set; } // Phòng / Khoa
        public int TamNgung { get; set; }

        public ICollection<KhoaPhongViTri> KhoaPhongViTris { get; set; }
    }

}
