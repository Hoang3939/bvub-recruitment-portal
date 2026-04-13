using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class HopDongNguoiLaoDong
    {
        [Key]
        public int HopDongId { get; set; }

        [Required(ErrorMessage = "Thiếu ID Ứng viên.")]
        public int UngVienId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Loại hợp đồng.")]
        [StringLength(30, ErrorMessage = "Loại hợp đồng tối đa 30 ký tự.")]
        public string Loai { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Khoa/Phòng công tác.")]
        public int KhoaPhongCongTacId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Nơi sinh.")]
        [StringLength(70, ErrorMessage = "Nơi sinh tối đa 70 ký tự.")]
        public string NoiSinh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Ngành đào tạo")]
        [StringLength(50, ErrorMessage = "Nơi sinh tối đa 50 ký tự.")]
        public string ChuyenNganhDaoTao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Năm tốt nghiệp")]
        [StringLength(50, ErrorMessage = "Năm tốt nghiệp tối đa 50 ký tự.")]
        public string NamTotNghiep { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Trình độ tin học")]
        [StringLength(100, ErrorMessage = "Trình độ tin học tối đa 100 ký tự.")]
        public string TrinhDoTinHoc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Trình độ ngoại ngữ")]
        [StringLength(100, ErrorMessage = "Trình độ ngoại ngữ tối đa 100 ký tự.")]
        public string TrinhDoNgoaiNgu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Chứng chỉ hành nghề")]
        [StringLength(100, ErrorMessage = "Chứng chỉ hành nghề tối đa 100 ký tự.")]
        public string ChungChiHanhNghe { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Nghề nghiệp trước khi tuyển dụng")]
        [StringLength(100, ErrorMessage = "Nghề nghiệp trước khi tuyển dụng tối đa 100 ký tự.")]
        public string NgheNghiepTruocTuyenDung { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Dân tộc.")]
        [StringLength(50, ErrorMessage = "Dân tộc tối đa 50 ký tự.")]
        public string DanToc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tôn giáo.")]
        [StringLength(50, ErrorMessage = "Tôn giáo tối đa 50 ký tự.")]
        public string TonGiao { get; set; }

        public bool DangVien { get; set; } = false;

        [DataType(DataType.Date)]
        public DateTime? NgayVaoDang { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayChinhThuc { get; set; }

        [Required(ErrorMessage = "Ngày nộp bắt buộc.")]
        [DataType(DataType.Date, ErrorMessage = "Ngày nộp không hợp lệ.")]
        public DateTime NgayNop { get; set; }

        public int TrangThai { get; set; } = 0;

        [Required(ErrorMessage = "Vui lòng nhập Mã tra cứu.")]
        [StringLength(50, ErrorMessage = "Mã tra cứu tối đa 50 ký tự.")]
        public string MaTraCuu { get; set; }

        public virtual UngVien? UngVien { get; set; }

        public DanhMucKhoaPhong? KhoaPhongCongTac { get; set; }
    }
}
