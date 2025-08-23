using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class DonVienChuc
    {
        [Key]
        public int VienChucId { get; set; }

        // FK
        public int UngVienId { get; set; }
        public UngVien UngVien { get; set; }
        public int ViTriDuTuyenId { get; set; }
        public DanhMucViTriDuTuyen ViTriDuTuyen { get; set; }
        public int ChucDanhDuTuyenId { get; set; }
        public DanhMucChucDanhDuTuyen ChucDanhDuTuyen { get; set; }
        public int KhoaPhongId { get; set; }
        public DanhMucKhoaPhong KhoaPhong { get; set; }

        // Fields
        [Required, MaxLength(20)]
        public string DanToc { get; set; }

        [Required, MaxLength(20)]
        public string TonGiao { get; set; }

        [Required, MaxLength(255)]
        public string QueQuan { get; set; }

        [MaxLength(255)]
        public string HoKhau { get; set; }

        public int ChieuCao { get; set; }
        public int CanNang { get; set; }

        [Required, MaxLength(100)]
        public string TrinhDoVanHoa { get; set; }

        [Required, MaxLength(50)]
        public string LoaiHinhDaoTao { get; set; }

        [Required, MaxLength(50)]
        public string DoiTuongUuTien { get; set; }

        [Column(TypeName = "date")]
        public DateTime NgayNop { get; set; }

        public int TrangThai { get; set; } = 0;

        [Required, MaxLength(50)]
        public string MaTraCuu { get; set; }

        // ===== Navigation properties =====
        public virtual ICollection<VanBang> VanBangs { get; set; } = new List<VanBang>();

    }
}
