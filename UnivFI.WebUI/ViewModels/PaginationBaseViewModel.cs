using System;

namespace UnivFI.WebUI.ViewModels
{
    /// <summary>
    /// 페이징 기능을 확장한 기본 뷰모델
    /// </summary>
    public abstract class PaginationBaseViewModel : BaseViewModel
    {
        // 총 아이템 수
        public int TotalItems { get; set; }

        // 총 페이지 수 (계산 속성)
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        // 페이징 유틸리티 메서드
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public int PreviousPage => Page > 1 ? Page - 1 : 1;
        public int NextPage => Page < TotalPages ? Page + 1 : TotalPages;

        // 표시할 시작 아이템 번호
        public int StartItemIndex => (Page - 1) * PageSize + 1;

        // 표시할 끝 아이템 번호
        public int EndItemIndex => Math.Min(StartItemIndex + PageSize - 1, TotalItems);
    }
}