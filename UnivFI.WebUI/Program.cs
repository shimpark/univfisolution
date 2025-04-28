using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Security.Claims;
using System.Text;
using UnivFI.Application;
using UnivFI.Application.Interfaces.Services;
using UnivFI.Infrastructure;
using UnivFI.WebUI.Constants;
using UnivFI.WebUI.Mappings;
using UnivFI.WebUI.Models;

//===============================================================
// 1. 애플리케이션 설정 및 구성
//===============================================================

// 웹 애플리케이션 빌더 생성 - ASP.NET Core 응용 프로그램의 시작점
var builder = WebApplication.CreateBuilder(args);

// 개발 환경에서만 User Secrets를 로드 (개발용 비밀 설정 관리)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

//===============================================================
// 2. 애플리케이션 설정 값 로드 및 검증
//===============================================================

// 암호화 키를 설정 파일에서 로드 (중요: 실제 운영에서는 안전하게 관리해야 함)
var encryptionKey = builder.Configuration["Security:EncryptionKey"];
if (string.IsNullOrEmpty(encryptionKey))
{
    throw new InvalidOperationException("암호화 키가 설정되지 않았습니다. appsettings.json의 Security:EncryptionKey를 확인하세요.");
}

// JWT 인증용 시크릿 키 로드 (API 인증에 사용됨)
var jwtKey = builder.Configuration["JwtSettings:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT 키가 설정되지 않았습니다. appsettings.json의 JwtSettings:Key를 확인하세요.");
}

// CORS 허용 도메인 설정 로드 (API 접근 제한용)
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
if (allowedOrigins == null || allowedOrigins.Length == 0)
{
    // 설정이 없으면 개발용 도메인만 기본으로 허용
    allowedOrigins = new string[] { "https://localhost:7225", "http://localhost:5225" };

    // 운영 환경에서는 반드시 명시적으로 설정해야 함 (경고 로그)
    if (!builder.Environment.IsDevelopment())
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Program");
        logger.LogWarning("CORS 허용 도메인이 설정되지 않았습니다. 운영 환경에서는 appsettings.json의 AllowedOrigins 값을 반드시 설정하세요.");
    }
}

// JWT 설정 로드 (토큰 검증에 사용)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

//===============================================================
// 3. 서비스 등록 (DI 컨테이너 구성)
//===============================================================

// MVC 컨트롤러 및 뷰 서비스 등록 (웹 UI 구성)
builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        // 대소문자 구분 없는 쿼리스트링 바인딩 설정
        options.EnableEndpointRouting = false;
        options.ValueProviderFactories.Add(new QueryStringValueProviderFactory());
    });

// 대소문자 구분 없는 라우팅 설정
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// JSON 직렬화 설정에서 대소문자 구분 없애기
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 모델 바인딩 설정
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// 모델 메타데이터 설정 - 대소문자 구분 없는 바인딩
builder.Services.Configure<MvcOptions>(options =>
{
    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
});

// 암호화 서비스 등록 (싱글톤: 애플리케이션 전체에서 하나의 인스턴스 공유)
builder.Services.AddSingleton<UnivFI.Infrastructure.Security.IEncryptionService>(
    provider => new UnivFI.Infrastructure.Security.EncryptionService(encryptionKey));

//---------------------------------------------------------------
// 3.1 인증 및 권한 서비스 등록
//---------------------------------------------------------------

// 권한 정책 설정 - 모든 컨트롤러/액션에 인증 필요 (익명 접근 제한)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 인증 서비스 등록 - 쿠키와 JWT 모두 지원하는 복합 인증 설정
builder.Services.AddAuthentication(options =>
{
    // 기본 인증 방식 설정 (MixedAuth는 쿠키와 JWT를 경로에 따라 구분)
    options.DefaultAuthenticateScheme = "MixedAuth";
    options.DefaultChallengeScheme = "MixedAuth";
    options.DefaultScheme = "MixedAuth";
})
.AddCookie(options =>
{
    // 쿠키 인증 설정 (웹 UI 사용자용)
    options.Cookie.Name = AuthConstants.CookieName; // 쿠키 이름
    options.LoginPath = "/Account/Login";           // 로그인 페이지 경로
    options.LogoutPath = "/Account/Logout";         // 로그아웃 페이지 경로
    options.AccessDeniedPath = "/Account/AccessDenied"; // 접근 거부 페이지
    options.ExpireTimeSpan = TimeSpan.FromDays(1);  // 쿠키 유효 기간
    options.SlidingExpiration = true;               // 사용할 때마다 유효기간 갱신
})
.AddJwtBearer(options =>
{
    // JWT 인증 설정 (API 접근용)
    options.SaveToken = true;                       // 토큰 저장 활성화
    options.RequireHttpsMetadata = false;           // 개발 환경에서는 HTTPS 필요 없음
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // 토큰 검증 설정
        ValidateIssuer = true,                      // 발급자 검증
        ValidateAudience = true,                    // 대상자 검증
        ValidateLifetime = true,                    // 유효 기간 검증
        ValidateIssuerSigningKey = true,            // 서명 키 검증
        ValidIssuer = jwtSettings?.Issuer,          // 유효한 발급자
        ValidAudience = jwtSettings?.Audience,      // 유효한 대상자
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings?.Key ?? "defaultKey")), // 서명 키
        ClockSkew = TimeSpan.Zero,                  // 시간 오차 허용 안 함

        // 역할 관련 클레임 타입 지정
        RoleClaimType = ClaimTypes.Role,            // 역할 클레임 타입
        NameClaimType = ClaimTypes.Name             // 이름 클레임 타입
    };

    // JWT 토큰 검증 이벤트 핸들러
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            // 토큰 검증 성공 시 사용자 정보 로깅
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var user = context.Principal;

            // 사용자 ID와 역할 정보 로깅
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            logger.LogInformation("인증된 사용자 ID: {UserId}, 역할: {Roles}",
                userId, string.Join(", ", roles));

            return Task.CompletedTask;
        }
    };
})
// 복합 인증 스키마 추가 - 경로에 따라 쿠키 또는 JWT 인증 사용
.AddPolicyScheme("MixedAuth", "Cookie or JWT Auth", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // API 경로(/api/...)는 JWT 인증 사용, 그 외는 쿠키 인증 사용
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }
        return CookieAuthenticationDefaults.AuthenticationScheme;
    };
});

