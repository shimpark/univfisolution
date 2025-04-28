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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using UnivFI.WebUI.Areas.Admin.ViewModels.UserRole;
using UnivFI.WebUI.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace UnivFI.WebUI.Areas.Admin.Controllers
{
    [Area(AdminAreaConfiguration.AreaName)]
    [Authorize(Roles = "Administrators")] // 쿠키 인증 사용 (기본 스키마)
    public class UserRoleController : BaseController
    {
        private readonly IUserRoleService _userRoleService;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public UserRoleController(
            IUserRoleService userRoleService,
            IUserService userService,
            IRoleService roleService,
            IMapper mapper,
            ITempDataDictionaryFactory tempDataFactory,
            ICompositeViewEngine viewEngine,
            ILogger<UserRoleController> logger = null)
            : base(mapper, tempDataFactory, viewEngine, logger)
        {
            _userRoleService = userRoleService;
            _userService = userService;
            _roleService = roleService;
        }

        // GET: UserRole
        public async Task<IActionResult> Index(string searchTerm = "", int userId = 0, int roleId = 0, int page = 1, int pageSize = 10)
        {
            // 페이징 및 검색 기능이 적용된.사용자-역할 목록 조회
            var pagedResult = await _userRoleService.GetPagedUserRolesAsync(searchTerm, userId, roleId, page, pageSize);

            // 사용자 목록 가져오기 (필터링 드롭다운용)
            var users = await _userService.GetAllUsersAsync();
            var usersList = users.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.Name} ({u.Email})",
                Selected = userId == u.Id
            }).ToList();

            // 역할 목록 가져오기 (필터링 드롭다운용)
            var roles = await _roleService.GetAllRolesAsync();
            var rolesList = roles.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.RoleName,
                Selected = roleId == r.Id
            }).ToList();

            // 뷰모델 구성
            var viewModel = new UserRoleIndexViewModel
            {
                UserRoles = Mapper.Map<IEnumerable<UserRoleViewModel>>(pagedResult.Items),
                TotalItems = pagedResult.TotalCount,
                CurrentPage = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                UserId = userId,
                RoleId = roleId,
                UsersList = usersList,
                RolesList = rolesList
            };

            return View(viewModel);
        }

        // GET: UserRole/ByUser/5
        public async Task<IActionResult> ByUser(int userId)
        {
            var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(userId);
            var viewModels = Mapper.Map<IEnumerable<UserRoleViewModel>>(userRoles);

            var user = await _userService.GetUserByIdAsync(userId);
            ViewBag.UserName = user?.Name ?? "알 수 없는 사용자";
            ViewBag.UserId = userId;

            return View(viewModels);
        }

        // GET: UserRole/ByRole/5
        public async Task<IActionResult> ByRole(int roleId)
        {
            var userRoles = await _userRoleService.GetUserRolesByRoleIdAsync(roleId);
            var viewModels = Mapper.Map<IEnumerable<UserRoleViewModel>>(userRoles);

            var role = await _roleService.GetRoleByIdAsync(roleId);
            ViewBag.RoleName = role?.RoleName ?? "알 수 없는 역할";
            ViewBag.RoleId = roleId;

            return View(viewModels);
        }

        // GET: UserRole/Assign
        public async Task<IActionResult> Assign(int? preSelectedUserId = null, int? preSelectedRoleId = null)
        {
            var users = await _userService.GetAllUsersAsync();
            var roles = await _roleService.GetAllRolesAsync();

            // SelectListItem 명시적으로 생성
            var userSelectList = users.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.Name} ({u.Email})",
                Selected = preSelectedUserId.HasValue && preSelectedUserId.Value == u.Id
            }).ToList();

            var roleSelectList = roles.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.RoleName,
                Selected = preSelectedRoleId.HasValue && preSelectedRoleId.Value == r.Id
            }).ToList();

            var model = new UserRoleAssignViewModel
            {
                Users = userSelectList,
                Roles = roleSelectList
            };

            // 미리 선택된 값이 있으면 모델에 설정
            if (preSelectedUserId.HasValue)
            {
                model.UserId = preSelectedUserId.Value;
            }

            if (preSelectedRoleId.HasValue)
            {
                model.RoleId = preSelectedRoleId.Value;
            }

            return View(model);
        }

        // POST: UserRole/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("AssignSubmit")]
        public async Task<IActionResult> AssignSubmit(UserRoleAssignViewModel model)
        {
            if (model.UserId <= 0 || model.RoleId <= 0)
            {
                return Json(new { success = false, message = "사용자와 역할을 모두 선택해주세요." });
            }

            // 이미 할당되어 있는지 확인
            var alreadyAssigned = await _userRoleService.UserHasRoleAsync(model.UserId, model.RoleId);
            if (alreadyAssigned)
            {
                return Json(new { success = false, message = "이미 해당 사용자에게 역할이 할당되어 있습니다." });
            }

            var result = await _userRoleService.AssignRoleToUserAsync(model.UserId, model.RoleId);
            return Json(new { success = result, message = result ? "역할이 성공적으로 할당되었습니다." : "역할 할당 중 오류가 발생했습니다." });
        }

        // POST: UserRole/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(UserRoleAssignViewModel model)
        {
            if (model.UserId <= 0 || model.RoleId <= 0)
            {
                return BadRequest(ModelState);
            }

            var result = await _userRoleService.RemoveRoleFromUserAsync(model.UserId, model.RoleId);
            return Json(new { success = result });
        }

        // GET: UserRole/GetRolesByUserId/5
        [HttpGet]
        public async Task<IActionResult> GetRolesByUserId(int userId)
        {
            var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(userId);
            return Json(userRoles);
        }

        // GET: UserRole/HasRole
        [HttpGet]
        public async Task<IActionResult> HasRole(int userId, int roleId)
        {
            var hasRole = await _userRoleService.UserHasRoleAsync(userId, roleId);
            return Json(new { hasRole });
        }

        // GET: UserRole/GetUsersByRoleId/5
        [HttpGet]
        public async Task<IActionResult> GetUsersByRoleId(int roleId)
        {
            var userRoles = await _userRoleService.GetUserRolesByRoleIdAsync(roleId);
            return Json(userRoles);
        }

        // GET: UserRole/GetRoleAssignmentsForUser/5
        [HttpGet]
        public async Task<IActionResult> GetRoleAssignmentsForUser(int userId)
        {
            // 모든 역할 가져오기
            var allRoles = await _roleService.GetAllRolesAsync();

            // 사용자에게 할당된 역할 ID 가져오기
            var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(userId);
            var assignedRoleIds = userRoles.Select(ur => ur.RoleId).ToList();

            // 모든 역할의 할당 상태 설정
            var roleAssignments = allRoles.Select(role => new
            {
                roleId = role.Id,
                roleName = role.RoleName,
                isAssigned = assignedRoleIds.Contains(role.Id)
            });

            return Json(roleAssignments);
        }

        // GET: UserRole/GetUserAssignmentsForRole/5
        [HttpGet]
        public async Task<IActionResult> GetUserAssignmentsForRole(int roleId)
        {
            // 모든 사용자 가져오기
            var allUsers = await _userService.GetAllUsersAsync();

            // 역할에 할당된 사용자 ID 가져오기
            var roleUsers = await _userRoleService.GetUserRolesByRoleIdAsync(roleId);
            var assignedUserIds = roleUsers.Select(ur => ur.UserId).ToList();

            // 모든 사용자의 할당 상태 설정
            var userAssignments = allUsers.Select(user => new
            {
                userId = user.Id,
                userName = user.Email,
                name = user.Name,
                isAssigned = assignedUserIds.Contains(user.Id)
            });

            return Json(userAssignments);
        }


    }
}
