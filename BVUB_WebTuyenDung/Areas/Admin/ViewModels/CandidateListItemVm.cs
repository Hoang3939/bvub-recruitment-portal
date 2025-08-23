namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class CandidateListItemVm
    {
        public int UngVienId { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string LoaiDon { get; set; }      // "Đơn viên chức" / "Hợp đồng lao động"
        public DateTime NgayUngTuyen { get; set; }
        public string TrangThai { get; set; }
        public string TrangThaiClass { get; set; } // pending/approved/cancelled
        public string DonType { get; set; }        // "VC" hoặc "HD"
        public int? DonId { get; set; }            // id bản ghi đơn

    }
}
