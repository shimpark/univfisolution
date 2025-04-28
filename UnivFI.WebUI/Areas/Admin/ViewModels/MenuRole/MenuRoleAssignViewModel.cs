using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.MenuRole
{
    public class MenuRoleAssignViewModel
    {
        [Required(ErrorMessage = "메뉴를 선택해주세요.")]
        [Display(Name = "메뉴")]
        public int MenuId { get; set; }

        [Required(ErrorMessage = "역할을 선택해주세요.")]
        [Display(Name = "역할")]
        public int RoleId { get; set; }

        public IEnumerable<SelectListItem> Menus { get; set; }
        public IEnumerable<SelectListItem> Roles { get; set; }
    }
}