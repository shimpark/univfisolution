using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UnivFI.Domain.Interfaces.Repositories;
using UnivFI.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace UnivFI.WebUI.Controllers
{
    public class AgGridController : Controller
    {
        private readonly IMenuRepository _menuRepository;
        private readonly IMenuService _menuService;
        private readonly ILogger<AgGridController> _logger;

        public AgGridController(
            IMenuRepository menuRepository,
            IMenuService menuService,
            ILogger<AgGridController> logger)
        {
            _menuRepository = menuRepository;
            _menuService = menuService;
            _logger = logger;
        }

        public IActionResult MenuList()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuData(
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

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            try
            {
                var result = await _menuService.DeleteMenuAsync(id);
                if (result)
                {
                    return Ok(new { success = true, message = "메뉴가 성공적으로 삭제되었습니다." });
                }
                return NotFound(new { success = false, message = "메뉴를 찾을 수 없습니다." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메뉴 삭제 중 오류가 발생했습니다. MenuId: {MenuId}", id);
                return StatusCode(500, new { success = false, message = "메뉴 삭제 중 오류가 발생했습니다." });
            }
        }
    }
}