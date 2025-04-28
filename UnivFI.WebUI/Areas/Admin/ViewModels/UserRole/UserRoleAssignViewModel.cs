using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.UserRole
{
    public class UserRoleAssignViewModel
    {
        [Required(ErrorMessage = "사용자를 선택해주세요.")]
        [Range(1, int.MaxValue, ErrorMessage = "사용자를 선택해주세요.")]
        [Display(Name = "사용자")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "역할을 선택해주세요.")]
        [Range(1, int.MaxValue, ErrorMessage = "역할을 선택해주세요.")]
        [Display(Name = "역할")]
        public int RoleId { get; set; }

        public IEnumerable<SelectListItem> Users { get; set; }
        public IEnumerable<SelectListItem> Roles { get; set; }
    }
}