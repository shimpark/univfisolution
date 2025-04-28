using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UnivFI.WebUI.Models.API
{
    /// <summary>
    /// 로그인 요청 모델
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "사용자 이름은 필수입니다.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "비밀번호는 필수입니다.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 로그인 상태 유지 여부
        /// </summary>
        public bool? RememberMe { get; set; }
    }

    /// <summary>
    /// 인증 응답 모델
    /// </summary>
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        [JsonPropertyName("userInfo")]
        public UnivFI.WebUI.Helpers.UserInfo? UserInfo { get; set; }
    }

    /// <summary>
    /// 토큰 갱신 요청 모델
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// 사용자 정보 모델
    /// </summary>
    public class UserInfo
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}