using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class DonVienChuc
    {
        [Key]
        public int VienChucId { get; set; }

        [Required(ErrorMessage = "Thiếu ID Ứng viên.")]
        public int UngVienId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Chức danh dự tuyển.")]
        public int ChucDanhDuTuyenId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Vị trí dự tuyển.")]
        public int ViTriDuTuyenId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Khoa/Phòng.")]
        public int KhoaPhongId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Dân tộc.")]
        [StringLength(20, ErrorMessage = "Dân tộc tối đa 20 ký tự.")]
        public string DanToc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tôn giáo.")]
        [StringLength(20, ErrorMessage = "Tôn giáo tối đa 20 ký tự.")]
        public string TonGiao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Quê quán.")]
        [StringLength(255, ErrorMessage = "Quê quán tối đa 255 ký tự.")]
        public string QueQuan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Hộ khẩu.")]
        [StringLength(255, ErrorMessage = "Hộ khẩu tối đa 255 ký tự.")]
        public string HoKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Chiều cao.")]
        public int ChieuCao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Cân nặng.")]
        public int CanNang { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Trình độ văn hóa.")]
        [StringLength(100, ErrorMessage = "Trình độ văn hóa tối đa 100 ký tự.")]
        public string TrinhDoVanHoa { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Loại hình đào tạo.")]
        [StringLength(50, ErrorMessage = "Loại hình đào tạo tối đa 50 ký tự.")]
        public string LoaiHinhDaoTao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Đối tượng ưu tiên.")]
        [StringLength(50, ErrorMessage = "Đối tượng ưu tiên tối đa 50 ký tự.")]
        public string DoiTuongUuTien { get; set; }

        [Required(ErrorMessage = "Ngày nộp bắt buộc.")]
        [DataType(DataType.Date, ErrorMessage = "Ngày nộp không hợp lệ.")]
        public DateTime NgayNop { get; set; }

        public int TrangThai { get; set; } = 0;

        [Required(ErrorMessage = "Vui lòng nhập Mã tra cứu.")]
        [StringLength(50, ErrorMessage = "Mã tra cứu tối đa 50 ký tự.")]
        public string MaTraCuu { get; set; }

        public UngVien? UngVien { get; set; }
        public ICollection<VanBang>? VanBangs { get; set; }
        public DanhMucChucDanhDuTuyen? ChucDanhDuTuyen { get; set; }
        public DanhMucViTriDuTuyen? ViTriDuTuyen { get; set; }
        public DanhMucKhoaPhong? KhoaPhong { get; set; }
    }
}
