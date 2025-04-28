using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using UnivFI.WebUI.Helpers;
using UnivFI.WebUI.Extensions;
using UnivFI.WebUI.ViewModels;
using UnivFI.WebUI.Areas.Admin.ViewModels.User;
using UnivFI.WebUI.Areas.Admin.ViewModels.UserRole;
using UnivFI.WebUI.Areas.Admin.ViewModels;
using UnivFI.WebUI.Controllers;
using System;
using System.Linq;

namespace UnivFI.WebUI.Areas.Admin.Controllers
{
    [Area(AdminAreaConfiguration.AreaName)]
    [Authorize(Roles = "Administrators")]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;
        private const int DefaultPageSize = 10;

        /// <summary>
        /// 필요한 의존성으로 UserController를 초기화하는 생성자
        /// </summary>
        /// <param name="userService">사용자 관련 작업을 위한 서비스</param>
        /// <param name="userRoleService">사용자 역할 관련 작업을 위한 서비스</param>
        /// <param name="roleService">역할 관련 작업을 위한 서비스</param>
        /// <param name="mapper">객체 매핑을 위한 Mapster 인스턴스</param>
        /// <param name="viewEngine">뷰 렌더링을 위한 뷰 엔진</param>
        /// <param name="tempDataFactory">TempData 팩토리</param>
        /// <param name="logger">로거 인스턴스</param>
        public UserController(
            IUserService userService,
            IUserRoleService userRoleService,
            IRoleService roleService,
            IMapper mapper,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            ILogger<UserController>? logger = null)
            : base(mapper, tempDataFactory, viewEngine, logger)
        {
            _userService = userService;
            _userRoleService = userRoleService;
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

        /// <summary>
        /// 선택적 검색 기능이 있는 페이지가 매겨진 사용자 목록을 표시합니다
        /// </summary>
        /// <param name="page">현재 페이지 번호 (기본값 1)</param>
        /// <param name="pageSize">페이지당 표시할 항목 수</param>
        /// <param name="searchTerm">사용자를 필터링하기 위한 선택적 검색어</param>
        /// <param name="searchFields">검색할 필드 (쉼표로 구분)</param>
        /// <returns>페이지가 매겨진 사용자 목록이 포함된 뷰</returns>
        public async Task<IActionResult> Index(int page = 1, int? pageSize = null, string? searchTerm = null, string? searchFields = null)
        {
            // 페이지 번호 유효성 검사
            page = ValidatePageNumber(page);

            // 페이지 크기 처리 - URL에서 전달된 값이 있으면 사용하고, 없으면 기본값 사용
            int actualPageSize = pageSize ?? DefaultPageSize;

            // 최소/최대 페이지 크기 제한 (선택적)
            if (actualPageSize < 5) actualPageSize = 5;
            if (actualPageSize > 100) actualPageSize = 100;

            // 검색 필드 기본값 설정
            searchFields ??= "Name,Email";

            // 유저 목록과 총 개수 가져오기 - actualPageSize 사용
            var users = await _userService.GetUsersAsync(page, actualPageSize, searchTerm, searchFields);
            var totalCount = await _userService.GetTotalCountAsync(searchTerm, searchFields);

            // 내림차순 정렬 적용
            var orderedUsers = users.OrderByDescending(u => u.Id);

            // 페이지네이션 정보 계산 - actualPageSize 사용
            var totalPages = CalculateTotalPages(totalCount, actualPageSize);

            // ViewModel로 변환
            var userViewModels = Mapper.Map<IEnumerable<UserViewModel>>(orderedUsers);

            // 목록 ViewModel 생성 및 설정 - actualPageSize 사용
            var model = new UserListViewModel
            {
                Users = userViewModels,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalCount,
                PageSize = actualPageSize,
                SearchTerm = searchTerm ?? string.Empty,
                SearchFields = searchFields ?? string.Empty
            };

            return View(model);
        }

        /// <summary>
        /// 사용자 생성 양식을 표시합니다
        /// </summary>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>빈 사용자 생성 양식이 있는 뷰</returns>
        public IActionResult Create([FromQuery] PaginationRequestViewModel paginationInfo)
        {
            SetPaginationViewBag(paginationInfo);
            return View();
        }

        /// <summary>
        /// 사용자 생성 양식 제출을 처리합니다
        /// </summary>
        /// <param name="model">사용자 생성 양식의 데이터</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>성공 시 Index로 리디렉션, 그렇지 않으면 유효성 검사 오류가 있는 양식을 반환</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            if (ModelState.IsValid)
            {
                var createUserDto = Mapper.Map<CreateUserDto>(model);
                var userId = await _userService.CreateUserAsync(createUserDto);

                // 생성 성공 후 목록으로 돌아갈 때 검색 조건과 페이지 번호 유지
                return RedirectToAction(nameof(Index), paginationInfo.ToRouteValues());
            }

            SetPaginationViewBag(paginationInfo);
            return View(model);
        }

        /// <summary>
        /// 특정 사용자에 대한 상세 정보를 표시합니다
        /// </summary>
        /// <param name="id">표시할 사용자의 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>상세 사용자 정보가 있는 뷰 또는 사용자가 존재하지 않는 경우 NotFound</returns>
        public async Task<IActionResult> Detail(int id, [FromQuery] PaginationRequestViewModel paginationInfo)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = Mapper.Map<UserViewModel>(user);

