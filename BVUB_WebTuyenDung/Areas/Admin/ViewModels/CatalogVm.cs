using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class TitleFormVm
    {
        public int? ChucDanhId { get; set; }
        [Required, MaxLength(70)] public string TenChucDanh { get; set; }
        public int TamNgung { get; set; } = 0;
    }

    public class PositionUpsertVm
    {
        public int? ViTriId { get; set; }
        [Required, MaxLength(70)] public string TenViTri { get; set; }

        [Display(Name = "Chức danh")]
        public int ChucDanhId { get; set; }
        public IEnumerable<SelectListItem> AllChucDanhs { get; set; }

        public int TamNgung { get; set; } = 0;
    }

    public class DepartmentFormVm
    {
        public int? KhoaPhongId { get; set; }

        [Required, MaxLength(70)] public string Ten { get; set; }
        [Required] public string Loai { get; set; }  // "Phòng"/"Khoa"
        public int TamNgung { get; set; } = 0;

        // multi-select các vị trí
        public List<int> SelectedViTriIds { get; set; } = new();
        public IEnumerable<SelectListItem> AllViTris { get; set; }
    }
}
