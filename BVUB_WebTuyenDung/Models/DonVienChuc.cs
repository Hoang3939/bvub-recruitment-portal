using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class DonVienChuc
    {
        [Key]
        public int VienChucId { get; set; }

        [Required]
        public int UngVienId { get; set; }

        [Required]
        public int ChucDanhDuTuyenId { get; set; }

        [Required]
        public int ViTriDuTuyenId { get; set; }

        [Required]
        public int KhoaPhongId { get; set; }

        [Required, StringLength(20)]
        public string DanToc { get; set; }

        [Required, StringLength(20)]
        public string TonGiao { get; set; }

        [Required, StringLength(255)]
        public string QueQuan { get; set; }

        [StringLength(255)]
        public string HoKhau { get; set; }

        [Required]
        public int ChieuCao { get; set; }

        [Required]
        public int CanNang { get; set; }

        [Required, StringLength(100)]
        public string TrinhDoVanHoa { get; set; }

        [Required, StringLength(50)]
        public string LoaiHinhDaoTao { get; set; }

        [Required, StringLength(50)]
        public string DoiTuongUuTien { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime NgayNop { get; set; }

        public int TrangThai { get; set; } = 0;
        [Required, StringLength(50)]
        public string MaTraCuu { get; set; }

        public UngVien? UngVien { get; set; }
        public ICollection<VanBang>? VanBangs { get; set; }
    }
}
    