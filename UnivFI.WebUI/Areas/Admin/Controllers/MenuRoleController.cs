using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.WebUI.Areas.Admin.ViewModels.MenuRole;
using UnivFI.WebUI.Extensions;
using UnivFI.WebUI.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using UnivFI.WebUI.Areas.Admin.ViewModels;
using UnivFI.WebUI.Helpers;

namespace UnivFI.WebUI.Areas.Admin.Controllers
{
    [Area(AdminAreaConfiguration.AreaName)]
    [Authorize(Roles = "Administrators")]
    public class MenuRoleController : BaseController
    {
        private readonly IMenuRoleService _menuRoleService;
        private readonly IMenuService _menuService;
        private readonly IRoleService _roleService;
        private const int DefaultPageSize = 10;

        public MenuRoleController(
            IMenuRoleService menuRoleService,
            IMenuService menuService,
            IRoleService roleService,
            IMapper mapper,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            ILogger<MenuRoleController>? logger = null)
            : base(mapper, tempDataFactory, viewEngine, logger)
        {
            _menuRoleService = menuRoleService;
            _menuService = menuService;
            _roleService = roleService;
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

        // GET: Admin/MenuRole
        public async Task<IActionResult> Index(int page = 1, int pageSize = DefaultPageSize, string searchTerm = "", string searchFields = "")
        {
            // 페이지 번호 유효성 검사
            page = Math.Max(1, page);
            pageSize = Math.Max(5, Math.Min(50, pageSize));

            // 검색 필드 기본값 설정
            searchFields = string.IsNullOrEmpty(searchFields) ? "MenuKey,MenuTitle,RoleName" : searchFields;

            // 메뉴 역할 목록과 총 개수 가져오기
            var menuRoles = await _menuRoleService.GetMenuRolesAsync(page, pageSize, searchTerm, searchFields);
            var totalCount = await _menuRoleService.GetTotalCountAsync(searchTerm, searchFields);

            // 페이지네이션 정보 계산
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // ViewModel로 변환
            var menuRoleViewModels = Mapper.Map<IEnumerable<MenuRoleViewModel>>(menuRoles);

            // 목록 ViewModel 생성 및 설정
            var model = new MenuRoleListViewModel
            {
                MenuRoles = menuRoleViewModels,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalCount,
                SearchTerm = searchTerm,
                SearchFields = searchFields
            };

            // 페이징 컴포넌트 뷰모델 생성
            var pagingModel = new ViewModels.PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalCount,
                ActionName = "Index",
                ControllerName = "MenuRole",
                AreaName = AdminAreaConfiguration.AreaName,
                RouteData = new Dictionary<string, string>
                {
                    { "searchTerm", searchTerm ?? "" },
                    { "searchFields", searchFields ?? "" },
                    { "pageSize", pageSize.ToString() }
                }
            };

            // 페이징 뷰모델을 ViewBag에 추가
            ViewBag.PagingModel = pagingModel;

            return View(model);
        }

        // GET: Admin/MenuRole/ByMenu/5 or MenuRole/ByMenu?menuId=5
        public async Task<IActionResult> ByMenu(int menuId, int page = 1, int pageSize = DefaultPageSize, [FromQuery] PaginationRequestViewModel paginationInfo = null)
        {
            // 페이징 파라미터 유효성 검사
            page = Math.Max(1, page);
            pageSize = Math.Max(5, Math.Min(50, pageSize));

            // 페이지네이션 정보가 null이면 새로 생성
            paginationInfo ??= new PaginationRequestViewModel
            {
                ReturnPage = page,
                ReturnPageSize = pageSize
            };

            // ViewBag에 페이지네이션 정보 설정
            SetPaginationViewBag(paginationInfo);

            // 메뉴 아이디별 메뉴 역할 목록 가져오기
            var menuRoles = await _menuRoleService.GetMenuRolesByMenuIdAsync(menuId, page, pageSize);
            var totalCount = await _menuRoleService.GetTotalCountByMenuIdAsync(menuId);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // ViewModel로 변환
            var viewModels = Mapper.Map<IEnumerable<MenuRoleViewModel>>(menuRoles);

            // 메뉴 정보 가져오기
            var menu = await _menuService.GetMenuByIdAsync(menuId);
            ViewBag.MenuTitle = menu?.Title ?? "알 수 없는 메뉴";
            ViewBag.MenuId = menuId;

            // 페이징 컴포넌트 뷰모델 생성
            var pagingModel = new ViewModels.PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalCount,
                ActionName = "ByMenu",
                ControllerName = "MenuRole",
                AreaName = AdminAreaConfiguration.AreaName,
                RouteData = new Dictionary<string, string>
                {
                    { "menuId", menuId.ToString() },
                    { "pageSize", pageSize.ToString() }
                }
            };

