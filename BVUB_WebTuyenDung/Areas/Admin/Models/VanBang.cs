using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class VanBang
    {
        [Key]
        public int VanBangId { get; set; }

        [Display(Name = "Ứng viên")]
        [ForeignKey(nameof(UngVien))]
        public int UngVienId { get; set; }

        [StringLength(50)]
        [Display(Name = "Tên cơ sở đào tạo")]
        public string TenCoSo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Ngày cấp")]
        public DateTime? NgayCap { get; set; }

        [StringLength(20)]
        [Display(Name = "Số hiệu")]
        public string SoHieu { get; set; }

        [StringLength(50)]
        [Display(Name = "Chuyên ngành đào tạo")]
        public string ChuyenNganhDaoTao { get; set; }

        [StringLength(50)]
        [Display(Name = "Ngành đào tạo")]
        public string NganhDaoTao { get; set; }

        [StringLength(50)]
        [Display(Name = "Hình thức đào tạo")]
        public string HinhThucDaoTao { get; set; }

        [StringLength(50)]
        [Display(Name = "Xếp loại")]
        public string XepLoai { get; set; }

        [StringLength(50)]
        [Display(Name = "Loại văn bằng")]
        public string LoaiVanBang { get; set; }

        // Navigation
        public virtual UngVien UngVien { get; set; }
    }
}
