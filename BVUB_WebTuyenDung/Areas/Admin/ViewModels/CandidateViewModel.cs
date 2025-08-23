using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class CandidateViewModel
    {
        public int CandidateId { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public DateTime NgayUngTuyen { get; set; }
        public string TrangThai { get; set; }
    }
}

