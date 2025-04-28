using System;

namespace UnivFI.Domain.Models
{
    /// <summary>
    /// 검색 조건을 정의하는 클래스
    /// </summary>
    public class SearchCriteria
    {
        /// <summary>
        /// 검색할 필드 이름
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// 검색 연산자 (contains, equals, startsWith, endsWith)
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 검색할 값
        /// </summary>
        public string Value { get; set; }
    }
}