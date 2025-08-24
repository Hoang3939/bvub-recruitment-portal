using System;
using System.Collections.Generic;

namespace BVUB_WebTuyenDung.Models
{
    public class VanBangRow
    {
        public string TenCoSo { get; set; } = "";
        public DateTime? NgayCap { get; set; }
        public string SoHieu { get; set; } = "";
        public string ChuyenNganhDaoTao { get; set; } = "";
        public string NganhDaoTao { get; set; } = "";
        public string HinhThucDaoTao { get; set; } = "";
        public string XepLoai { get; set; } = "";
    }

    public class VienChucDetailsVM
    {
        // DonVienChuc
        public int VienChucId { get; set; }
        public string MaTraCuu { get; set; } = "";
        public int TrangThai { get; set; }
        public DateTime NgayNop { get; set; }

        // Tên danh mục từ ID
        public string TenChucDanh { get; set; } = "";
        public string TenViTri { get; set; } = "";
        public string TenKhoaPhong { get; set; } = "";

        // Ứng viên
        public UngVien UngVien { get; set; } = new UngVien();

        // Văn bằng
        public List<VanBangRow> VanBangs { get; set; } = new();
    }

    public class NguoiLaoDongDetailsVM
    {
        // HopDongNguoiLaoDong
        public int HopDongId { get; set; }
        public string MaTraCuu { get; set; } = "";
        public int TrangThai { get; set; }
        public DateTime NgayNop { get; set; }
        public string LoaiHopDong { get; set; } = "";

        // Tên khoa phòng công tác từ ID
        public string TenKhoaPhongCongTac { get; set; } = "";

        // Thông tin khác của HĐ
        public string ChuyenNganhDaoTao { get; set; } = "";
        public string NamTotNghiep { get; set; } = "";
        public string TrinhDoTinHoc { get; set; } = "";
        public string TrinhDoNgoaiNgu { get; set; } = "";
        public string ChungChiHanhNghe { get; set; } = "";
        public string NgheNghiepTruocTuyenDung { get; set; } = "";

        // Ứng viên
        public UngVien UngVien { get; set; } = new UngVien();
    }
}
