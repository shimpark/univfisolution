using System;

namespace UnivFI.Application.DTOs
{
    public class MenuRoleDto
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public int RoleId { get; set; }
        public required string MenuKey { get; set; }
        public required string MenuTitle { get; set; }
        public required string MenuUrl { get; set; }
        public required string RoleName { get; set; }
    }

    public class AssignMenuRoleDto
    {
        public int MenuId { get; set; }
        public int RoleId { get; set; }
    }
}