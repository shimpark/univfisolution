using System;
using System.Collections.Generic;

namespace UnivFI.WebUI.ViewModels
{
    /// <summary>
    /// 페이징 컴포넌트를 위한 뷰모델
    /// </summary>
    public class PaginationViewModel
    {
        /// <summary>
        /// 현재 페이지 번호
        /// </summary>
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// 전체 페이지 수
        /// </summary>
        public int TotalPages { get; set; } = 1;

        /// <summary>
        /// 화면에 표시할 페이지 버튼 수
        /// </summary>
        public int DisplayRange { get; set; } = 5;

        /// <summary>
        /// 페이지당 항목 수
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 전체 항목 수
        /// </summary>
        public int TotalItems { get; set; } = 0;

        /// <summary>
        /// 이전 페이지 존재 여부
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 다음 페이지 존재 여부
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// 페이지를 로드할 액션 이름
        /// </summary>
        public string ActionName { get; set; } = "Index";

        /// <summary>
        /// 페이지를 로드할 컨트롤러 이름
        /// </summary>
        public string ControllerName { get; set; } = string.Empty;

        /// <summary>
        /// 페이지를 로드할 영역 이름
        /// </summary>
        public string AreaName { get; set; } = string.Empty;

        /// <summary>
        /// 페이지 로드 시 추가 라우트 데이터
        /// </summary>
        public Dictionary<string, string> RouteData { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 현재 페이지 범위의 시작 항목 번호
        /// </summary>
        public int StartItem => (CurrentPage - 1) * PageSize + 1;

        /// <summary>
        /// 현재 페이지 범위의 끝 항목 번호
        /// </summary>
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

        /// <summary>
        /// 시작 페이지 번호
        /// </summary>
        public int StartPage
        {
            get
            {
                int halfRange = DisplayRange / 2;
                int start = CurrentPage - halfRange;
                return Math.Max(1, start);
            }
        }

        /// <summary>
        /// 끝 페이지 번호
        /// </summary>
        public int EndPage
        {
            get
            {
                int start = StartPage;
                int end = start + DisplayRange - 1;
                return Math.Min(TotalPages, end);
            }
        }
    }
}