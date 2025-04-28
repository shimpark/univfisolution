using System.Collections.Generic;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.MenuRole
{
    /// <summary>
    /// 메뉴 역할 목록 페이지에서 사용되는 페이징 뷰모델
    /// </summary>
    public class MenuRoleListViewModel : PaginatedListViewModel<MenuRoleViewModel>
    {
        /// <summary>
        /// 메뉴 역할 목록 (Items 속성에 대한 더 명확한 이름 제공)
        /// </summary>
        public IEnumerable<MenuRoleViewModel> MenuRoles
        {
            get => Items;
            set => Items = value;
        }
    }
}