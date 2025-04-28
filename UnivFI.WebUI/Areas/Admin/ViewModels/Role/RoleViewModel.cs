using System;
using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.Role
{
    public class RoleViewModel
    {
        public int Id { get; set; }

        [Display(Name = "역할 이름")]
        public string RoleName { get; set; }

        [Display(Name = "설명")]
        public string RoleComment { get; set; }

        [Display(Name = "역할 색상")]
        public string RoleColor { get; set; }

        [Display(Name = "정렬 순서")]
        public int? RoleOrder { get; set; }

        [Display(Name = "사용 여부")]
        public bool UseYn { get; set; } = true;
    }
}