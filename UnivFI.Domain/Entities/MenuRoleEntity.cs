using System;
using Dapper.Contrib.Extensions;

namespace UnivFI.Domain.Entities
{
    /// <summary>
    /// MenuRoles 테이블을 표현하는 엔티티 클래스
    /// 메뉴와 역할 간의 다대다 관계를 표현
    /// </summary>
    [Table("MenuRoles")]
    public class MenuRoleEntity
    {
        /// <summary>
        /// 복합 키 - MenuId
        /// </summary>
        [Dapper.Contrib.Extensions.ExplicitKey]
        public int MenuId { get; set; }

        /// <summary>
        /// 복합 키 - RoleId
        /// </summary>
        [Dapper.Contrib.Extensions.ExplicitKey]
        public int RoleId { get; set; }

        // 탐색 속성 - 데이터베이스에 저장되지 않음
        [Write(false)]
        public virtual MenuEntity Menu { get; set; }

        [Write(false)]
        public virtual RoleEntity Role { get; set; }
    }
}