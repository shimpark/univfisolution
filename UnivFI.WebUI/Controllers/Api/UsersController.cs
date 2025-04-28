using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.WebUI.Models.API;
using System;

namespace UnivFI.WebUI.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            IUserRoleService userRoleService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _logger = logger;
        }

        /// <summary>
        /// 현재 로그인한 사용자 정보 조회
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyInfo()
        {
            try
            {
                // 다양한 방법으로 사용자 ID 찾기
                int userId;

                // 방법 1: 표준 ClaimTypes.NameIdentifier 사용
                var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // 방법 2: 직접 "sub" 클레임 찾기
                if (string.IsNullOrEmpty(subClaim))
                {
                    subClaim = User.FindFirst("sub")?.Value;
                }

                // 방법 3: User.Identity.Name에서 추출 시도
                if (string.IsNullOrEmpty(subClaim) && User.Identity?.Name != null)
                {
                    subClaim = User.Identity.Name;
                }

                // 로깅 추가
                _logger.LogInformation($"추출된 사용자 ID 클레임: {subClaim}");
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation($"클레임 - 타입: {claim.Type}, 값: {claim.Value}");
                }

                if (!int.TryParse(subClaim, out userId))
                {
                    return BadRequest(new { Success = false, Message = "유효하지 않은 사용자입니다. 클레임 확인 필요." });
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { Success = false, Message = $"ID가 {userId}인 사용자를 찾을 수 없습니다." });
                }

                // 역할 정보 추가
                var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(userId);
                var roles = userRoles.Select(ur => ur.RoleName).Where(r => !string.IsNullOrEmpty(r)).ToList();

                var userDto = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    Roles = roles
                };

                return Ok(new { Success = true, User = userDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 정보 조회 중 오류 발생");
                return StatusCode(500, new { Success = false, Message = "서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 모든 사용자 목록 조회 (관리자 전용)
        /// </summary>
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrators,Admin")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] string? searchTerm = null, [FromQuery] string? searchFields = null)
        {
            try
            {
                // 기본 검색 필드 설정
                if (string.IsNullOrEmpty(searchFields))
                {
                    searchFields = "Name,Email";
                }

                var users = await _userService.GetUsersAsync(page, 10, searchTerm, searchFields);
                var totalCount = await _userService.GetTotalCountAsync(searchTerm, searchFields);
                var totalPages = (int)Math.Ceiling(totalCount / 10.0);

                return Ok(new
                {
                    Success = true,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = 10,
                    Users = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 목록 조회 중 오류 발생");
                return StatusCode(500, new { Success = false, Message = "서버 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 특정 사용자 상세 정보 조회
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrators,Admin")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { Success = false, Message = "사용자를 찾을 수 없습니다." });
                }

                // 역할 정보 함께 반환
                var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(id);
                var roles = userRoles.Select(ur => ur.RoleName).Where(r => !string.IsNullOrEmpty(r)).ToList();

                var userDto = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    Roles = roles
                };

                return Ok(new { Success = true, User = userDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 정보 조회 중 오류 발생");
                return StatusCode(500, new { Success = false, Message = "서버 오류가 발생했습니다." });
            }
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = await _userService.CreateUserAsync(createUserDto);
            var user = await _userService.GetUserByIdAsync(userId);

            return CreatedAtAction(nameof(GetUserById), new { id = userId }, user);
        }

        /// <summary>
        /// 사용자 생성 및 다중 역할 할당
        /// </summary>
        [HttpPost("with-roles")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrators")]
        public async Task<IActionResult> CreateUserWithRoles([FromBody] CreateUserWithRolesRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Success = false, Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // 1. 사용자 생성
                var createUserDto = request.ToCreateUserDto();
                var userId = await _userService.CreateUserAsync(createUserDto);

                // 2. 역할 할당
                var assignmentResults = new List<object>();
                bool allSuccess = true;

                foreach (var roleId in request.RoleIds)
                {
                    try
                    {
                        var success = await _userRoleService.AssignRoleToUserAsync(userId, roleId);
                        assignmentResults.Add(new { RoleId = roleId, Success = success });

                        if (!success)
                        {
                            allSuccess = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"역할 할당 중 오류 발생 - 사용자 ID: {userId}, 역할 ID: {roleId}");
                        assignmentResults.Add(new { RoleId = roleId, Success = false, Error = ex.Message });
                        allSuccess = false;
                    }
                }

                // 3. 생성된 사용자 정보 조회 (역할 포함)
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return StatusCode(500, new { Success = false, Message = "사용자가 생성되었으나 조회할 수 없습니다." });
                }

                // 4. 역할 정보 추가
                var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(userId);
                var roles = userRoles.Select(ur => ur.RoleName).Where(r => !string.IsNullOrEmpty(r)).ToList();

                var userDto = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    Roles = roles
                };

                return Ok(new
                {
                    Success = true,
                    Message = allSuccess ? "사용자 생성 및 모든 역할 할당 성공" : "사용자 생성 성공, 일부 역할 할당 실패",
                    User = userDto,
                    RoleAssignments = assignmentResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 생성 및 역할 할당 중 오류 발생");
                return StatusCode(500, new { Success = false, Message = "서버 오류가 발생했습니다.", Error = ex.Message });
            }
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrators,Admin")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto updateUserDto)
        {
            if (id != updateUserDto.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _userService.UpdateUserAsync(updateUserDto);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrators,Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                // 먼저 사용자의 모든 역할을 가져옵니다
                var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(id);

                // 각 역할을 삭제합니다
                foreach (var userRole in userRoles)
                {
                    await _userRoleService.RemoveRoleFromUserAsync(id, userRole.RoleId);
                }

                // 그런 다음 사용자를 삭제합니다
                var success = await _userService.DeleteUserAsync(id);

                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                return StatusCode(500, "Internal server error while trying to delete user");
            }
        }
    }
}
