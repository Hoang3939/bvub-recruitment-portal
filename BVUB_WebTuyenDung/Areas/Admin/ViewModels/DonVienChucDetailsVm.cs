using System.Collections.Generic;
using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class DonVienChucDetailsVm
    {
        // <-- Ứng viên -->
        public UngVien UngVien { get; set; } = default!;

        // <-- Đơn viên chức -->
        public DonVienChuc Don { get; set; } = default!;

        // <-- Nhãn + CSS trạng thái để render badge -->
        public string TrangThaiLabel { get; set; } = "";
        public string TrangThaiClass { get; set; } = "";

        // <-- Danh sách văn bằng của Ứng viên (dùng ở phần III. Văn bằng trong View) -->
        public ICollection<VanBang> VanBangs { get; set; } = new List<VanBang>();
    }
}
