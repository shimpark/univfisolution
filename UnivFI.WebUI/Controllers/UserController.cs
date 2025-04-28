using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Mapster;
using MapsterMapper;
using UnivFI.Application.Interfaces.Services;
using UnivFI.Application.DTOs;

namespace UnivFI.WebUI.Controllers
{
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            IMapper mapper,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            ICompositeViewEngine compositeViewEngine,
            ILogger<UserController> logger)
            : base(mapper, tempDataDictionaryFactory, compositeViewEngine, logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task<JsonResult> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Json(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 목록을 가져오는 중 오류가 발생했습니다.");
                return Json(new { success = false, message = "사용자 목록을 가져오는 중 오류가 발생했습니다." });
            }
        }
    }
}