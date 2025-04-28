using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UnivFI.Domain.Interfaces.Repositories;
using System.Linq;
using System.Collections.Generic;
using UnivFI.Domain.Entities;
using UnivFI.WebUI.ViewModels;
using System;
using Microsoft.Extensions.Logging;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;

namespace UnivFI.WebUI.Controllers
{
    public class HandsontableController : Controller
    {
        private readonly IMenuRepository _menuRepository;
        private readonly IMenuService _menuService;
        private readonly ILogger<HandsontableController> _logger;

        public HandsontableController(IMenuRepository menuRepository, IMenuService menuService, ILogger<HandsontableController> logger)
        {
            _menuRepository = menuRepository;
            _menuService = menuService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Demo()
        {
            return View();
        }

        /// <summary>
        /// Grid.js를 사용한 메뉴 관리 데모 페이지를 표시합니다.
        /// </summary>
        public IActionResult GridDemo()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuData()
        {
            var menus = await _menuRepository.GetAllAsync();
            return Json(menus);
        }

        [HttpGet]
        public async Task<IActionResult> GetHierarchicalMenuData(
            int page = 1,
            int pageSize = 10,
            string searchTerm = "",
            string sortColumn = "",
            bool ascending = true)
        {
            try
            {
                var (items, totalCount) = await _menuService.GetHierarchicalMenuDataAsync(
                    page,
                    pageSize,
                    searchTerm,
                    sortColumn,
                    ascending
                );

                return Json(new
                {
                    items,
                    totalCount,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메뉴 데이터를 가져오는 중 오류가 발생했습니다.");
                return StatusCode(500, "서버 오류가 발생했습니다.");
            }

        }

        /// <summary>
        /// 새 메뉴 데이터를 저장합니다.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenus([FromBody] List<MenuCreateDto> menuCreates)
        {
            try
            {
                if (menuCreates == null || !menuCreates.Any())
                    return BadRequest(new { success = false, message = "생성할 메뉴 데이터가 없습니다." });

                var createdMenus = new List<object>();

                foreach (var menuCreate in menuCreates)
                {
                    var createDto = new CreateMenuDto
                    {
                        MenuKey = menuCreate.MenuKey,
                        Title = menuCreate.Title,
                        Url = menuCreate.Url,
                        ParentId = menuCreate.ParentId,
                        MenuOrder = menuCreate.MenuOrder,
                        Levels = menuCreate.Levels,
                        UseNewIcon = menuCreate.UseNewIcon
                    };

                    var newMenuId = await _menuService.CreateMenuAsync(createDto);

                    createdMenus.Add(new
                    {
                        id = newMenuId,
                        originalMenuKey = menuCreate.MenuKey
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"{menuCreates.Count}개 메뉴가 성공적으로 생성되었습니다.",
                    createdMenus = createdMenus
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "메뉴 생성 중 오류가 발생했습니다");
                return StatusCode(500, new { success = false, message = $"메뉴 생성 중 오류가 발생했습니다: {ex.Message}" });
            }
        }


        /// <summary>
        /// 변경된 메뉴 데이터를 저장합니다.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMenus([FromBody] List<MenuUpdateDto> menuUpdates)
        {
            if (menuUpdates == null || !menuUpdates.Any())
            {
                return BadRequest(new { success = false, message = "업데이트할 메뉴 데이터가 없습니다." });
            }

            foreach (var menuUpdate in menuUpdates)
            {
                var existingMenu = await _menuService.GetMenuByIdAsync(menuUpdate.Id);
                if (existingMenu == null)
                {
                    continue;
                }

                var updateDto = new UpdateMenuDto
                {
                    Id = menuUpdate.Id,
                    MenuKey = menuUpdate.MenuKey,
                    Title = menuUpdate.Title,
                    Url = menuUpdate.Url,
                    ParentId = menuUpdate.ParentId,
                    MenuOrder = menuUpdate.MenuOrder,
                    Levels = menuUpdate.Levels,
                    UseNewIcon = menuUpdate.UseNewIcon
                };

                await _menuService.UpdateMenuAsync(updateDto);
            }

            return Json(new { success = true, message = $"{menuUpdates.Count}개 메뉴가 성공적으로 업데이트되었습니다." });

        }

        /// <summary>
        /// 메뉴 데이터를 삭제합니다.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenus([FromBody] List<int> menuIds)
        {
            if (menuIds == null || !menuIds.Any())
            {
                return BadRequest(new { success = false, message = "삭제할 메뉴 ID가 없습니다." });
            }

            var deletedCount = 0;
            var errors = new List<string>();

            foreach (var menuId in menuIds)
            {
                try
                {
                    var result = await _menuService.DeleteMenuAsync(menuId);
                    if (result)
                    {
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"메뉴 ID {menuId} 삭제 실패: {ex.Message}");
                }
            }

            if (errors.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"{deletedCount}개 메뉴 삭제 성공, {errors.Count}개 실패",
                    errors = errors
                });
            }

            return Json(new
            {
                success = true,
                message = $"{deletedCount}개 메뉴가 성공적으로 삭제되었습니다."
            });

        }

        private List<object> BuildHierarchicalMenuData(IEnumerable<MenuDto> menus)
        {
            var result = new List<object>();
            var rootMenus = menus.Where(m => m.ParentId == null || m.ParentId == 0).OrderBy(m => m.MenuOrder);

            foreach (var menu in rootMenus)
            {
                var hasChildren = menus.Any(m => m.ParentId == menu.Id);
                result.Add(new
                {
                    id = menu.Id,
                    menuKey = menu.MenuKey,
                    title = menu.Title,
                    url = menu.Url,
                    parentId = menu.ParentId,
                    menuOrder = menu.MenuOrder ?? 0,
                    levels = menu.Levels ?? 0,
                    indent = 0,
                    hasChildren = hasChildren,
                    useNewIcon = menu.UseNewIcon
                });

                if (hasChildren)
                {
                    AddChildMenus(menus, menu.Id, 1, result);
                }
            }

            return result;

        }

        private void AddChildMenus(IEnumerable<MenuDto> allMenus, int parentId, int indentLevel, List<object> result)
        {
            var childMenus = allMenus.Where(m => m.ParentId == parentId).OrderBy(m => m.MenuOrder);

            foreach (var childMenu in childMenus)
            {
                var hasChildren = allMenus.Any(m => m.ParentId == childMenu.Id);
                result.Add(new
                {
                    id = childMenu.Id,
                    menuKey = childMenu.MenuKey,
                    title = childMenu.Title,
                    url = childMenu.Url,
                    parentId = childMenu.ParentId,
                    menuOrder = childMenu.MenuOrder ?? 0,
                    levels = childMenu.Levels ?? 0,
                    indent = indentLevel,
                    hasChildren = hasChildren,
                    useNewIcon = childMenu.UseNewIcon
                });

                if (hasChildren)
                {
                    AddChildMenus(allMenus, childMenu.Id, indentLevel + 1, result);
                }
            }
        }
    }

    // 메뉴 생성을 위한 DTO 클래스
    public class MenuCreateDto
    {
        public string MenuKey { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public int? ParentId { get; set; }
        public int? MenuOrder { get; set; }
        public int? Levels { get; set; }
        public bool? UseNewIcon { get; set; }
    }

    // 메뉴 업데이트를 위한 DTO 클래스
    public class MenuUpdateDto
    {
        public int Id { get; set; }
        public string MenuKey { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public int? ParentId { get; set; }
        public int? MenuOrder { get; set; }
        public int? Levels { get; set; }
        public bool? UseNewIcon { get; set; }
    }
}