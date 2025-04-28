using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UnivFI.Domain.Entities;
using UnivFI.WebUI.ViewModels;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using UnivFI.WebUI.Helpers;
using Mapster;
using MapsterMapper;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.WebUI.Areas.Admin.ViewModels.Role;
using Microsoft.Extensions.Logging;
using UnivFI.WebUI.Extensions;
using UnivFI.WebUI.Controllers;
using UnivFI.WebUI.Areas.Admin.ViewModels;

namespace UnivFI.WebUI.Areas.Admin.Controllers
{
    [Area(AdminAreaConfiguration.AreaName)]
    [Authorize(Roles = "Administrators")]
    public class RoleController : BaseController
    {
        private readonly IRoleService _roleService;
        private readonly IMenuRoleService _menuRoleService;
        private readonly IUserRoleService _userRoleService;
        private const int DefaultPageSize = 10;

        /// <summary>
        /// 역할 컨트롤러 생성자
        /// </summary>
        /// <param name="roleService">역할 서비스</param>
        /// <param name="menuRoleService">메뉴-역할 서비스</param>
        /// <param name="userRoleService">사용자-역할 서비스</param>
        /// <param name="mapper">맵터</param>
        /// <param name="viewEngine">뷰 엔진</param>
        /// <param name="tempDataFactory">임시 데이터 팩토리</param>
        /// <param name="logger">로거</param>
        public RoleController(
            IRoleService roleService,
            IMenuRoleService menuRoleService,
            IUserRoleService userRoleService,
            IMapper mapper,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            ILogger<RoleController> logger)
            : base(mapper, tempDataFactory, viewEngine, logger)
        {
            _roleService = roleService;
            _menuRoleService = menuRoleService;
            _userRoleService = userRoleService;
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

        /// <summary>
        /// 역할 목록 조회
        /// </summary>
        /// <param name="page">페이지 번호</param>
        /// <param name="pageSize">페이지당 표시할 항목 수</param>
        /// <param name="searchTerm">검색어</param>
        /// <param name="searchFields">검색 필드</param>
        /// <returns>역할 목록 뷰</returns>
        public async Task<IActionResult> Index(int page = 1, int? pageSize = null, string? searchTerm = null, string? searchFields = null)
        {
            // 페이지 번호 유효성 검사
            if (page < 1) page = 1;

            // 페이지 크기 처리 - URL에서 전달된 값이 있으면 사용하고, 없으면 기본값 사용
            int actualPageSize = pageSize ?? DefaultPageSize;

            // 최소/최대 페이지 크기 제한 (선택적)
            if (actualPageSize < 5) actualPageSize = 5;
            if (actualPageSize > 100) actualPageSize = 100;

            // 검색 필드 기본값 설정
            searchFields ??= "RoleName,RoleComment";

            // 역할 목록과 총 개수 가져오기
            var roles = await _roleService.GetPagedRolesAsync(page, actualPageSize, searchTerm, searchFields);
            var totalCount = await _roleService.GetTotalCountAsync(searchTerm, searchFields);

            // 페이지네이션 정보 계산
            var totalPages = (int)Math.Ceiling(totalCount / (double)actualPageSize);
            var roleViewModels = Mapper.Map<IEnumerable<RoleViewModel>>(roles);

            // 목록 ViewModel 생성 및 설정
            var model = new RoleListViewModel
            {
                Roles = roles ?? Enumerable.Empty<RoleDto>(),
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalCount,
                PageSize = actualPageSize,
                SearchTerm = searchTerm ?? string.Empty,
                SearchFields = searchFields
            };

            return View(model);
        }

        /// <summary>
        /// 역할 생성 양식을 표시합니다
        /// </summary>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>빈 역할 생성 양식이 있는 뷰</returns>
        public IActionResult Create([FromQuery] PaginationRequestViewModel paginationInfo)
        {
            SetPaginationViewBag(paginationInfo);
            return View();
        }

        /// <summary>
        /// 역할 생성 양식 제출을 처리합니다
        /// </summary>
        /// <param name="model">역할 생성 양식의 데이터</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>성공 시 Index로 리디렉션, 그렇지 않으면 유효성 검사 오류가 있는 양식을 반환</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleCreateViewModel model, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            if (ModelState.IsValid)
            {
                var createRoleDto = Mapper.Map<CreateRoleDto>(model);
                var roleId = await _roleService.CreateRoleAsync(createRoleDto);

                // 생성 성공 후 목록으로 돌아갈 때 검색 조건과 페이지 번호 유지
                return RedirectToAction(nameof(Index), paginationInfo.ToRouteValues());
            }

            SetPaginationViewBag(paginationInfo);
            return View(model);
        }

