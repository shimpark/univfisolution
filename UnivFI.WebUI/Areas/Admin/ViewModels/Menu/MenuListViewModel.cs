using System.Collections.Generic;
using UnivFI.Domain.Entities;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.Menu
{
    public class MenuListViewModel
    {
        public IEnumerable<MenuViewModel> Menus { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public string SearchTerm { get; set; }
        public string SearchFields { get; set; }
    }
}