            // 페이징 뷰모델을 ViewBag에 추가
            ViewBag.PagingModel = pagingModel;

            return View(viewModels);
        }

        // GET: Admin/MenuRole/ByRole/5 or MenuRole/ByRole?roleId=5
        public async Task<IActionResult> ByRole(int roleId, int page = 1, int pageSize = DefaultPageSize, [FromQuery] PaginationRequestViewModel paginationInfo = null)
        {
            // 페이징 파라미터 유효성 검사
            page = Math.Max(1, page);
            pageSize = Math.Max(5, Math.Min(50, pageSize));

            // 페이지네이션 정보가 null이면 새로 생성
            paginationInfo ??= new PaginationRequestViewModel
            {
                ReturnPage = page,
                ReturnPageSize = pageSize
            };

            // ViewBag에 페이지네이션 정보 설정
            SetPaginationViewBag(paginationInfo);

            // 역할 아이디별 메뉴 역할 목록 가져오기
            var menuRoles = await _menuRoleService.GetMenuRolesByRoleIdAsync(roleId, page, pageSize);
            var totalCount = await _menuRoleService.GetTotalCountByRoleIdAsync(roleId);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // ViewModel로 변환
            var viewModels = Mapper.Map<IEnumerable<MenuRoleViewModel>>(menuRoles);

            // 역할 정보 가져오기
            var role = await _roleService.GetRoleByIdAsync(roleId);
            ViewBag.RoleName = role?.RoleName ?? "알 수 없는 역할";
            ViewBag.RoleId = roleId;

            // 페이징 컴포넌트 뷰모델 생성
            var pagingModel = new ViewModels.PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalCount,
                ActionName = "ByRole",
                ControllerName = "MenuRole",
                AreaName = AdminAreaConfiguration.AreaName,
                RouteData = new Dictionary<string, string>
                {
                    { "roleId", roleId.ToString() },
                    { "pageSize", pageSize.ToString() }
                }
            };

            // 페이징 뷰모델을 ViewBag에 추가
            ViewBag.PagingModel = pagingModel;

