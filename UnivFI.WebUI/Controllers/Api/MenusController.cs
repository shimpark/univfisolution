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
    public class MenusController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenusController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        // GET: api/Menus
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuDto>>> GetMenus([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var menus = await _menuService.GetAllMenusAsync();
            return Ok(menus);
        }

        // GET: api/Menus/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MenuDto>> GetMenu(int id)
        {
            var menu = await _menuService.GetMenuByIdAsync(id);

            if (menu == null)
            {
                return NotFound();
            }

            return Ok(menu);
        }

        // POST: api/Menus
        [HttpPost]
        public async Task<ActionResult<MenuDto>> CreateMenu(CreateMenuDto createMenuDto)
        {
            var id = await _menuService.CreateMenuAsync(createMenuDto);
            var createdMenu = await _menuService.GetMenuByIdAsync(id);

            return CreatedAtAction(nameof(GetMenu), new { id }, createdMenu);
        }

        // PUT: api/Menus/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMenu(int id, UpdateMenuDto updateMenuDto)
        {
            if (id != updateMenuDto.Id)
            {
                return BadRequest();
            }

            var menu = await _menuService.GetMenuByIdAsync(id);
            if (menu == null)
            {
                return NotFound();
            }

            var success = await _menuService.UpdateMenuAsync(updateMenuDto);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Menus/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var menu = await _menuService.GetMenuByIdAsync(id);
            if (menu == null)
            {
                return NotFound();
            }

            var success = await _menuService.DeleteMenuAsync(id);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // GET: api/Menus/ByRole/5
        [HttpGet("ByRole/{roleId}")]
        public async Task<ActionResult<IEnumerable<MenuDto>>> GetMenusByRole(int roleId)
        {
            var menus = await _menuService.GetMenusByRoleIdAsync(roleId);
            return Ok(menus);
        }
    }
}