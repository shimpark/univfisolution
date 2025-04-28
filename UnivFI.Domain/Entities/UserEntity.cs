using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;

namespace UnivFI.Domain.Entities
{
    // 실용적 접근: 인프라스트럭처 관련 속성이지만 기능 동작을 위해 유지
    [Table("Users")]
    public class UserEntity
    {
        [Key]
        public int Id { get; set; }
        public string UserName { get; set; }
        [Write(true)]  // 명시적으로 쓰기 가능하도록 설정
        public string Password { get; set; }
        [Write(true)]  // 명시적으로 쓰기 가능하도록 설정
        public string Salt { get; set; }
        [Write(true)]  // 명시적으로 쓰기 가능하도록 설정
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 탐색 속성 - 데이터베이스 저장 제외
        [Write(false)] // 인프라스트럭처 관련 속성
        public ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();
    }
}
