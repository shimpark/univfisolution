using System.Collections.Generic;
using UnivFI.Application.DTOs;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.Role
{
    public class RoleListViewModel
    {
        public IEnumerable<RoleDto> Roles { get; set; } = new List<RoleDto>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public string? SearchFields { get; set; }
    }
}