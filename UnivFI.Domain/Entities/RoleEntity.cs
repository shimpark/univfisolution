using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;

namespace UnivFI.Domain.Entities
{
    // 실용적 접근: 인프라스트럭처 관련 속성이지만 기능 동작을 위해 유지
    [Table("Roles")]
    public class RoleEntity
    {
        [Dapper.Contrib.Extensions.Key]
        public int Id { get; set; }
        public string RoleName { get; set; }
        public string RoleComment { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 탐색 속성 - 데이터베이스 저장 제외
        [Write(false)] // 인프라스트럭처 관련 속성
        public ICollection<MenuRoleEntity> MenuRoles { get; set; } = new List<MenuRoleEntity>();

        [Write(false)] // 인프라스트럭처 관련 속성
        public ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();
    }
}