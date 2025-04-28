using System;
using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.Role
{
    public class RoleCreateViewModel
    {
        [Required(ErrorMessage = "역할 이름은 필수 입력 항목입니다.")]
        [StringLength(100, ErrorMessage = "역할명은 최대 100자까지 입력 가능합니다.")]
        [Display(Name = "역할 이름")]
        public string RoleName { get; set; }

        [StringLength(1000, ErrorMessage = "설명은 최대 1000자까지 입력 가능합니다.")]
        [Display(Name = "설명")]
        public string RoleComment { get; set; }

        [StringLength(20, ErrorMessage = "역할 색상은 최대 20자까지 입력 가능합니다.")]
        [Display(Name = "역할 색상")]
        public string RoleColor { get; set; }

        [Display(Name = "정렬 순서")]
        public int? RoleOrder { get; set; }

        [Display(Name = "사용 여부")]
        public bool UseYn { get; set; } = true;
    }
}