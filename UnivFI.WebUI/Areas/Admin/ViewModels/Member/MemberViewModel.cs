using System;
using System.Collections.Generic;
using X.PagedList;
using UnivFI.WebUI.Areas.Admin.ViewModels.Common;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.Member
{
    public class MemberViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm");
        public string FormattedUpdatedAt => UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";

        // 목록으로 돌아갈 때 사용할 검색 파라미터
        public SearchViewModel? ReturnSearch { get; set; }
    }

    public class MemberListViewModel
    {
        public IPagedList<MemberViewModel> Members { get; set; } = null!;
        public string SearchTerm { get; set; } = string.Empty;
        public string? SearchFields { get; set; }
        public string? CurrentSort { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }  // 읽기/쓰기 가능하도록 변경

        // 페이징 관련 편의 속성들
        public int TotalPages => Members?.PageCount ?? 0;
        public bool HasPreviousPage => Members?.HasPreviousPage ?? false;
        public bool HasNextPage => Members?.HasNextPage ?? false;
        public int TotalItemCount => Members?.TotalItemCount ?? 0;
    }
}