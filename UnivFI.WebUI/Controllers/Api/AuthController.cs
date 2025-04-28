using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.WebUI.Helpers;
using UnivFI.WebUI.Models;
using UnivFI.WebUI.Models.API;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace UnivFI.WebUI.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly JwtHelper _jwtHelper;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            IUserRoleService userRoleService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _jwtHelper = new JwtHelper(jwtSettings.Value);
            _logger = logger;
        }

        /// <summary>
        /// 로그인 및 토큰 발급
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "유효하지 않은 요청입니다."
                });
            }

            try
            {
                // 사용자 인증
                var authResult = await _userService.AuthenticateAsync(request.UserName, request.Password);
                if (!authResult.Success || authResult.User == null)
                {
                    return Unauthorized(new AuthResponse
                    {
                        Success = false,
                        Message = authResult.ErrorMessage ?? "인증에 실패했습니다."
                    });
                }

                // 사용자 역할 조회
                var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(authResult.User.Id);
                var roles = userRoles.Select(ur => ur.RoleName).Where(r => !string.IsNullOrEmpty(r)).ToList();

                // 사용자 정보 매핑
                var userInfo = new UnivFI.WebUI.Helpers.UserInfo
                {
                    Id = authResult.User.Id,
                    UserName = request.UserName, // 로그인 요청의 사용자명 사용
                    Name = authResult.User.Name,
                    Email = authResult.User.Email,
                    Roles = roles
                };

                // 토큰 생성
                var token = _jwtHelper.GenerateJwtToken(userInfo);
                var refreshToken = _jwtHelper.GenerateRefreshToken();
                var tokenExpiry = _jwtHelper.CalculateTokenExpiry();
                var refreshTokenExpiry = _jwtHelper.CalculateRefreshTokenExpiry();

                // 리프레시 토큰 저장
                await _userService.SaveRefreshTokenAsync(authResult.User.Id, refreshToken, refreshTokenExpiry);

                _logger.LogInformation($"API 로그인 완료: 사용자={request.UserName}, 역할={string.Join(",", roles)}");

                // 응답 생성
                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "로그인 성공",
                    Token = token,
                    RefreshToken = refreshToken,
                    TokenExpiry = tokenExpiry,
                    RefreshTokenExpiry = refreshTokenExpiry,
                    UserInfo = userInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그인 처리 중 오류 발생");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "서버 오류가 발생했습니다."
                });
            }
        }

        /// <summary>
        /// 토큰 갱신
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "유효하지 않은 요청입니다."
                });
            }

            try
            {
                // 토큰에서 사용자 ID 추출
                var userId = _jwtHelper.GetUserIdFromToken(request.Token);
                if (!userId.HasValue)
                {
                    return Unauthorized(new AuthResponse
                    {
                        Success = false,
                        Message = "유효하지 않은 토큰입니다."
                    });
                }

                // 리프레시 토큰 검증
                var authResult = await _userService.RefreshTokenAsync(request.Token, request.RefreshToken);
                if (!authResult.Success || authResult.User == null)
                {
                    return Unauthorized(new AuthResponse
                    {
                        Success = false,
                        Message = authResult.ErrorMessage ?? "토큰 갱신에 실패했습니다."
                    });
                }

                // 사용자 역할 조회
                var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(authResult.User.Id);
                var roles = userRoles.Select(ur => ur.RoleName).Where(r => !string.IsNullOrEmpty(r)).ToList();

                // 사용자 정보 매핑
                var userInfo = new UnivFI.WebUI.Helpers.UserInfo
                {
                    Id = authResult.User.Id,
                    UserName = authResult.User.Name, // 사용자 이름을 UserName으로 사용
                    Name = authResult.User.Name,
                    Email = authResult.User.Email,
                    Roles = roles
                };

                // 새 토큰 생성
                var token = _jwtHelper.GenerateJwtToken(userInfo);
                var refreshToken = _jwtHelper.GenerateRefreshToken();
                var tokenExpiry = _jwtHelper.CalculateTokenExpiry();
                var refreshTokenExpiry = _jwtHelper.CalculateRefreshTokenExpiry();

                // 리프레시 토큰 저장
                await _userService.SaveRefreshTokenAsync(authResult.User.Id, refreshToken, refreshTokenExpiry);

                // 응답 생성
                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "토큰 갱신 성공",
                    Token = token,
                    RefreshToken = refreshToken,
                    TokenExpiry = tokenExpiry,
                    RefreshTokenExpiry = refreshTokenExpiry,
                    UserInfo = userInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "토큰 갱신 중 오류 발생");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "서버 오류가 발생했습니다."
                });
            }
        }

        /// <summary>
        /// 로그아웃 (토큰 폐기)
        /// </summary>
        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // 사용자 ID 추출
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return BadRequest(new { Success = false, Message = "유효하지 않은 사용자입니다." });
                }

                // 리프레시 토큰 폐기
                await _userService.RevokeTokenAsync(userId);

                return Ok(new
                {
                    Success = true,
                    Message = "로그아웃 성공"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API 로그아웃 처리 중 오류 발생");
                return StatusCode(500, new { Success = false, Message = "서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 현재 인증된 사용자 정보 조회 및 토큰 검증
        /// </summary>
        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetCurrentUser()
        {
            try
            {
                // 현재 사용자 ID 가져오기
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogWarning("토큰에서 사용자 ID를 찾을 수 없습니다");
                    return Unauthorized(new { Success = false, Message = "유효하지 않은 토큰입니다" });
                }

                // JWT 클레임에서 사용자 정보 추출
                var userInfo = new UnivFI.WebUI.Helpers.UserInfo
                {
                    Id = userId,
                    UserName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unknown",
                    Name = User.FindFirst("name")?.Value ?? "Unknown",
                    Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Unknown",
                    Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
                        .Select(c => c.Value)
                        .Distinct()
                        .ToList()
                };

                return Ok(new
                {
                    Success = true,
                    Message = "토큰 검증 성공",
                    User = userInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 정보 조회 중 오류 발생");
                return StatusCode(500, new { Success = false, Message = "내부 서버 오류" });
            }
        }

        /// <summary>
        /// 토큰 유효성 검증 및 사용자 정보 반환 (클라이언트와 호환성을 위한 엔드포인트)
        /// </summary>
        [HttpGet("verify")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Verify()
        {
            try
            {
                // 현재 사용자 ID 가져오기
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogWarning("토큰에서 사용자 ID를 찾을 수 없습니다");
                    return Unauthorized(new { Success = false, Message = "유효하지 않은 토큰입니다" });
                }

                // JWT 클레임에서 사용자 정보 추출
                var userInfo = new UnivFI.WebUI.Helpers.UserInfo
                {
                    Id = userId,
                    UserName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unknown",
                    Name = User.FindFirst("name")?.Value ?? "Unknown",
                    Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Unknown",
                    Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
                        .Select(c => c.Value)
                        .Distinct()
                        .ToList()
                };

                // 클라이언트 호환성을 위한 응답 형식
                return Ok(new
                {
                    Success = true,
                    Message = "토큰이 유효합니다",
                    User = userInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "토큰 검증 중 오류 발생");
                return StatusCode(500, new { Success = false, Message = "내부 서버 오류" });
            }
        }

        /// <summary>
        /// 토큰 유효성 검증 (간단 검증용)
        /// </summary>
        [HttpGet("verify-token")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult VerifyToken()
        {
            return Ok(new { Success = true, Message = "토큰이 유효합니다" });
        }
    }
}