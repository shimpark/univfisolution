using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace UnivFI.Application.DTOs
{
    public class UpdateUserDto
    {
        public int Id { get; set; }
        public required string UserName { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }

        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }
    }
}
