using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;
using UnivFI.WebUI.ViewModels;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using UnivFI.WebUI.Helpers;
using UnivFI.WebUI.Areas.Admin.ViewModels.Menu;
using Mapster;
using MapsterMapper;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.WebUI.Areas.Admin.ViewModels;
using Microsoft.Extensions.Logging;
using UnivFI.WebUI.Controllers;

namespace UnivFI.WebUI.Areas.Admin.Controllers
{
    [Area(AdminAreaConfiguration.AreaName)]
    [Authorize(Roles = "Administrators")]
    public class MenuController : BaseController
    {
        private readonly IMenuRepository _menuRepository;
        private readonly IMenuRoleRepository _menuRoleRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ICompositeViewEngine _viewEngine;
        private const int DefaultPageSize = 10;

        public MenuController(
            IMenuRepository menuRepository,
            IMenuRoleRepository menuRoleRepository,
            IRoleRepository roleRepository,
            IMapper mapper,
            ITempDataDictionaryFactory tempDataFactory,
            ICompositeViewEngine viewEngine,
            ILogger<MenuController> logger)
            : base(mapper, tempDataFactory, viewEngine, logger)
        {
            _menuRepository = menuRepository;
            _menuRoleRepository = menuRoleRepository;
            _roleRepository = roleRepository;
            _viewEngine = viewEngine;
        }

        /// <summary>
        /// ViewBag에 페이지네이션 관련 값들을 설정합니다.
        /// </summary>
        /// <param name="paginationInfo">페이지네이션 정보</param>
        private void SetPaginationViewBag(PaginationRequestViewModel paginationInfo)
        {
            ViewBag.ReturnPage = paginationInfo.ReturnPage;
            ViewBag.ReturnPageSize = paginationInfo.ReturnPageSize;
            ViewBag.ReturnSearchTerm = paginationInfo.ReturnSearchTerm;
            ViewBag.ReturnSearchFields = paginationInfo.ReturnSearchFields;
        }

