using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class VanBang
    {
        [Key]
        public int VanBangId { get; set; }

        [Required(ErrorMessage = "Thiếu ID Đơn vị viên chức.")]
        public int DonVienChucId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tên cơ sở.")]
        [StringLength(50, ErrorMessage = "Tên cơ sở tối đa 50 ký tự.")]
        public string TenCoSo { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Ngày cấp.")]
        [DataType(DataType.Date, ErrorMessage = "Ngày cấp không hợp lệ.")]
        public DateTime? NgayCap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số hiệu.")]
        [StringLength(20, ErrorMessage = "Số hiệu tối đa 20 ký tự.")]
        public string SoHieu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Chuyên ngành đào tạo.")]
        [StringLength(50, ErrorMessage = "Chuyên ngành đào tạo tối đa 50 ký tự.")]
        public string ChuyenNganhDaoTao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Ngành đào tạo.")]
        [StringLength(50, ErrorMessage = "Ngành đào tạo tối đa 50 ký tự.")]
        public string NganhDaoTao { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Hình thức đào tạo.")]

        [StringLength(50, ErrorMessage = "Hình thức đào tạo tối đa 50 ký tự.")]
        public string HinhThucDaoTao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Xếp loại.")]

        [StringLength(50, ErrorMessage = "Xếp loại tối đa 50 ký tự.")]
        public string XepLoai { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Loại văn bằng.")]
        [StringLength(50, ErrorMessage = "Loại văn bằng tối đa 50 ký tự.")]
        public string LoaiVanBang { get; set; }

        public DonVienChuc? DonVienChuc { get; set; }
    }
}
