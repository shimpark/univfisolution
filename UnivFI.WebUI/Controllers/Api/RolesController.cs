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
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetRole(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            return Ok(role);
        }

        // POST: api/Roles
        [HttpPost]
        public async Task<ActionResult<RoleDto>> CreateRole(CreateRoleDto createRoleDto)
        {
            var id = await _roleService.CreateRoleAsync(createRoleDto);
            var createdRole = await _roleService.GetRoleByIdAsync(id);

            return CreatedAtAction(nameof(GetRole), new { id }, createdRole);
        }

        // PUT: api/Roles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, UpdateRoleDto updateRoleDto)
        {
            if (id != updateRoleDto.Id)
            {
                return BadRequest();
            }

            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var success = await _roleService.UpdateRoleAsync(updateRoleDto);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var success = await _roleService.DeleteRoleAsync(id);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // GET: api/Roles/ByUser/5
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRolesByUser(int userId)
        {
            var roles = await _roleService.GetRolesByUserIdAsync(userId);
            return Ok(roles);
        }

        // GET: api/Roles/ByMenu/5
        [HttpGet("ByMenu/{menuId}")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRolesByMenu(int menuId)
        {
            var roles = await _roleService.GetRolesByMenuIdAsync(menuId);
            return Ok(roles);
        }
    }
}