using System;
using Dapper.Contrib.Extensions;

namespace UnivFI.Domain.Entities
{
    [Table("UserRoles")]
    public class UserRoleEntity
    {
        [Dapper.Contrib.Extensions.ExplicitKey]
        public int UserId { get; set; }

        [Dapper.Contrib.Extensions.ExplicitKey]
        public int RoleId { get; set; }

        // 탐색 속성
        [Write(false)]
        public UserEntity User { get; set; }

        [Write(false)]
        public RoleEntity Role { get; set; }
    }
}