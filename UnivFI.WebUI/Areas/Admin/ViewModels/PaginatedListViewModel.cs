namespace UnivFI.WebUI.Areas.Admin.ViewModels
{
    /// <summary>
    /// 페이징 처리된 목록 뷰모델의 기본 클래스
    /// </summary>
    /// <typeparam name="T">항목 뷰모델 타입</typeparam>
    public class PaginatedListViewModel<T> where T : class
    {
        /// <summary>
        /// 현재 페이지의 항목 목록
        /// </summary>
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

        /// <summary>
        /// 현재 페이지 번호
        /// </summary>
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// 전체 페이지 수
        /// </summary>
        public int TotalPages { get; set; } = 1;

        /// <summary>
        /// 페이지당 항목 수
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 전체 항목 수
        /// </summary>
        public int TotalItems { get; set; } = 0;

        /// <summary>
        /// 검색어
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// 검색 필드
        /// </summary>
        public string SearchFields { get; set; } = string.Empty;

        /// <summary>
        /// 이전 페이지 존재 여부
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 다음 페이지 존재 여부
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}