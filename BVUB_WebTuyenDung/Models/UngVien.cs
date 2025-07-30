namespace BVUB_WebTuyenDung.Models
{
    public class UngVien
    {
        public int UngVienId { get; set; }
        public string HoTen { get; set; }
        public DateTime NgaySinh { get; set; }
        public bool GioiTinh { get; set; }
        public string SoDienThoai { get; set; }
        public string Email { get; set; }
        public string CCCD { get; set; }
        public DateTime NgayCapCCCD { get; set; }
        public string NoiCapCCCD { get; set; }
        public string DiaChiThuongTru { get; set; }
        public string DiaChiCuTru { get; set; }
        public string MaSoThue { get; set; }
        public string SoTaiKhoan { get; set; }
        public string TinhTrangSucKhoe { get; set; }
        public string TrinhDoChuyenMon { get; set; }
    }

}
