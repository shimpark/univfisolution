using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using UnivFI.WebUI.Models;
using UnivFI.WebUI.Helpers;

namespace UnivFI.WebUI.Controllers;

[Authorize] // 기본적으로 인증이 필요
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous] // 모든 사용자가 접근 가능
    public IActionResult Index()
    {
        // 인증 헤더 확인 (디버깅용)
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            _logger.LogInformation("Authorization 헤더: {AuthHeader}", authHeader.ToString());
        }

        return View();
    }

    [AllowAnonymous] // 모든 사용자가 접근 가능
    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize]
    public IActionResult SecuredPage()
    {
        try
        {
            // 쿠키 인증에서 사용자 정보 가져오기
            var user = GetCurrentUser();

            // 디버깅용 로그
            _logger.LogInformation("인증된 사용자: {UserName}, 이름: {Name}, 이메일: {Email}",
                user.UserName,
                user.Name,
                user.Email);

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SecuredPage 액세스 중 오류 발생");
            return RedirectToAction("Error");
        }
    }

    // 관리자만 접근 가능
    [Authorize(Roles = "Administrators")]
    public IActionResult AdminPage()
    {
        var user = GetCurrentUser();
        ViewBag.UserInfo = user;
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private UserInfo GetCurrentUser()
    {
        var user = new UserInfo
        {
            Id = 0,
            UserName = "Unknown",
            Name = "Unknown",
            Email = "unknown@example.com",
            Roles = new List<string>()
        };

        // 쿠키 인증 클레임에서 사용자 정보 추출
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (idClaim != null && int.TryParse(idClaim.Value, out int id))
        {
            user.Id = id;

            // 사용자명 추출
            user.UserName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            // 이름 추출
            user.Name = User.FindFirst("name")?.Value ?? user.UserName;

            // 이메일 추출
            user.Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@example.com";

            // 역할 추출
            var roleClaims = User.FindAll(ClaimTypes.Role);
            user.Roles = roleClaims.Select(c => c.Value).Distinct().ToList();
        }

        return user;
    }
}
