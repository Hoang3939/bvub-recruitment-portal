using System.Collections.Generic;
using BVUB_WebTuyenDung.Areas.Admin.Models;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class UngTuyenNguoiLaoDongViewModel
    {
        public UngVien UngVien { get; set; } = default!;

        public HopDongNguoiLaoDong HopDongNguoiLaoDong { get; set; } = default!;

        // Văn bằng của Ứng viên
        public List<VanBang> VanBangs { get; set; } = new List<VanBang>();
    }
}