            // 사용자 역할 정보 조회
            var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(id);
            viewModel.Roles = userRoles
                .Select(ur => ur.RoleName)
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList();

            SetPaginationViewBag(paginationInfo);
            return View(viewModel);
        }

        /// <summary>
        /// 기존 사용자 편집을 위한 양식을 표시합니다
        /// </summary>
        /// <param name="id">편집할 사용자의 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>미리 채워진 편집 양식이 있는 뷰 또는 사용자가 존재하지 않는 경우 NotFound</returns>
        public async Task<IActionResult> Edit(int id, [FromQuery] PaginationRequestViewModel paginationInfo)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var updateUserDto = new UpdateUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Email = user.Email
            };

            SetPaginationViewBag(paginationInfo);
            return View(updateUserDto);
        }

        /// <summary>
        /// 사용자 편집 양식 제출을 처리합니다
        /// </summary>
        /// <param name="model">편집 양식에서 업데이트된 사용자 데이터</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>성공 시 Index로 리디렉션, 그렇지 않으면 유효성 검사 오류가 있는 양식 또는 NotFound 반환</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateUserDto model, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            if (ModelState.IsValid)
            {
                var success = await _userService.UpdateUserAsync(model);
                if (!success)
                {
                    return NotFound();
                }

                // 수정 성공 후 목록으로 돌아갈 때 검색 조건과 페이지 번호, 페이지 크기 유지
                return RedirectToAction(nameof(Index), paginationInfo.ToRouteValues());
            }

            SetPaginationViewBag(paginationInfo);
            return View(model);
        }

        /// <summary>
        /// 사용자 삭제 확인 페이지를 표시합니다
        /// </summary>
        /// <param name="id">삭제할 사용자의 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>사용자 삭제 확인 페이지</returns>
        public async Task<IActionResult> Delete(int id, [FromQuery] PaginationRequestViewModel paginationInfo)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = Mapper.Map<UserViewModel>(user);

            // 사용자 역할 정보 조회
            var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(id);
            viewModel.Roles = userRoles
                .Select(ur => ur.RoleName)
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList();

            SetPaginationViewBag(paginationInfo);
            return View(viewModel);
        }

        /// <summary>
        /// 지정된 ID의 사용자를 삭제합니다
        /// </summary>
        /// <param name="id">삭제할 사용자의 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>성공 시 Index로 리디렉션 또는 사용자가 존재하지 않는 경우 NotFound</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            var success = await _userService.DeleteUserAsync(id);
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
                ControllerName = "User",
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

        /// <summary>
        /// 사용자의 역할을 관리하는 페이지를 표시합니다
        /// </summary>
        /// <param name="id">역할을 관리할 사용자의 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>사용자 역할 관리 페이지</returns>
        public async Task<IActionResult> ManageRoles(int id, [FromQuery] PaginationRequestViewModel paginationInfo)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // 모든 역할 목록 가져오기 (IRoleService 사용)
            var allRoles = await _roleService.GetAllRolesAsync();

            // 사용자의 현재 역할 목록 가져오기
            var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(id);
            var userRoleIds = userRoles.Select(ur => ur.RoleId).ToList();

            var viewModel = new UserRoleManageViewModel
            {
                UserId = id,
                UserName = user.Name,
                UserEmail = user.Email,
                AssignedRoles = userRoles.ToList(),
                AvailableRoles = allRoles
                    .Where(r => !userRoleIds.Contains(r.Id))
                    .ToList()
            };

            SetPaginationViewBag(paginationInfo);
            return View(viewModel);
        }

        /// <summary>
        /// 사용자에게 역할을 할당합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="roleId">할당할 역할 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>사용자 역할 관리 페이지로 리디렉션</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int userId, int roleId, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            var success = await _userRoleService.AssignRoleToUserAsync(userId, roleId);

            if (!success)
            {
                TempData["ErrorMessage"] = "역할 할당 중 오류가 발생했습니다.";
            }
            else
            {
                TempData["SuccessMessage"] = "역할이 성공적으로 할당되었습니다.";
            }

            return RedirectToAction(nameof(ManageRoles), new
            {
                id = userId,
                returnPage = paginationInfo.ReturnPage,
                returnPageSize = paginationInfo.ReturnPageSize,
                returnSearchTerm = paginationInfo.ReturnSearchTerm,
                returnSearchFields = paginationInfo.ReturnSearchFields
            });
        }

        /// <summary>
        /// 사용자로부터 역할을 제거합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="roleId">제거할 역할 ID</param>
        /// <param name="paginationInfo">페이지네이션 및 검색 정보</param>
        /// <returns>사용자 역할 관리 페이지로 리디렉션</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(int userId, int roleId, [FromForm] PaginationRequestViewModel paginationInfo)
        {
            var success = await _userRoleService.RemoveRoleFromUserAsync(userId, roleId);

            if (!success)
            {
                TempData["ErrorMessage"] = "역할 제거 중 오류가 발생했습니다.";
            }
            else
            {
                TempData["SuccessMessage"] = "역할이 성공적으로 제거되었습니다.";
            }

            return RedirectToAction(nameof(ManageRoles), new
            {
                id = userId,
                returnPage = paginationInfo.ReturnPage,
                returnPageSize = paginationInfo.ReturnPageSize,
                returnSearchTerm = paginationInfo.ReturnSearchTerm,
                returnSearchFields = paginationInfo.ReturnSearchFields
            });
        }
    }
}