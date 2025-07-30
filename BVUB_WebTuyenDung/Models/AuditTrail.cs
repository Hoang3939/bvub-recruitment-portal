namespace BVUB_WebTuyenDung.Models
{
    public class AuditTrail
    {
        public int AuditTrailId { get; set; }

        public string LoaiDon { get; set; } // Viên chức hoặc Người lao động
        public int DonId { get; set; }

        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhatMoi { get; set; }

        public int AdminCapNhatId { get; set; }
        public AdminUser AdminCapNhat { get; set; }

        public string GhiChu { get; set; }
    }

}
