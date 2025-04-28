using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UnivFI.Application.Interfaces.Services;
using UnivFI.WebUI.Constants;
using UnivFI.WebUI.ViewModels;

namespace UnivFI.WebUI.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IUserService userService,
            IUserRoleService userRoleService,
            IRoleService roleService,
            ILogger<AccountController> logger)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _roleService = roleService;
            _logger = logger;

            // PasswordHasher에 로거 설정
            UnivFI.Application.Helpers.PasswordHasher.SetLogger(logger);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            var isAuthenticated = User?.Identity != null &&
                                 User.Identity.IsAuthenticated &&
                                 User.FindFirst(ClaimTypes.NameIdentifier) != null;

            if (isAuthenticated)
            {
                _logger.LogInformation("이미 인증된 사용자가 로그인 페이지에 접근 시도: {User}", User.Identity.Name);

                if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _logger.LogWarning("리다이렉트 루프 감지, 인증 쿠키 제거");

                    var model = new LoginViewModel
                    {
                        ReturnUrl = null
                    };
                    return View(model);
                }

                return RedirectToAction("Index", "Home");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("비인증 사용자의 로그인 페이지 접근");

            var loginModel = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };
            return View(loginModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // ViewModel 데이터 로깅 (비밀번호 제외)
            _logger.LogInformation(
                "로그인 시도 - UserName: {UserName}, RememberMe: {RememberMe}, ReturnUrl: {ReturnUrl}",
                model.UserName,
                model.RememberMe,
                model.ReturnUrl ?? "없음"
            );

            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "로그인 유효성 검사 실패 - UserName: {UserName}, ModelState Errors: {Errors}",
                    model.UserName,
                    string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                );
                return View(model);
            }

            try
            {
                var result = await _userService.AuthenticateAsync(model.UserName, model.Password);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "인증에 실패했습니다.");
                    return View(model);
                }

                var user = result.User;
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "사용자 정보를 찾을 수 없습니다.");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, model.UserName),
                    new Claim(AuthConstants.UserNameKey, model.UserName),
                    new Claim(AuthConstants.NameKey, user.Name),
                    new Claim(AuthConstants.EmailKey, user.Email)
                };

                var userRoles = await _userRoleService.GetUserRolesByUserIdAsync(user.Id);

                foreach (var userRole in userRoles)
                {
                    if (!string.IsNullOrEmpty(userRole.RoleName))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.RoleName));
                        _logger.LogInformation($"사용자 {model.UserName}에게 역할 추가: {userRole.RoleName}");
                    }
                }

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"사용자 {model.UserName}이(가) 성공적으로 로그인했습니다.");

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그인 처리 중 오류가 발생했습니다.");
                ModelState.AddModelError(string.Empty, "로그인 처리 중 오류가 발생했습니다. 다시 시도해 주세요.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // 사용자 ID 가져오기
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int id))
                {
                    // JWT 리프레시 토큰 무효화
                    await _userService.RevokeTokenAsync(id);
                }

                // 쿠키 인증 로그아웃
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // 명시적으로 UnivFI.Auth 쿠키 삭제
                Response.Cookies.Delete(Constants.AuthConstants.CookieName, new CookieOptions
                {
                    Path = "/"
                });

                // AJAX 요청인 경우 JSON 결과 반환
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그아웃 처리 중 오류 발생");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false });
                }

                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
            {
                return RedirectToAction("Login");
            }

            var userInfo = new
            {
                Id = id,
                Name = User.FindFirst(AuthConstants.NameKey)?.Value,
                Email = User.FindFirst(AuthConstants.EmailKey)?.Value,
                UserName = User.FindFirst(AuthConstants.UserNameKey)?.Value,
            };

            return View(userInfo);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                ModelState.AddModelError(string.Empty, "사용자 정보를 찾을 수 없습니다.");
                return View(model);
            }

            try
            {
                var success = await _userService.ChangePasswordAsync(
                    userId,
                    model.CurrentPassword,
                    model.NewPassword
                );

                if (success)
                {
                    TempData["SuccessMessage"] = "비밀번호가 성공적으로 변경되었습니다.";
                    return RedirectToAction("Profile");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "현재 비밀번호가 일치하지 않거나 비밀번호 변경 중 오류가 발생했습니다.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "비밀번호 변경 중 오류 발생");
                ModelState.AddModelError(string.Empty, "비밀번호 변경 중 오류가 발생했습니다.");
                return View(model);
            }
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}