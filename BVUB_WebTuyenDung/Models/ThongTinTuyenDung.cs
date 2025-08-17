using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class ThongTinTuyenDung
    {
        [Key]
        public int TuyenDungId { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayDang { get; set; }
        public DateTime HanNopHoSo { get; set; }
        public string LoaiTuyenDung { get; set; }
        public string FileDinhKem { get; set; }
        public int TrangThai { get; set; }
    }

}
