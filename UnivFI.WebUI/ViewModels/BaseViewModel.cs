namespace UnivFI.WebUI.ViewModels
{
    /// <summary>
    /// 페이징과 검색 속성을 포함하는 기본 ViewModel
    /// </summary>
    public abstract class BaseViewModel
    {
        // 페이징 속성
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // 검색 속성
        public string SearchTerm { get; set; }
        public string SearchFields { get; set; }
    }
}