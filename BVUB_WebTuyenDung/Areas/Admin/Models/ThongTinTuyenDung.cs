using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public enum TrangThaiTuyenDung
    {
        DangTuyen = 1, // Đang tuyển
        TamAn = 2, // Tạm ẩn
        NgungTuyen = 3, // Ngừng tuyển
        DaDong = 4  // Đã đóng
    }

    [Table("ThongTinTuyenDung")]
    public class ThongTinTuyenDung
    {
        [Key]
        public int TuyenDungId { get; set; }

        [Required, StringLength(255)]
        public string TieuDe { get; set; } = string.Empty;

        [Required] // NVARCHAR(MAX)
        public string NoiDung { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        [DataType(DataType.Date)]
        public DateTime NgayDang { get; set; }

        [Column(TypeName = "date")]
        [DataType(DataType.Date)]
        public DateTime HanNopHoSo { get; set; }

        [Required, StringLength(50)]
        public string LoaiTuyenDung { get; set; } = string.Empty;

        [StringLength(255)]
        public string? FileDinhKem { get; set; }

        [Required]
        public TrangThaiTuyenDung TrangThai { get; set; }

        /* --------- Thuộc tính tiện ích cho UI (không map DB) --------- */
        [NotMapped]
        public string TrangThaiLabel => TrangThai switch
        {
            TrangThaiTuyenDung.DangTuyen => "Đang tuyển",
            TrangThaiTuyenDung.TamAn => "Tạm ẩn",
            TrangThaiTuyenDung.NgungTuyen => "Ngừng tuyển",
            TrangThaiTuyenDung.DaDong => "Đã đóng",
            _ => "Không rõ"
        };

        [NotMapped]
        public string TrangThaiClass => TrangThai switch
        {
            TrangThaiTuyenDung.DangTuyen => "recruiting",
            TrangThaiTuyenDung.TamAn => "hidden",
            TrangThaiTuyenDung.NgungTuyen => "stopped",
            TrangThaiTuyenDung.DaDong => "closed",
            _ => "unknown"
        };
    }
}
