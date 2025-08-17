using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class AuditTrail
    {
        [Key]
        public int AuditTrailId { get; set; }

        public string LoaiDon { get; set; }      
        public int DonId { get; set; }             
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhatMoi { get; set; }
        public int AdminCapNhatId { get; set; }
        public string? GhiChu { get; set; }

        // Nếu muốn liên kết tới AdminUser (trong Areas):
        [ForeignKey(nameof(AdminCapNhatId))]
        public BVUB_WebTuyenDung.Areas.Admin.Models.AdminUser? AdminCapNhat { get; set; }
    }
}
