namespace BVUB_WebTuyenDung.Models
{
    public class UngTuyenVienChucViewModel
    {
        public UngVien UngVien { get; set; } = new UngVien();
        public DonVienChuc DonVienChuc { get; set; } = new DonVienChuc();
        public List<VanBang> VanBangs { get; set; } = new List<VanBang>();

        public int? SelectedChucDanhId { get; set; }  // để biết đang chọn chức danh nào

        // DÙNG CHO Ô “Tình trạng sức khỏe: Khác”
        public string? TinhTrangSucKhoeKhac { get; set; }
    }
}