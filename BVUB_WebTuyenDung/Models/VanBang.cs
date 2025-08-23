using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class VanBang
    {
        [Key]
        public int VanBangId { get; set; }

        [Required]
        public int DonVienChucId { get; set; }

        [Required, StringLength(50)]
        public string TenCoSo { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? NgayCap { get; set; }

        [StringLength(20)]
        public string SoHieu { get; set; }

        [StringLength(50)]
        public string ChuyenNganhDaoTao { get; set; }

        [StringLength(50)]
        public string NganhDaoTao { get; set; }

        [StringLength(50)]
        public string HinhThucDaoTao { get; set; }

        [StringLength(50)]
        public string XepLoai { get; set; }

        [Required, StringLength(50)]
        public string LoaiVanBang { get; set; }


        public DonVienChuc? DonVienChuc { get; set; }
    }
}
