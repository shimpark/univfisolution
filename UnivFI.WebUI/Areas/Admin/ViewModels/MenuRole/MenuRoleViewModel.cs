using System;
using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.MenuRole
{
    public class MenuRoleViewModel
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public int RoleId { get; set; }

        [Display(Name = "메뉴 키")]
        public required string MenuKey { get; set; }

        [Display(Name = "메뉴 제목")]
        public required string MenuTitle { get; set; }

        [Display(Name = "역할 이름")]
        public required string RoleName { get; set; }
    }
}