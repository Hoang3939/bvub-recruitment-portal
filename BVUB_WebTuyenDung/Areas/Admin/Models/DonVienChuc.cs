using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    [Table("DonVienChuc")]
    public class DonVienChuc
    {
        [Key]
        public int VienChucId { get; set; }

        public int UngVienId { get; set; }
        public int ViTriDuTuyenId { get; set; }
        public int KhoaPhongId { get; set; }

        public string DanToc { get; set; }
        public string TonGiao { get; set; }
        public string QueQuan { get; set; }
        public string HoKhau { get; set; }
        public int ChieuCao { get; set; }
        public int CanNang { get; set; }

        public string TrinhDoVanHoa { get; set; }
        public string LoaiHinhDaoTao { get; set; }
        public string DoiTuongUuTien { get; set; }

        public DateTime NgayNop { get; set; }
        public int TrangThai { get; set; }  
        public string? MaTraCuu { get; set; }
        public int ChucDanhDuTuyenId { get; set; }

        [ForeignKey(nameof(UngVienId))]
        public BVUB_WebTuyenDung.Areas.Admin.Models.UngVien? UngVien { get; set; }
    }
}
