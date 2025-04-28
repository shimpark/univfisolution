using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.WebUI.Areas.Admin.ViewModels.Member;
using X.PagedList;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using UnivFI.WebUI.Controllers;
using UnivFI.WebUI.Areas.Admin.ViewModels.Common;

namespace UnivFI.WebUI.Areas.Admin.Controllers
{
    [Area(AdminAreaConfiguration.AreaName)]
    [Authorize(Roles = "Administrators")]
    public class MemberController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;
        private const int DefaultPageSize = 10;

        public MemberController(
            IUserService userService,
            IUserRoleService userRoleService,
            IRoleService roleService,
            IMapper mapper,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            ILogger<MemberController>? logger = null)
            : base(mapper, tempDataFactory, viewEngine, logger)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _roleService = roleService;
        }

        public async Task<IActionResult> Index([FromQuery] SearchViewModel search)
        {
            try
            {
                // 페이지 번호와 크기 설정
                int currentPage = search?.Page ?? 1;
                int itemsPerPage = search?.PageSize ?? DefaultPageSize;

                // 검색 필드 기본값 설정
                string searchFields = search?.SearchFields ?? "Name,Email";

                // 정렬 파라미터 처리
                ViewBag.NameSortParm = string.IsNullOrEmpty(search.SortOrder) ? "Name" : "";
                ViewBag.EmailSortParm = search.SortOrder == "Email" ? "email_desc" : "Email";
                ViewBag.DateSortParm = search.SortOrder == "Date" ? "date_desc" : "Date";

                // 사용자 목록 조회 (정렬 파라미터 포함)
                var users = await _userService.GetUsersAsync(currentPage, itemsPerPage, search.SearchTerm, searchFields, search.SortOrder);
                var totalCount = await _userService.GetTotalCountAsync(search.SearchTerm, searchFields);

                // ViewModel로 변환
                var memberViewModels = Mapper.Map<IEnumerable<MemberViewModel>>(users);

                // X.PagedList 생성
                var pagedList = new StaticPagedList<MemberViewModel>(
                    memberViewModels,
                    currentPage,
                    itemsPerPage,
                    totalCount
                );

                // ViewModel 설정
                var model = new MemberListViewModel
                {
                    Members = pagedList,
                    SearchTerm = search.SearchTerm ?? string.Empty,
                    SearchFields = searchFields,
                    CurrentSort = search.SortOrder,
                    PageSize = itemsPerPage,
                    CurrentPage = currentPage
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "멤버 목록을 가져오는 중 오류가 발생했습니다.");
                TempData["ErrorMessage"] = "멤버 목록을 불러오는 중 오류가 발생했습니다.";
                return RedirectToAction("Index", "Home", new { area = AdminAreaConfiguration.AreaName });
            }
        }

        public async Task<IActionResult> Detail(int id, [FromQuery] SearchViewModel returnSearch)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var viewModel = Mapper.Map<MemberViewModel>(user);

                // 사용자 역할 정보 조회
                var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(id);
                viewModel.Roles = userRoles
                    .Select(ur => ur.RoleName)
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList();

                // 반환 파라미터 설정
                viewModel.ReturnSearch = returnSearch;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "멤버 상세 정보를 가져오는 중 오류가 발생했습니다. ID: {Id}", id);
                TempData["ErrorMessage"] = "멤버 정보를 불러오는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index), new
                {
                    page = returnSearch.Page,
                    pageSize = returnSearch.PageSize,
                    searchTerm = returnSearch.SearchTerm,
                    searchFields = returnSearch.SearchFields,
                    sortOrder = returnSearch.SortOrder
                });
            }
        }
    }
}