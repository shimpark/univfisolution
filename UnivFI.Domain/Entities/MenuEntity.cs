using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;

namespace UnivFI.Domain.Entities
{
    // 실용적 접근: 인프라스트럭처 관련 속성이지만 기능 동작을 위해 유지
    [Table("Menus")]
    public class MenuEntity
    {
        [Dapper.Contrib.Extensions.Key] // 인프라스트럭처 관련 속성
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public int MenuOrder { get; set; }
        public string MenuKey { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public short? Levels { get; set; }

        // UseNewIcon은 테이블 스키마에서 bit 타입으로 정의됨
        public bool UseNewIcon { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 탐색 속성 - 데이터베이스 저장 제외
        [Write(false)] // 인프라스트럭처 관련 속성
        public virtual ICollection<MenuRoleEntity> MenuRoles { get; set; } = new List<MenuRoleEntity>();
    }
}