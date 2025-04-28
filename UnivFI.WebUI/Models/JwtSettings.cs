namespace UnivFI.WebUI.Models
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int TokenExpiryMinutes { get; set; } = 60; // 기본 1시간
        public int RefreshTokenExpiryDays { get; set; } = 7; // 기본 7일
    }
}