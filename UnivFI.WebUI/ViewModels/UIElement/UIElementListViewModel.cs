using System.Collections.Generic;

namespace UnivFI.WebUI.ViewModels.UIElement
{
    /// <summary>
    /// UI 요소 목록 페이지를 위한 ViewModel
    /// </summary>
    public class UIElementListViewModel : PaginationBaseViewModel
    {
        /// <summary>
        /// UI 요소 항목 목록
        /// </summary>
        public List<UIElementViewModel> Items { get; set; } = new List<UIElementViewModel>();

        /// <summary>
        /// 정렬 필드
        /// </summary>
        public string SortField { get; set; }

        /// <summary>
        /// 정렬 방향 (asc 또는 desc)
        /// </summary>
        public string SortOrder { get; set; } = "asc";
    }
}