//---------------------------------------------------------------
// 3.2 CORS 정책 등록 (Cross-Origin Resource Sharing)
//---------------------------------------------------------------

builder.Services.AddCors(options =>
{
    // API 접근용 CORS 정책 - 특정 도메인만 허용 (API 보안)
    options.AddPolicy("ApiCorsPolicy",
        policy => policy
            .WithOrigins(allowedOrigins)            // 허용된 도메인만
            .AllowAnyMethod()                       // 모든 HTTP 메서드 허용
            .AllowAnyHeader()                       // 모든 HTTP 헤더 허용
            .AllowCredentials());                   // 인증 정보 포함 허용

    // 웹 UI용 CORS 정책 - 개발 편의를 위해 모든 도메인 허용
    options.AddPolicy("WebCorsPolicy",
        policy => policy
            .AllowAnyOrigin()                       // 모든 도메인 허용
            .AllowAnyMethod()                       // 모든 HTTP 메서드 허용
            .AllowAnyHeader());                     // 모든 HTTP 헤더 허용
});

//---------------------------------------------------------------
// 3.3 비즈니스 로직 서비스 등록
//---------------------------------------------------------------

// 애플리케이션 계층 서비스 등록 (비즈니스 로직)
builder.Services.AddApplication();

// 인프라스트럭처 계층 서비스 등록 (데이터 접근, 외부 서비스 등)
builder.Services.AddInfrastructure(builder.Configuration);

//---------------------------------------------------------------
// 3.4 Mapster 설정 (객체 간 매핑)
//---------------------------------------------------------------

// 웹 계층 매핑 설정 등록
builder.Services.AddWebMappings();

// HttpContextAccessor 서비스 등록 (뷰에서 HttpContext 접근용)
builder.Services.AddHttpContextAccessor();

//===============================================================
// 4. 애플리케이션 빌드 및 HTTP 파이프라인 구성
//===============================================================

var app = builder.Build(); // 웹 애플리케이션 빌드 (모든 서비스 등록 후)

//---------------------------------------------------------------
// 4.1 환경별 설정
//---------------------------------------------------------------

// 운영 환경에서의 오류 처리 및 보안 설정
if (!app.Environment.IsDevelopment())
{
    // 오류 발생 시 사용자 친화적인 오류 페이지로 리디렉션
    app.UseExceptionHandler("/Home/Error");

    // HTTP Strict Transport Security 활성화 (HTTPS 강제)
    app.UseHsts();
}

//---------------------------------------------------------------
// 4.2 미들웨어 파이프라인 구성 (순서 중요!)
//---------------------------------------------------------------

// 1. HTTP 요청을 HTTPS로 리디렉션 (보안 강화)
app.UseHttpsRedirection();

// 2. 정적 파일 제공 (CSS, JS, 이미지 등)
app.UseStaticFiles();

// 3. 라우팅 미들웨어 활성화 (URL 경로 분석)
app.UseRouting();

// 4. CORS 미들웨어 활성화 (교차 출처 요청 처리)
app.UseCors();

// 5. 인증 미들웨어 활성화 (사용자 신원 확인)
app.UseAuthentication();

// 6. 권한 부여 미들웨어 활성화 (권한 검사)
app.UseAuthorization();

//---------------------------------------------------------------
// 4.3 엔드포인트 및 라우트 구성
//---------------------------------------------------------------

// API 컨트롤러에 ApiCorsPolicy 적용 (특정 도메인만 접근 가능)
app.MapControllers().RequireCors("ApiCorsPolicy");

// MVC 컨트롤러에 기본 라우트 패턴 및 WebCorsPolicy 적용
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .RequireCors("WebCorsPolicy");

//===============================================================
// 5. 애플리케이션 초기화 및 실행
//===============================================================

// 관리자 계정 초기화 (애플리케이션 첫 실행 시 필요)
using (var scope = app.Services.CreateAsyncScope())
{
    var initService = scope.ServiceProvider.GetRequiredService<ISystemInitService>();
    await initService.InitializeAdminAccountAsync();
}

// 애플리케이션 실행 (HTTP 요청 수신 시작)

app.Run();