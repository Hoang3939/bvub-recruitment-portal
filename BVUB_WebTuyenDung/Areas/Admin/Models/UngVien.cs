using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class UngVien
    {
        [Key]
        public int UngVienId { get; set; }

        [Required, MaxLength(50)]
        public string HoTen { get; set; }

        [Required]
        [Column(TypeName = "date")]             
        public DateTime NgaySinh { get; set; }

        [Required]
        public int GioiTinh { get; set; }       

        [Required, MaxLength(20)]
        public string SoDienThoai { get; set; }

        [Required, MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; }         

        [Required, MaxLength(20)]
        public string CCCD { get; set; }

        [Required]
        [Column(TypeName = "date")]              
        public DateTime NgayCapCCCD { get; set; }

        [Required, MaxLength(255)]
        public string NoiCapCCCD { get; set; }

        [Required, MaxLength(255)]
        public string DiaChiThuongTru { get; set; }

        [Required, MaxLength(255)]
        public string DiaChiCuTru { get; set; }

        [Required, MaxLength(20)]
        public string MaSoThue { get; set; }

        [Required, MaxLength(20)]
        public string SoTaiKhoan { get; set; }

        [Required, MaxLength(50)]
        public string TinhTrangSucKhoe { get; set; }

        [Required, MaxLength(100)]
        public string TrinhDoChuyenMon { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime NgayUngTuyen { get; set; }
    }
}
