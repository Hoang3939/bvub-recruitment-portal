using System.Collections.Generic;
using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class UngTuyenVienChucViewModel
    {
        public UngVien UngVien { get; set; } = new();
        public DonVienChuc DonVienChuc { get; set; } = new();
        public List<VanBang> VanBangs { get; set; } = new();

        // Để biết đang chọn chức danh nào
        public int? SelectedChucDanhId { get; set; }

        // Ô “Tình trạng sức khỏe: Khác”
        public string? TinhTrangSucKhoeKhac { get; set; }

        // Ô “Trình độ văn hóa: Khác” 
        public string? TrinhDoVanHoaKhac { get; set; }
    }
}
