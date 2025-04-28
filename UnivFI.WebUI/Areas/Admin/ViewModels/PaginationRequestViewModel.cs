using System;
using System.Collections.Generic;

namespace UnivFI.WebUI.Areas.Admin.ViewModels
{
    /// <summary>
    /// 페이지네이션 및 검색 요청에 대한 공통 정보를 담는 뷰모델
    /// </summary>
    public class PaginationRequestViewModel
    {
        /// <summary>
        /// 돌아갈 페이지 번호
        /// </summary>
        public int ReturnPage { get; set; } = 1;

        /// <summary>
        /// 돌아갈 때 적용할 페이지 크기
        /// </summary>
        public int ReturnPageSize { get; set; } = 10;

        /// <summary>
        /// 돌아갈 때 적용할 검색어
        /// </summary>
        public string? ReturnSearchTerm { get; set; }

        /// <summary>
        /// 돌아갈 때 적용할 검색 필드
        /// </summary>
        public string? ReturnSearchFields { get; set; }

        /// <summary>
        /// 라우트 파라미터로 변환
        /// </summary>
        /// <returns>라우트 파라미터 딕셔너리</returns>
        public object ToRouteValues()
        {
            return new
            {
                page = ReturnPage,
                pageSize = ReturnPageSize,
                searchTerm = ReturnSearchTerm,
                searchFields = ReturnSearchFields
            };
        }
    }
}