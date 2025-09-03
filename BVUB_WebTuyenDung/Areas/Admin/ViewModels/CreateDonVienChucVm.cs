using System;
using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class CreateDonVienChucVm
    {
        [Required] public int UngVienId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn vị trí dự tuyển")]
        public int ViTriDuTuyenId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chức danh dự tuyển")]
        public int ChucDanhDuTuyenId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khoa/phòng")]
        public int KhoaPhongId { get; set; }

        [Required, MaxLength(20)] public string DanToc { get; set; }
        [Required, MaxLength(20)] public string TonGiao { get; set; }
        [Required, MaxLength(255)] public string QueQuan { get; set; }
        [MaxLength(255)] public string HoKhau { get; set; }

        [Required] public int ChieuCao { get; set; }
        [Required] public int CanNang { get; set; }

        [Required, MaxLength(100)] public string TrinhDoVanHoa { get; set; }
        [Required, MaxLength(50)] public string LoaiHinhDaoTao { get; set; }
        [Required, MaxLength(50)] public string DoiTuongUuTien { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime NgayNop { get; set; } = DateTime.Now;
    }
}
