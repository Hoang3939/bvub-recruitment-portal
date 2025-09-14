using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class AuditTrail
    {
        [Key]
        public int AuditTrailId { get; set; }

        [Required, MaxLength(100)]
        public string UserName { get; set; }   // Người thao tác (username)

        [Required, MaxLength(255)]
        public string Action { get; set; }     // Thao tác: "Thêm phòng X", "Xóa vị trí Y", ...

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime ActionDate { get; set; } = DateTime.Now;  // mặc định gán current time
    }
}