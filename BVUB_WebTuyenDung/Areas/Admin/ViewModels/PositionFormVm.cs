using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Areas.Admin.ViewModels
{
    public class PositionFormVm
    {
        public int ViTriId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên vị trí")]
        public string TenViTri { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chức danh")]
        public int ChucDanhId { get; set; }

        [Range(0, 1)]
        public int TamNgung { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem> AllChucDanhs { get; set; }
    }
}
