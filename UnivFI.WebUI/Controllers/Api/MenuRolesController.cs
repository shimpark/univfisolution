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
    public class MenuRolesController : ControllerBase
    {
        private readonly IMenuRoleService _menuRoleService;

        public MenuRolesController(IMenuRoleService menuRoleService)
        {
            _menuRoleService = menuRoleService;
        }

        // GET: api/MenuRoles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuRoleDto>>> GetMenuRoles()
        {
            var menuRoles = await _menuRoleService.GetAllMenuRolesAsync();
            return Ok(menuRoles);
        }

        // GET: api/MenuRoles/ByMenu/5
        [HttpGet("ByMenu/{menuId}")]
        public async Task<ActionResult<IEnumerable<MenuRoleDto>>> GetMenuRolesByMenu(int menuId)
        {
            var menuRoles = await _menuRoleService.GetMenuRolesByMenuIdAsync(menuId);
            return Ok(menuRoles);
        }

        // GET: api/MenuRoles/ByRole/5
        [HttpGet("ByRole/{roleId}")]
        public async Task<ActionResult<IEnumerable<MenuRoleDto>>> GetMenuRolesByRole(int roleId)
        {
            var menuRoles = await _menuRoleService.GetMenuRolesByRoleIdAsync(roleId);
            return Ok(menuRoles);
        }

        // POST: api/MenuRoles
        [HttpPost]
        public async Task<IActionResult> AssignRoleToMenu(AssignMenuRoleDto assignMenuRoleDto)
        {
            var success = await _menuRoleService.AssignRoleToMenuAsync(assignMenuRoleDto);

            if (!success)
            {
                return BadRequest("역할을 메뉴에 할당할 수 없습니다.");
            }

            return NoContent();
        }

        // DELETE: api/MenuRoles/Menu/5/Role/1
        [HttpDelete("Menu/{menuId}/Role/{roleId}")]
        public async Task<IActionResult> RemoveRoleFromMenu(int menuId, int roleId)
        {
            var success = await _menuRoleService.RemoveRoleFromMenuAsync(menuId, roleId);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // GET: api/MenuRoles/Check/Menu/5/Role/1
        [HttpGet("Check/Menu/{menuId}/Role/{roleId}")]
        public async Task<ActionResult<bool>> CheckMenuHasRole(int menuId, int roleId)
        {
            var hasRole = await _menuRoleService.MenuHasRoleAsync(menuId, roleId);
            return Ok(hasRole);
        }
    }
}