        // GET: Admin/Menu
        public async Task<IActionResult> Index(int page = 1, int? pageSize = null, string searchTerm = "", string searchFields = "")
        {
            try
            {
                // 페이지 번호 유효성 검사
                if (page < 1) page = 1;

                // 페이지 크기 처리 - URL에서 전달된 값이 있으면 사용하고, 없으면 기본값 사용
                int actualPageSize = pageSize ?? DefaultPageSize;

                // 최소/최대 페이지 크기 제한
                if (actualPageSize < 5) actualPageSize = 5;
                if (actualPageSize > 100) actualPageSize = 100;

                // 검색 필드 기본값 설정
                searchFields = string.IsNullOrEmpty(searchFields) ? "MenuKey,Title,Url" : searchFields;

                var result = await _menuRepository.GetPagedAsync(page, actualPageSize, searchTerm, searchFields);

                // 내림차순 정렬 적용
                var orderedMenus = result.Items.OrderByDescending(m => m.Id);

                var model = new MenuListViewModel
                {
                    Menus = Mapper.Map<IEnumerable<MenuViewModel>>(orderedMenus),
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(result.TotalCount / (double)actualPageSize),
                    TotalItems = result.TotalCount,
                    SearchTerm = searchTerm ?? string.Empty,
                    SearchFields = searchFields,
                    PageSize = actualPageSize
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 목록 조회 중 오류 발생");
                TempData["ErrorMessage"] = "메뉴 목록을 불러오는 중 오류가 발생했습니다.";
                return View(new MenuListViewModel());
            }
        }

        // GET: Admin/Menu/Detail/5
        public async Task<IActionResult> Detail(int? id, [FromQuery] PaginationRequestViewModel paginationInfo)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var menu = await _menuRepository.GetByIdAsync(id.Value);
                if (menu == null)
                {
                    return NotFound();
                }

                // 메뉴에 할당된 역할 가져오기
                var menuRoles = await _menuRoleRepository.GetByMenuIdAsync(id.Value);
                var assignedRoleIds = menuRoles.Select(mr => mr.RoleId).ToList();

                // 모든 역할 가져오기
                var allRoles = await _roleRepository.GetAllAsync();

                ViewBag.AssignedRoles = allRoles.Where(r => assignedRoleIds.Contains(r.Id)).ToList();
                SetPaginationViewBag(paginationInfo);

                var viewModel = Mapper.Map<MenuViewModel>(menu);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 상세 정보 조회 중 오류 발생: {Id}", id);
                TempData["ErrorMessage"] = "메뉴 상세 정보를 불러오는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Menu/Create
        public async Task<IActionResult> Create([FromQuery] PaginationRequestViewModel paginationInfo)
        {
            try
            {
                // 생성 폼에 사용할 역할 목록 준비
                await PrepareRolesForView();

                // ParentId 선택을 위한 메뉴 목록 준비
                await PrepareParentMenusForView();

                SetPaginationViewBag(paginationInfo);

                return View(new MenuViewModel
                {
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    MenuOrder = 0,
                    Levels = 0
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 생성 폼 준비 중 오류 발생");
                TempData["ErrorMessage"] = "메뉴 생성 폼을 준비하는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Menu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuViewModel viewModel, int[] selectedRoles, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // 생성 시간 설정
                    viewModel.CreatedAt = DateTime.Now;
                    viewModel.UpdatedAt = DateTime.Now;

                    // URL이 null인 경우 빈 문자열로 설정
                    viewModel.Url = viewModel.Url ?? string.Empty;

                    // ViewModel -> Entity 변환
                    var menuEntity = Mapper.Map<MenuEntity>(viewModel);
                    var menuId = await _menuRepository.CreateAsync(menuEntity);

                    // 선택된 역할을 메뉴에 할당
                    if (selectedRoles != null && selectedRoles.Any())
                    {
                        foreach (var roleId in selectedRoles)
                        {
                            await _menuRoleRepository.AssignRoleToMenuAsync(menuId, roleId);
                        }
                    }

                    TempData["SuccessMessage"] = "메뉴가 성공적으로 생성되었습니다.";
                    return RedirectToAction(nameof(Index), paginationInfo.ToRouteValues());
                }

                // 오류 발생 시 역할 목록 다시 준비
                await PrepareRolesForView();
                await PrepareParentMenusForView();
                SetPaginationViewBag(paginationInfo);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 생성 중 오류 발생");
                ModelState.AddModelError("", "메뉴를 생성하는 중 오류가 발생했습니다.");
                await PrepareRolesForView();
                await PrepareParentMenusForView();
                SetPaginationViewBag(paginationInfo);
                return View(viewModel);
            }
        }

        // GET: Admin/Menu/Edit/5
        public async Task<IActionResult> Edit(int? id, [FromQuery] PaginationRequestViewModel paginationInfo)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var menu = await _menuRepository.GetByIdAsync(id.Value);
                if (menu == null)
                {
                    return NotFound();
                }

                // 메뉴에 할당된 역할 가져오기
                var menuRoles = await _menuRoleRepository.GetByMenuIdAsync(id.Value);
                var assignedRoleIds = menuRoles.Select(mr => mr.RoleId).ToList();

                // 모든 역할 가져오기
                var allRoles = await _roleRepository.GetAllAsync();

                ViewBag.Roles = allRoles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.RoleName,
                    Selected = assignedRoleIds.Contains(r.Id)
                }).ToList();

                // ParentId 선택을 위한 메뉴 목록 준비 (자기 자신은 제외)
                await PrepareParentMenusForView(id.Value);

                SetPaginationViewBag(paginationInfo);

                // Entity -> ViewModel 변환 및 역할 할당
                var viewModel = Mapper.Map<MenuViewModel>(menu);
                viewModel.SelectedRoles = assignedRoleIds.ToArray();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 수정 폼 준비 중 오류 발생: {Id}", id);
                TempData["ErrorMessage"] = "메뉴 수정 폼을 준비하는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Menu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuViewModel viewModel, int[] selectedRoles, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // 원래 엔티티를 가져와서 CreatedAt 값을 유지
                    var originalMenu = await _menuRepository.GetByIdAsync(id);
                    viewModel.CreatedAt = originalMenu.CreatedAt;
                    viewModel.UpdatedAt = DateTime.Now;

