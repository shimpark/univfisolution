using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.WebUI.ViewModels.UIElement;
using Mapster;
using MapsterMapper;
using System.Collections.Generic;
using System.Linq;

namespace UnivFI.WebUI.Controllers
{
    [Authorize(Roles = "Administrators")]
    public class UIElementController : BaseController
    {
        private readonly IUIElementService _uiElementService;
        private readonly IUIElementUserPermissionService _permissionService;
        private readonly IUserService _userService;
        private readonly ILogger<UIElementController> _logger;

        public UIElementController(
            IUIElementService uiElementService,
            IUIElementUserPermissionService permissionService,
            IUserService userService,
            IMapper mapper,
            ITempDataDictionaryFactory tempDataFactory,
            ICompositeViewEngine viewEngine,
            ILogger<UIElementController> logger)
            : base(mapper, tempDataFactory, viewEngine, logger)
        {
            _uiElementService = uiElementService ?? throw new ArgumentNullException(nameof(uiElementService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string searchTerm = null, string searchFields = null)
        {
            try
            {
                // 검색 필드가 지정되지 않았을 경우 기본값으로 모든 필드 설정
                if (string.IsNullOrEmpty(searchFields))
                {
                    searchFields = "ElementKey,ElementName,Description";
                }

                // 검색 필드 처리
                var searchFieldList = !string.IsNullOrEmpty(searchFields)
                    ? searchFields.Split(',').ToList()
                    : new List<string>();

                // 검색 조건에 따른 UI 요소 조회
                var elementsDto = await _uiElementService.GetAllAsync();

                // 검색어가 있는 경우 필터링
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    elementsDto = elementsDto.Where(e =>
                        (searchFieldList.Contains("ElementKey") && e.ElementKey?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                        (searchFieldList.Contains("ElementName") && e.ElementName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                        (searchFieldList.Contains("Description") && e.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                    ).ToList();
                }

                // 페이징 처리
                var totalItems = elementsDto.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var pagedElements = elementsDto
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 새로운 ViewModel 생성 및 설정
                var viewModel = new UIElementListViewModel
                {
                    Items = UIElementViewModel.FromDtoList(pagedElements),
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    SearchTerm = searchTerm,
                    SearchFields = searchFields
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 목록 조회 중 오류 발생: {Message}", ex.Message);
                return View(new UIElementListViewModel());
            }
        }

        public IActionResult Create()
        {
            return View(new CreateUIElementViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUIElementViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var id = await _uiElementService.CreateAsync(model.ToDto());
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 생성 중 오류 발생: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "UI 요소를 생성하는 중 오류가 발생했습니다.");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 10, string searchTerm = null, string searchFields = null)
        {
            var element = await _uiElementService.GetByIdAsync(id);
            if (element == null)
                return NotFound();

            var model = new UpdateUIElementViewModel
            {
                Id = element.Id,
                ElementName = element.ElementName,
                ElementType = element.ElementType,
                Description = element.Description,
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SearchFields = searchFields
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateUIElementViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            try
            {
                var updateDto = new UpdateUIElementDto
                {
                    ElementName = model.ElementName,
                    ElementType = model.ElementType,
                    Description = model.Description
                };

                var result = await _uiElementService.UpdateAsync(id, updateDto);
                if (!result)
                    return NotFound();

                return RedirectToAction(nameof(Index), new
                {
                    page = model.Page,
                    pageSize = model.PageSize,
                    searchTerm = model.SearchTerm,
                    searchFields = model.SearchFields
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 수정 중 오류 발생 - ID: {Id}: {Message}", id, ex.Message);
                ModelState.AddModelError(string.Empty, "UI 요소를 수정하는 중 오류가 발생했습니다.");
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id, int page = 1, int pageSize = 10, string searchTerm = null, string searchFields = null)
        {
            var elementDto = await _uiElementService.GetByIdAsync(id);
            if (elementDto == null)
                return NotFound();

            // UI 요소 권한 정보 가져오기
            var permissions = await _permissionService.GetPermissionsByElementIdAsync(id);

            var viewModel = UIElementViewModel.FromDto(elementDto);
            viewModel.Page = page;
            viewModel.PageSize = pageSize;
            viewModel.SearchTerm = searchTerm;
            viewModel.SearchFields = searchFields;

            // 권한 정보 설정 - 단순히 ElementId와 UserId만 추출
            if (permissions != null)
            {
                var userPermViewModels = new List<UIElementUserPermissionViewModel>();

                foreach (var p in permissions)
                {
                    var userDto = await _userService.GetUserByIdAsync(p.UserId);
                    var permViewModel = new UIElementUserPermissionViewModel
                    {
                        ElementId = p.ElementId,
                        UserId = p.UserId,
                        ElementKey = elementDto.ElementKey,
                        ElementName = elementDto.ElementName,
                        ElementType = elementDto.ElementType,
                        User = userDto
                    };
                    userPermViewModels.Add(permViewModel);
                }

                viewModel.UserPermissions = userPermViewModels;
            }

            // 성공 메시지가 있으면 ViewBag에 전달
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(int id, int page = 1, int pageSize = 10, string searchTerm = null, string searchFields = null)
        {
            var elementDto = await _uiElementService.GetByIdAsync(id);
            if (elementDto == null)
                return NotFound();

            var viewModel = UIElementViewModel.FromDto(elementDto);
            viewModel.Page = page;
            viewModel.PageSize = pageSize;
            viewModel.SearchTerm = searchTerm;
            viewModel.SearchFields = searchFields;

            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int page = 1, int pageSize = 10, string searchTerm = null, string searchFields = null)
        {
            try
            {
                var result = await _uiElementService.DeleteAsync(id);
                if (!result)
                    return NotFound();

                return RedirectToAction(nameof(Index), new
                {
                    page = page,
                    pageSize = pageSize,
                    searchTerm = searchTerm,
                    searchFields = searchFields
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 삭제 중 오류 발생 - ID: {Id}: {Message}", id, ex.Message);
                return RedirectToAction(nameof(Index), new
                {
                    page = page,
                    pageSize = pageSize,
                    searchTerm = searchTerm,
                    searchFields = searchFields
                });
            }
        }

        public async Task<IActionResult> ManageUserPermissions(int id, int page = 1, int pageSize = 20, string searchTerm = null, string sortField = null, string sortOrder = "asc")
        {
            // 요소 정보 가져오기
            var element = await _uiElementService.GetByIdAsync(id);
            if (element == null)
                return NotFound();

            // 모든 사용자 목록 가져오기 (사용자 서비스 필요)
            var users = await _userService.GetUsersAsync(page, pageSize, searchTerm);

            // 총 사용자 수 가져오기
            var totalCount = await _userService.GetTotalCountAsync(searchTerm);

            // 해당 요소에 대한 모든 사용자 권한 가져오기
            var permissions = await _permissionService.GetPermissionsByElementIdAsync(id);

            // DTO를 ViewModel로 변환
            var permissionViewModels = permissions?
                .Select(p => UIElementUserPermissionViewModel.FromDto(p))
                .ToList() ?? new List<UIElementUserPermissionViewModel>();

            var model = new ManageUserPermissionsViewModel
            {
                ElementId = element.Id,
                ElementName = element.ElementName,
                ElementKey = element.ElementKey,
                Users = users.ToList(),
                UserPermissions = permissionViewModels,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                SearchTerm = searchTerm,
                SortField = sortField,
                SortOrder = sortOrder
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUserPermissions(ManageUserPermissionsViewModel model)
        {
            // if (!ModelState.IsValid)
            //     return View("ManageUserPermissions", model);

            try
            {
                bool allSuccess = true;

                // 각 사용자 권한 저장
                for (int i = 0; i < model.UserPermissions.Count; i++)
                {
                    var permission = model.UserPermissions[i];

                    if (permission.IsEnabled) // UI 표시 용도로만 사용되는 속성
                    {
                        // 권한 활성화된 사용자는 레코드 추가 또는 유지
                        var dto = new CreateUIElementUserPermissionDto
                        {
                            ElementId = model.ElementId,
                            UserId = permission.UserId
                        };

                        var result = await _permissionService.CreateOrUpdateAsync(dto);
                        if (!result)
                        {
                            allSuccess = false;
                            _logger.LogWarning("권한 저장 실패 - 요소: {ElementId}, 사용자: {UserId}", model.ElementId, permission.UserId);
                        }
                    }
                    else
                    {
                        // 권한이 비활성화된 사용자는 권한 삭제 (레코드 제거)
                        await _permissionService.DeleteAsync(model.ElementId, permission.UserId);
                    }
                }

                if (allSuccess)
                {
                    TempData["SuccessMessage"] = "UI 요소 권한이 성공적으로 저장되었습니다.";
                    // 성공 시 현재 페이지와 검색 조건을 유지하여 돌아감
                    return RedirectToAction("ManageUserPermissions", new
                    {
                        id = model.ElementId,
                        page = model.CurrentPage,
                        pageSize = model.PageSize,
                        searchTerm = model.SearchTerm,
                        sortField = model.SortField,
                        sortOrder = model.SortOrder
                    });
                }
                else
                {
                    // 일부 실패 시 경고 메시지 표시
                    ModelState.AddModelError(string.Empty, "일부 UI 요소 권한이 저장되지 않았습니다.");
                    return View("ManageUserPermissions", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 권한 저장 중 오류 발생 - 요소: {ElementId}: {Message}",
                    model.ElementId, ex.Message);

                // 오류 발생 시 붉은색 메시지 표시
                ModelState.AddModelError(string.Empty, "UI 요소 권한을 저장하는 중 오류가 발생했습니다: " + ex.Message);
                return View("ManageUserPermissions", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUserPermissions([FromBody] AssignUserPermissionsViewModel model)
        {
            if (model == null || model.ElementId <= 0 || model.UserIds == null || !model.UserIds.Any())
            {
                return Json(new { success = false, message = "잘못된 요청입니다." });
            }

            try
            {
                bool allSuccess = true;
                var failedUsers = new List<int>();

                foreach (var userId in model.UserIds)
                {
                    var dto = new UserElementPermissionBatchDto
                    {
                        UserId = userId,
                        ElementIds = new List<int> { model.ElementId }
                    };

                    var success = await _permissionService.AssignPermissionsBatchAsync(dto);
                    if (!success)
                    {
                        allSuccess = false;
                        failedUsers.Add(userId);
                    }
                }

                if (allSuccess)
                {
                    return Json(new { success = true, message = "모든 사용자에게 권한이 성공적으로 할당되었습니다." });
                }
                else if (failedUsers.Count == model.UserIds.Count)
                {
                    return Json(new { success = false, message = "모든 사용자에 대한 권한 할당이 실패했습니다." });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = $"일부 사용자({failedUsers.Count}명)에 대한 권한 할당이 실패했습니다.",
                        failedUsers = failedUsers
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 권한 할당 중 오류 발생 - 요소: {ElementId}, 사용자: {UserIds}",
                    model.ElementId, string.Join(", ", model.UserIds));
                return Json(new { success = false, message = "권한 할당 중 오류가 발생했습니다." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserPermission([FromBody] AssignUserPermissionsViewModel model)
        {
            if (model == null || model.ElementId <= 0 || model.UserIds == null || !model.UserIds.Any())
            {
                return Json(new { success = false, message = "잘못된 요청입니다." });
            }

            try
            {
                var userId = model.UserIds.First(); // 단일 사용자 권한 제거
                var result = await _permissionService.DeleteAsync(model.ElementId, userId);

                if (result)
                {
                    return Json(new { success = true, message = "사용자 권한이 성공적으로 제거되었습니다." });
                }
                else
                {
                    return Json(new { success = false, message = "사용자 권한 제거에 실패했습니다." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 권한 제거 중 오류 발생 - 요소: {ElementId}, 사용자: {UserId}",
                    model.ElementId, model.UserIds.First());
                return Json(new { success = false, message = "권한 제거 중 오류가 발생했습니다." });
            }
        }
    }
}