            return View(viewModels);
        }

        // GET: Admin/MenuRole/Assign
        public async Task<IActionResult> Assign(int? preSelectedMenuId = null, int? preSelectedRoleId = null, [FromQuery] PaginationRequestViewModel paginationInfo = null)
        {
            // 페이지네이션 정보가 null이면 새로 생성
            paginationInfo ??= new PaginationRequestViewModel();

            // ViewBag에 페이지네이션 정보 설정
            SetPaginationViewBag(paginationInfo);

            var menus = await _menuService.GetAllMenusAsync();
            var roles = await _roleService.GetAllRolesAsync();

            var model = new MenuRoleAssignViewModel
            {
                Menus = menus.Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = $"{m.MenuKey} - {m.Title}",
                    Selected = preSelectedMenuId.HasValue && m.Id == preSelectedMenuId
                }),

                Roles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.RoleName,
                    Selected = preSelectedRoleId.HasValue && r.Id == preSelectedRoleId
                })
            };

            // 미리 선택된 값이 있으면 모델에 설정
            if (preSelectedMenuId.HasValue)
            {
                model.MenuId = preSelectedMenuId.Value;
            }

            if (preSelectedRoleId.HasValue)
            {
                model.RoleId = preSelectedRoleId.Value;
            }

            return View(model);
        }

        // POST: Admin/MenuRole/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(MenuRoleAssignViewModel model, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            if (!ModelState.IsValid)
            {
                // 유효성 검사 실패 시 드롭다운 목록 다시 채우기
                var menus = await _menuService.GetAllMenusAsync();
                var roles = await _roleService.GetAllRolesAsync();

                model.Menus = menus.Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = $"{m.MenuKey} - {m.Title}",
                    Selected = m.Id == model.MenuId
                });

                model.Roles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.RoleName,
                    Selected = r.Id == model.RoleId
                });

                SetPaginationViewBag(paginationInfo);
                return View(model);
            }

            try
            {
                // 이미 할당되어 있는지 확인
                bool alreadyAssigned = await _menuRoleService.MenuHasRoleAsync(model.MenuId, model.RoleId);
                if (alreadyAssigned)
                {
                    TempData["ErrorMessage"] = "해당 메뉴에는 이미 역할이 할당되어 있습니다.";

                    // 드롭다운 목록 다시 채우기
                    var menus = await _menuService.GetAllMenusAsync();
                    var roles = await _roleService.GetAllRolesAsync();

                    model.Menus = menus.Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = $"{m.MenuKey} - {m.Title}",
                        Selected = m.Id == model.MenuId
                    });

                    model.Roles = roles.Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.RoleName,
                        Selected = r.Id == model.RoleId
                    });

                    SetPaginationViewBag(paginationInfo);
                    return View(model);
                }

                // 메뉴에 역할 할당
                var assignMenuRoleDto = new AssignMenuRoleDto
                {
                    MenuId = model.MenuId,
                    RoleId = model.RoleId
                };
                bool result = await _menuRoleService.AssignRoleToMenuAsync(assignMenuRoleDto);
                if (result)
                {
                    TempData["SuccessMessage"] = "역할이 메뉴에 성공적으로 할당되었습니다.";

                    // 페이징 정보를 유지하면서 인덱스로 리디렉션
                    return RedirectToAction(nameof(Index), new
                    {
                        page = paginationInfo.ReturnPage,
                        pageSize = paginationInfo.ReturnPageSize,
                        searchTerm = paginationInfo.ReturnSearchTerm,
                        searchFields = paginationInfo.ReturnSearchFields
                    });
                }
                else
                {
                    TempData["ErrorMessage"] = "역할 할당 중 오류가 발생했습니다.";

                    // 드롭다운 목록 다시 채우기
                    var menus = await _menuService.GetAllMenusAsync();
                    var roles = await _roleService.GetAllRolesAsync();

                    model.Menus = menus.Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = $"{m.MenuKey} - {m.Title}",
                        Selected = m.Id == model.MenuId
                    });

                    model.Roles = roles.Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.RoleName,
                        Selected = r.Id == model.RoleId
                    });

                    SetPaginationViewBag(paginationInfo);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"에러 발생: {ex.Message}";

                // 드롭다운 목록 다시 채우기
                var menus = await _menuService.GetAllMenusAsync();
                var roles = await _roleService.GetAllRolesAsync();

                model.Menus = menus.Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = $"{m.MenuKey} - {m.Title}",
                    Selected = m.Id == model.MenuId
                });

                model.Roles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.RoleName,
                    Selected = r.Id == model.RoleId
                });

                SetPaginationViewBag(paginationInfo);
                return View(model);
            }
        }

        // POST: Admin/MenuRole/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int menuId, int roleId, string returnUrl = "", [FromForm] PaginationRequestViewModel paginationInfo = null)
        {
            try
            {
                // 메뉴 역할 제거
                bool result = await _menuRoleService.RemoveRoleFromMenuAsync(menuId, roleId);

                if (result)
                {
                    TempData["SuccessMessage"] = "역할이 메뉴에서 성공적으로 제거되었습니다.";
                }
                else
                {
                    TempData["ErrorMessage"] = "역할 제거 중 오류가 발생했습니다.";
                }

                // 리턴 URL이 있으면 해당 URL로 리디렉션
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // 페이징 정보가 없으면 기본값으로 설정
                paginationInfo ??= new PaginationRequestViewModel();

                // 인덱스 페이지로 리디렉션하되 페이징 정보 유지
                return RedirectToAction(nameof(Index), new
                {
                    page = paginationInfo.ReturnPage,
                    pageSize = paginationInfo.ReturnPageSize,
                    searchTerm = paginationInfo.ReturnSearchTerm,
                    searchFields = paginationInfo.ReturnSearchFields
                });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"에러 발생: {ex.Message}";

                // 리턴 URL이 있으면 해당 URL로 리디렉션
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // 페이징 정보가 없으면 기본값으로 설정
                paginationInfo ??= new PaginationRequestViewModel();

                // 인덱스 페이지로 리디렉션하되 페이징 정보 유지
                return RedirectToAction(nameof(Index), new
                {
                    page = paginationInfo.ReturnPage,
                    pageSize = paginationInfo.ReturnPageSize,
                    searchTerm = paginationInfo.ReturnSearchTerm,
                    searchFields = paginationInfo.ReturnSearchFields
                });
            }
        }

        // HTTP GET: Admin/MenuRole/RenderPagination
        [HttpGet]
        public IActionResult RenderPagination(int currentPage, int totalPages, int pageSize, int totalItems, string searchTerm = null, string searchFields = null)
        {
            var pagingModel = new PaginationViewModel
            {
                CurrentPage = currentPage,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems,
                ActionName = "Index",
                ControllerName = "MenuRole",
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
                string paginationHtml = ViewRenderHelper.RenderViewToString(this, ViewEngine, "Views/Shared/_Pagination.cshtml", pagingModel);

                return Json(new { paginationHtml });
            }
            catch (Exception ex)
            {
                Logger.LogError($"페이징 렌더링 오류: {ex.Message}");
                return Json(new { error = true, message = $"페이징 컴포넌트를 렌더링하는 중 오류가 발생했습니다: {ex.Message}" });
            }
        }
    }
}