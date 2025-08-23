using System;
using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class CreateHopDongNguoiLaoDongVm
    {
        [Required] public int UngVienId { get; set; }

        [Required, MaxLength(20)]
        public string Loai { get; set; } = "NguoiLaoDong";

        [Required] public int KhoaPhongCongTacId { get; set; }

        [Required, MaxLength(255)] public string NoiSinh { get; set; }
        [Required, MaxLength(50)] public string ChuyenNganhDaoTao { get; set; }
        [Required, MaxLength(50)] public string NamTotNghiep { get; set; }
        [Required, MaxLength(100)] public string TrinhDoTinHoc { get; set; }
        [Required, MaxLength(100)] public string TrinhDoNgoaiNgu { get; set; }
        [Required, MaxLength(100)] public string ChungChiHanhNghe { get; set; }
        [Required, MaxLength(100)] public string NgheNghiepTruocTuyenDung { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime NgayNop { get; set; } = DateTime.Today;
    }
}
