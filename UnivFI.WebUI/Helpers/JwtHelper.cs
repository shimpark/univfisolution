using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UnivFI.WebUI.Models;
using UnivFI.WebUI.Models.API;

namespace UnivFI.WebUI.Helpers
{
    public class JwtHelper
    {
        private readonly JwtSettings _jwtSettings;

        public JwtHelper(JwtSettings jwtSettings)
        {
            _jwtSettings = jwtSettings;
        }

        /// <summary>
        /// JWT 토큰 생성
        /// </summary>
        public string GenerateJwtToken(UserInfo userInfo)
        {
            var tokenHandler = new JwtSecurityTokenHandler(); // Jwt 토큰 핸들러 인스턴스 생성
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key); // 키를 ASCII로 인코딩하여 바이트 배열로 변환

            var claims = new List<Claim> // 클레임 리스트 생성
            {
                new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString()), // 사용자 ID에 대한 클레임 추가
                new Claim(ClaimTypes.Name, userInfo.UserName), // 사용자 이름에 대한 클레임 추가
                new Claim(ClaimTypes.Email, userInfo.Email), // 이메일에 대한 클레임 추가

                new Claim("sub", userInfo.Id.ToString()), // "sub" 클레임 추가
                new Claim("name", userInfo.Name), // "name" 클레임 추가
                new Claim(JwtRegisteredClaimNames.Email, userInfo.Email), // 이메일에 대한 JWT 등록된 클레임 추가
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // 고유 식별자에 대한 JWT 등록된 클레임 추가
            };

            foreach (var role in userInfo.Roles) // 사용자의 역할에 대해 반복
            {
                claims.Add(new Claim(ClaimTypes.Role, role)); // 역할에 대한 클레임 추가
                claims.Add(new Claim("role", role)); // "role" 클레임 추가
            }

            var tokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpiryMinutes); // 토큰 만료 시간 설정

            var tokenDescriptor = new SecurityTokenDescriptor // 토큰 디스크립터 생성
            {
                Subject = new ClaimsIdentity(claims), // 클레임을 가지는 신분 설정
                Expires = tokenExpiry, // 토큰 만료 시간 설정
                Issuer = _jwtSettings.Issuer, // 발급자 설정
                Audience = _jwtSettings.Audience, // 대상 설정
                SigningCredentials = new SigningCredentials( // 서명 자격 증명 설정
                    new SymmetricSecurityKey(key), // 대칭 키 설정
                    SecurityAlgorithms.HmacSha256Signature) // 서명 알고리즘 설정
            };

            var token = tokenHandler.CreateToken(tokenDescriptor); // 토큰 생성
            return tokenHandler.WriteToken(token); // 토큰을 문자열로 변환하여 반환


        }

        /// <summary>
        /// 리프레시 토큰 생성
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// 토큰에서 사용자 ID 추출
        /// </summary>
        public int? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidAudience = _jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return int.Parse(((JwtSecurityToken)validatedToken).Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
            }
            catch
            {
                return null;
            }

        }

        /// <summary>
        /// 토큰 만료 시간 계산
        /// </summary>
        public DateTime CalculateTokenExpiry()
        {
            return DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpiryMinutes);
        }

        /// <summary>
        /// 리프레시 토큰 만료 시간 계산
        /// </summary>
        public DateTime CalculateRefreshTokenExpiry()
        {
            return DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        }
    }
}