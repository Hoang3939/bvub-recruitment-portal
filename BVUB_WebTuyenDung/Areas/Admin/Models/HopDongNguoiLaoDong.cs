using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Areas.Admin.Models
{
    public class HopDongNguoiLaoDong
    {
        [Key]
        public int HopDongId { get; set; }

        public int UngVienId { get; set; }
        public string Loai { get; set; }  
        public int KhoaPhongCongTacId { get; set; }
        public string NoiSinh { get; set; }
        public string ChuyenNganhDaoTao { get; set; }
        public string NamTotNghiep { get; set; }
        public string TrinhDoTinHoc { get; set; }
        public string TrinhDoNgoaiNgu { get; set; }
        public string ChungChiHanhNghe { get; set; }
        public string NgheNghiepTruocTuyenDung { get; set; }
        public DateTime NgayNop { get; set; }
        public int TrangThai { get; set; }
        public string? MaTraCuu { get; set; }

        [ForeignKey(nameof(UngVienId))]
        public BVUB_WebTuyenDung.Areas.Admin.Models.UngVien? UngVien { get; set; }
    }
}
