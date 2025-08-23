using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class DepartmentFrmVm
    {
        public int KhoaPhongId { get; set; }

        [Display(Name = "Tên")]
        [Required(ErrorMessage = "Vui lòng nhập tên khoa/phòng")]
        public string Ten { get; set; }

        [Display(Name = "Loại")]
        public string Loai { get; set; } = "Khoa";

        [Display(Name = "Trạng thái")]
        public int TamNgung { get; set; } = 0;

        // Chọn chức danh để lọc vị trí
        [Display(Name = "Chức danh")]
        public int? SelectedChucDanhId { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem> AllChucDanhs { get; set; } = new List<SelectListItem>();

        // Danh sách ID vị trí được tick
        [Display(Name = "Vị trí")]
        public List<int> SelectedViTriIds { get; set; } = new();
    }
}
