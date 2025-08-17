using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Models
{
    public class DanhMucViTriDuTuyen
    {
        [Key]
        public int ViTriId { get; set; }
        public string TenViTri { get; set; }
        public int ChucDanhId { get; set; }
        public int TamNgung { get; set; }

        public DanhMucChucDanhDuTuyen ChucDanh { get; set; }

        public ICollection<KhoaPhongViTri> KhoaPhongViTris { get; set; }
    }

}
