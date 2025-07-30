namespace BVUB_WebTuyenDung.Models
{
    public class HopDongNguoiLaoDong
    {
        public int HopDongId { get; set; }

        public int UngVienId { get; set; }
        public UngVien UngVien { get; set; }

        public string Loai { get; set; }

        public int KhoaPhongCongTacId { get; set; }
        public DanhMucKhoaPhong KhoaPhongCongTac { get; set; }

        public string NoiSinh { get; set; }
        public string ChuyenNganhDaoTao { get; set; }
        public string NamTotNghiep { get; set; }
        public string TrinhDoTinHoc { get; set; }
        public string TrinhDoNgoaiNgu { get; set; }
        public string ChungChiHanhNghe { get; set; }
        public string NgheNghiepTruocTuyenDung { get; set; }
        public DateTime NgayNop { get; set; }
    }

}
