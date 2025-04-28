using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.Common
{
    public class SearchViewModel
    {
        public int Page { get; set; } = 1;

        [Display(Name = "페이지 크기")]
        public int PageSize { get; set; } = 10;

        [Display(Name = "검색어")]
        public string? SearchTerm { get; set; }

        [Display(Name = "검색 필드")]
        public string? SearchFields { get; set; }

        [Display(Name = "정렬")]
        public string? SortOrder { get; set; }

        public SearchViewModel()
        {
        }

        public SearchViewModel(int? page, int? pageSize, string? searchTerm, string? searchFields, string? sortOrder)
        {
            Page = page ?? 1;
            PageSize = pageSize ?? 10;
            SearchTerm = searchTerm;
            SearchFields = searchFields;
            SortOrder = sortOrder;
        }

        public Dictionary<string, string?> ToRouteValues()
        {
            return new Dictionary<string, string?>
            {
                { nameof(Page), Page.ToString() },
                { nameof(PageSize), PageSize.ToString() },
                { nameof(SearchTerm), SearchTerm },
                { nameof(SearchFields), SearchFields },
                { nameof(SortOrder), SortOrder }
            };
        }
    }
}