using System;

namespace UnivFI.Application.DTOs
{
    public class UserRoleDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public required string UserName { get; set; }
        public required string RoleName { get; set; }

        // 탐색 속성
        public required UserDto User { get; set; }
        public required RoleDto Role { get; set; }
    }

    public class AssignUserRoleDto
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }
}