        /// <summary>
        /// 역할 세부 정보를 표시합니다
        /// </summary>
        /// <param name="id">표시할 역할의 ID</param>
        /// <param name="returnPage">반환할 페이지 번호</param>
        /// <param name="returnPageSize">반환할 페이지당 항목 수</param>
        /// <param name="returnSearchTerm">반환할 검색어</param>
        /// <param name="returnSearchFields">반환할 검색 필드</param>
        /// <param name="menuPage">메뉴 목록을 가져올 페이지 번호</param>
        /// <param name="menuPageSize">메뉴 목록을 가져올 페이지당 항목 수</param>
        /// <param name="menuSearchTerm">메뉴 목록을 검색할 검색어</param>
        /// <param name="userPage">사용자 목록을 가져올 페이지 번호</param>
        /// <param name="userPageSize">사용자 목록을 가져올 페이지당 항목 수</param>
        /// <param name="userSearchTerm">사용자 목록을 검색할 검색어</param>
        /// <param name="activeTab">활성 탭</param>
        /// <returns>역할 세부 정보 뷰</returns>
        public async Task<IActionResult> Detail(int id,
            int returnPage = 1, int returnPageSize = 10,
            string returnSearchTerm = "", string returnSearchFields = "",
            int menuPage = 1, int menuPageSize = 10, string menuSearchTerm = "",
            int userPage = 1, int userPageSize = 10, string userSearchTerm = "",
            string activeTab = "menus")
        {
            try
            {
                var role = await _roleService.GetByIdAsync(id);
                if (role == null)
                {
                    TempData["ErrorMessage"] = "역할을 찾을 수 없습니다.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = Mapper.Map<RoleViewModel>(role);

                // 메뉴 목록 조회 (페이징 및 검색 적용)
                var (assignedMenus, menuTotalCount) = await _roleService.GetAssignedMenusAsync(id, menuPage, menuPageSize, menuSearchTerm);
                ViewBag.AssignedMenus = assignedMenus;
                ViewBag.MenuPage = menuPage;
                ViewBag.MenuPageSize = menuPageSize;
                ViewBag.MenuSearchTerm = menuSearchTerm;
                ViewBag.MenuTotalCount = menuTotalCount;

                // 사용자 목록 조회 (페이징 및 검색 적용)
                var (assignedUsers, userTotalCount) = await _roleService.GetAssignedUsersAsync(id, userPage, userPageSize, userSearchTerm);
                ViewBag.AssignedUsers = assignedUsers;
                ViewBag.UserPage = userPage;
                ViewBag.UserPageSize = userPageSize;
                ViewBag.UserSearchTerm = userSearchTerm;
                ViewBag.UserTotalCount = userTotalCount;

                // 목록 페이지 상태 저장
                ViewBag.ReturnPage = returnPage;
                ViewBag.ReturnPageSize = returnPageSize;
                ViewBag.ReturnSearchTerm = returnSearchTerm;
                ViewBag.ReturnSearchFields = returnSearchFields;

                // 활성 탭 상태 저장
                ViewBag.ActiveTab = activeTab;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "역할 상세 정보를 조회하는 중 오류가 발생했습니다. ID: {Id}", id);
                TempData["ErrorMessage"] = "역할 정보를 불러오는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 역할 수정 페이지
        /// </summary>
        /// <param name="id">역할 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>역할 수정 뷰</returns>
        public async Task<IActionResult> Edit(int id, [FromQuery] PaginationRequestViewModel paginationInfo)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var viewModel = new RoleUpdateViewModel
            {
                Id = role.Id,
                RoleName = role.RoleName,
                RoleComment = role.RoleComment
            };

            SetPaginationViewBag(paginationInfo);
            return View(viewModel);
        }

        /// <summary>
        /// 역할 편집 양식 제출을 처리합니다
        /// </summary>
        /// <param name="viewModel">편집된 역할 데이터</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>성공 시 Index로 리디렉션</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleUpdateViewModel viewModel, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            try
            {
                var updateRoleDto = new UpdateRoleDto
                {
                    Id = viewModel.Id,
                    RoleName = viewModel.RoleName,
                    RoleComment = viewModel.RoleComment
                };

                var success = await _roleService.UpdateRoleAsync(updateRoleDto);
                if (!success)
                {
                    ModelState.AddModelError("", "역할 수정에 실패했습니다.");
                    SetPaginationViewBag(paginationInfo);
                    return View(viewModel);
                }

                return RedirectToAction(nameof(Index), paginationInfo.ToRouteValues());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "역할 수정 중 오류 발생");
                ModelState.AddModelError("", "역할을 수정하는 중 오류가 발생했습니다.");
                SetPaginationViewBag(paginationInfo);
                return View(viewModel);
            }
        }

        /// <summary>
        /// 역할 삭제 확인 페이지를 표시합니다
        /// </summary>
        /// <param name="id">삭제할 역할의 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>삭제 확인 뷰</returns>
        public async Task<IActionResult> Delete(int id, [FromQuery] PaginationRequestViewModel paginationInfo)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var viewModel = Mapper.Map<RoleViewModel>(role);

            SetPaginationViewBag(paginationInfo);
            return View(viewModel);
        }

        /// <summary>
        /// 역할 삭제를 처리합니다
        /// </summary>
        /// <param name="id">삭제할 역할의 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>성공 시 Index로 리디렉션</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            var success = await _roleService.DeleteRoleAsync(id);
            if (!success)
            {
                return NotFound();
            }

            // 삭제 후에는 검색 조건은 유지하되 항상 1페이지로 이동
            var routeValues = new
            {
                page = 1, // 삭제 후 항상 1페이지로 이동
                pageSize = paginationInfo.ReturnPageSize,
                searchTerm = paginationInfo.ReturnSearchTerm,
                searchFields = paginationInfo.ReturnSearchFields
            };

            return RedirectToAction(nameof(Index), routeValues);
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
                ControllerName = "Role",
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
                string paginationHtml = ViewRenderHelper.RenderViewToString(this, ViewEngine, "~/Views/Shared/_Pagination.cshtml", pagingModel);

                return Json(new { paginationHtml });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "페이징 컴포넌트 렌더링 오류");
                return Json(new { error = true, message = $"페이징 컴포넌트를 렌더링하는 중 오류가 발생했습니다: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMenu(int roleId, int menuId)
        {
            try
            {
                var result = await _roleService.RemoveMenuFromRoleAsync(roleId, menuId);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error removing menu {MenuId} from role {RoleId}", menuId, roleId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUser(int roleId, int userId)
        {
            try
            {
                var result = await _roleService.RemoveUserFromRoleAsync(roleId, userId);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error removing user {UserId} from role {RoleId}", userId, roleId);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}