using System;

namespace UnivFI.Application.DTOs
{
    public class AuthResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public UserDto? User { get; set; }

        // Token 관련 정보
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
    }
}