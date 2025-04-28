using System.Collections.Generic;
using UnivFI.WebUI.ViewModels;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.User
{
    /// <summary>
    /// 사용자 목록 페이지에서 사용되는 페이징 뷰모델
    /// </summary>
    public class UserListViewModel : PaginatedListViewModel<UserViewModel>
    {
        /// <summary>
        /// 사용자 목록 (Items 속성에 대한 더 명확한 이름 제공)
        /// </summary>
        public IEnumerable<UserViewModel> Users
        {
            get => Items;
            set => Items = value;
        }
    }
}