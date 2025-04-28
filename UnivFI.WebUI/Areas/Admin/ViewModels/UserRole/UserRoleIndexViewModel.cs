using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.UserRole
{
    public class UserRoleIndexViewModel
    {
        public IEnumerable<UserRoleViewModel> UserRoles { get; set; }
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string SearchTerm { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

        public List<SelectListItem> UsersList { get; set; }
        public List<SelectListItem> RolesList { get; set; }

        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;
    }
}