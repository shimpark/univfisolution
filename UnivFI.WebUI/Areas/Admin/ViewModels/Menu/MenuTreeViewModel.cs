using System.Collections.Generic;
using UnivFI.Domain.Entities;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.Menu
{
    public class MenuTreeViewModel
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string MenuKey { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public short? MenuOrder { get; set; }
        public short? Levels { get; set; }
        public bool? UseNewIcon { get; set; }
        public bool HasChildren => Children.Count > 0;
        public List<MenuTreeViewModel> Children { get; set; } = new List<MenuTreeViewModel>();
    }
}