namespace BVUB_WebTuyenDung.Models
{
    public class DonVienChuc
    {
        public int VienChucId { get; set; }
        public int UngVienId { get; set; }
        public UngVien UngVien { get; set; }

        public int ViTriDuTuyenId { get; set; }
        public DanhMucViTriDuTuyen ViTriDuTuyen { get; set; }

        public string ChucDanhDuTuyen { get; set; }
        public int KhoaPhongId { get; set; }
        public DanhMucKhoaPhong KhoaPhong { get; set; }

        public string DanToc { get; set; }
        public string TonGiao { get; set; }
        public string QueQuan { get; set; }
        public string HoKhau { get; set; }
        public int ChieuCao { get; set; }
        public int CanNang { get; set; }
        public string TrinhDoVanHoa { get; set; }
        public string LoaiHinhDaoTao { get; set; }
        public string DoiTuongUuTien { get; set; }
        public DateTime NgayNop { get; set; }

        public ICollection<VanBang> VanBangs { get; set; }
    }

}
