using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;

namespace UnivFI.WebUI.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserRolesController : ControllerBase
    {
        private readonly IUserRoleService _userRoleService;

        public UserRolesController(IUserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }

        // GET: api/UserRoles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetUserRoles()
        {
            var userRoles = await _userRoleService.GetAllUserRolesAsync();
            return Ok(userRoles);
        }

        // GET: api/UserRoles/ByUser/5
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetUserRolesByUser(int userId)
        {
            var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(userId);
            return Ok(userRoles);
        }

        // GET: api/UserRoles/ByRole/5
        [HttpGet("ByRole/{roleId}")]
        public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetUserRolesByRole(int roleId)
        {
            var userRoles = await _userRoleService.GetUserRolesByRoleIdAsync(roleId);
            return Ok(userRoles);
        }

        // POST: api/UserRoles
        [HttpPost]
        public async Task<IActionResult> AssignRoleToUser(AssignUserRoleDto assignUserRoleDto)
        {
            var success = await _userRoleService.AssignRoleToUserAsync(assignUserRoleDto.UserId, assignUserRoleDto.RoleId);

            if (!success)
            {
                return BadRequest("역할을 사용자에게 할당할 수 없습니다.");
            }

            return NoContent();
        }

        // DELETE: api/UserRoles/User/5/Role/1
        [HttpDelete("User/{userId}/Role/{roleId}")]
        public async Task<IActionResult> RemoveRoleFromUser(int userId, int roleId)
        {
            var success = await _userRoleService.RemoveRoleFromUserAsync(userId, roleId);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // GET: api/UserRoles/Check/User/5/Role/1
        [HttpGet("Check/User/{userId}/Role/{roleId}")]
        public async Task<ActionResult<bool>> CheckUserHasRole(int userId, int roleId)
        {
            var hasRole = await _userRoleService.UserHasRoleAsync(userId, roleId);
            return Ok(hasRole);
        }
    }
}