                    // URL이 null인 경우 빈 문자열로 설정
                    viewModel.Url = viewModel.Url ?? string.Empty;

                    // ViewModel -> Entity 변환
                    var menuEntity = Mapper.Map<MenuEntity>(viewModel);
                    await _menuRepository.UpdateAsync(menuEntity);

                    // 기존 역할 연결 모두 제거
                    await _menuRoleRepository.RemoveAllRolesFromMenuAsync(viewModel.Id);

                    // 선택된 역할 다시 할당
                    if (selectedRoles != null && selectedRoles.Any())
                    {
                        foreach (var roleId in selectedRoles)
                        {
                            await _menuRoleRepository.AssignRoleToMenuAsync(viewModel.Id, roleId);
                        }
                    }

                    TempData["SuccessMessage"] = "메뉴가 성공적으로 수정되었습니다.";
                    return RedirectToAction(nameof(Index), paginationInfo.ToRouteValues());
                }

                // 오류 발생 시 역할 목록 다시 준비
                await PrepareRolesForView(selectedRoles);
                await PrepareParentMenusForView(id);
                SetPaginationViewBag(paginationInfo);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 수정 중 오류 발생: {Id}", id);
                ModelState.AddModelError("", "메뉴를 수정하는 중 오류가 발생했습니다.");
                await PrepareRolesForView(selectedRoles);
                await PrepareParentMenusForView(id);
                SetPaginationViewBag(paginationInfo);
                return View(viewModel);
            }
        }

        // GET: Admin/Menu/Delete/5
        public async Task<IActionResult> Delete(int? id, [FromQuery] PaginationRequestViewModel paginationInfo)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var menu = await _menuRepository.GetByIdAsync(id.Value);
                if (menu == null)
                {
                    return NotFound();
                }

                // 메뉴에 할당된 역할 가져오기
                var menuRoles = await _menuRoleRepository.GetByMenuIdAsync(id.Value);
                var assignedRoleIds = menuRoles.Select(mr => mr.RoleId).ToList();

                // 모든 역할 가져오기
                var allRoles = await _roleRepository.GetAllAsync();

                ViewBag.AssignedRoles = allRoles.Where(r => assignedRoleIds.Contains(r.Id)).ToList();
                SetPaginationViewBag(paginationInfo);

                var viewModel = Mapper.Map<MenuViewModel>(menu);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 삭제 페이지 준비 중 오류 발생: {Id}", id);
                TempData["ErrorMessage"] = "메뉴 삭제 페이지를 준비하는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Menu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            try
            {
                await _menuRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "메뉴가 성공적으로 삭제되었습니다.";
                return RedirectToAction(nameof(Index), paginationInfo.ToRouteValues());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 삭제 중 오류 발생: {Id}", id);
                TempData["ErrorMessage"] = "메뉴를 삭제하는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Menu/Tree
        public async Task<IActionResult> Tree()
        {
            try
            {
                var menus = await _menuRepository.GetAllForTreeAsync();
                var menuViewModels = menus.Select(m => Mapper.Map<MenuTreeViewModel>(m)).ToList();

                // 메뉴 트리 구성
                var menuTree = BuildMenuTree(menuViewModels);

                return View(menuTree);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 트리 조회 중 오류 발생");
                TempData["ErrorMessage"] = "메뉴 트리를 불러오는 중 오류가 발생했습니다.";
                return View(new List<MenuTreeViewModel>());
            }
        }

        // 메뉴 트리 구성 메서드
        private List<MenuTreeViewModel> BuildMenuTree(List<MenuTreeViewModel> menuItems)
        {
            var rootItems = menuItems.Where(m => m.ParentId == null || m.ParentId == 0).ToList();
            var childItems = menuItems.Where(m => m.ParentId != null && m.ParentId > 0).ToList();

            foreach (var rootItem in rootItems)
            {
                rootItem.Children = BuildChildTree(rootItem, childItems);
            }

            return rootItems;
        }

        private List<MenuTreeViewModel> BuildChildTree(MenuTreeViewModel parent, List<MenuTreeViewModel> allChildren)
        {
            var children = allChildren.Where(c => c.ParentId == parent.Id).ToList();
            foreach (var child in children)
            {
                child.Children = BuildChildTree(child, allChildren);
            }
            return children;
        }

        private async Task<bool> MenuExists(int id)
        {
            var menu = await _menuRepository.GetByIdAsync(id);
            return menu != null;
        }

        private async Task PrepareRolesForView(int[]? selectedRoles = null)
        {
            var roles = await _roleRepository.GetAllAsync();
            ViewBag.Roles = roles.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.RoleName,
                Selected = selectedRoles != null && selectedRoles.Contains(r.Id)
            }).ToList();
        }

        private async Task PrepareParentMenusForView(int? currentMenuId = null)
        {
            var menus = await _menuRepository.GetAllForTreeAsync();

            // 현재 메뉴가 있는 경우 현재 메뉴 자신은 제외
            if (currentMenuId.HasValue)
            {
                menus = menus.Where(m => m.Id != currentMenuId.Value).ToList();
            }

            // ParentId를 위한 선택 목록 생성
            var menuItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- 최상위 메뉴 --" }
            };

            menuItems.AddRange(menus.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.MenuKey} - {m.Title}"
            }));

            ViewBag.ParentMenus = menuItems;
        }

        // 메뉴 트리 업데이트 모델
        public class MenuTreeUpdateModel
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public short MenuOrder { get; set; }
            public short? Levels { get; set; }
        }

        // POST: Admin/Menu/UpdateMenuTree
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMenuTree([FromBody] List<MenuTreeUpdateModel> menuUpdates)
        {
            if (menuUpdates == null || !menuUpdates.Any())
            {
                return BadRequest("업데이트할 메뉴 데이터가 없습니다.");
            }

            try
            {
                foreach (var update in menuUpdates)
                {
                    var menu = await _menuRepository.GetByIdAsync(update.Id);
                    if (menu != null)
                    {
                        menu.ParentId = update.ParentId;
                        menu.MenuOrder = update.MenuOrder;
                        menu.Levels = update.Levels;
                        menu.UpdatedAt = DateTime.Now;

                        await _menuRepository.UpdateAsync(menu);
                    }
                }

                return Ok(new { success = true, message = "메뉴 구조가 성공적으로 업데이트되었습니다." });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 트리 업데이트 중 오류 발생");
                return StatusCode(500, new { success = false, message = $"메뉴 업데이트 중 오류가 발생했습니다: {ex.Message}" });
            }
        }

        /// <summary>
        /// 페이징 컴포넌트를 동적으로 렌더링하기 위한 메서드
        /// </summary>
        /// <param name="currentPage">현재 페이지 번호</param>
        /// <param name="totalPages">전체 페이지 수</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <param name="totalItems">전체 항목 수</param>
        /// <param name="searchTerm">검색어</param>
        /// <param name="searchFields">검색 필드</param>
        /// <returns>페이징 컴포넌트 HTML</returns>
        [HttpGet]
        public IActionResult RenderPagination(int currentPage, int totalPages, int pageSize, int totalItems, string? searchTerm = null, string? searchFields = null)
        {
            var pagingModel = new UnivFI.WebUI.Areas.Admin.ViewModels.PaginationViewModel
            {
                CurrentPage = currentPage,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems,
                ActionName = "Index",
                ControllerName = "Menu",
                AreaName = AdminAreaConfiguration.AreaName
            };

            // 페이지 크기를 RouteData에 추가
            pagingModel.RouteData.Add("pageSize", pageSize.ToString());

            if (!string.IsNullOrEmpty(searchTerm))
            {
                pagingModel.RouteData.Add("searchTerm", searchTerm);
            }

            if (!string.IsNullOrEmpty(searchFields))
            {
                pagingModel.RouteData.Add("searchFields", searchFields);
            }

            try
            {
                string paginationHtml = ViewRenderHelper.RenderViewToString(this, _viewEngine, "Views/Shared/_Pagination.cshtml", pagingModel);

                return Json(new { paginationHtml });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "페이징 컴포넌트 렌더링 오류");
                return Json(new { error = true, message = $"페이징 컴포넌트를 렌더링하는 중 오류가 발생했습니다: {ex.Message}" });
            }
        }
    }
}