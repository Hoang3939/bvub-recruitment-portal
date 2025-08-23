using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class AuditTrail
    {
        [Key]
        public int AuditTrailId { get; set; }

        [Required, MaxLength(50)]
        public string LoaiDon { get; set; }      // 'VienChuc' | 'NguoiLaoDong'

        [Required]
        public int DonId { get; set; }

        [Column(TypeName = "date")]
        public DateTime NgayTao { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgayCapNhatMoi { get; set; }

        // ĐỂ NULL ĐƯỢC
        public int? AdminCapNhatId { get; set; }
        public AdminUser AdminCapNhat { get; set; }

        public string GhiChu { get; set; }
    }
}
