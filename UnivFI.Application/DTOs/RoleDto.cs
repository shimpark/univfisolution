using System;
using System.Collections.Generic;

namespace UnivFI.Application.DTOs
{
    public class RoleDto
    {
        public int Id { get; set; }
        public required string RoleName { get; set; }
        public string? RoleComment { get; set; }
    }

    public class CreateRoleDto
    {
        public required string RoleName { get; set; }
        public string? RoleComment { get; set; }
    }

    public class UpdateRoleDto
    {
        public int Id { get; set; }
        public required string RoleName { get; set; }
        public string? RoleComment { get; set; }
    }
}