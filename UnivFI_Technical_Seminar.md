# UnivFI 솔루션 기술 세미나

## 목차

1. [클린 아키텍처 개요](#1-클린-아키텍처-개요)
2. [프로젝트 구조 및 계층](#2-프로젝트-구조-및-계층)
3. [명명 규칙 및 코드 스타일](#3-명명-규칙-및-코드-스타일)
4. [데이터 액세스 패턴](#4-데이터-액세스-패턴)
5. [보안 구현](#5-보안-구현)
6. [Tailwind CSS를 활용한 UI 구현](#6-tailwind-css를-활용한-ui-구현)
7. [실제 구현 사례](#7-실제-구현-사례)
8. [참고 자료](#8-참고-자료)

## 1. 클린 아키텍처 개요

UnivFI 솔루션은 Robert C. Martin의 클린 아키텍처 원칙을 기반으로 구현되었습니다. 이는 시스템의 관심사를 분리하고 의존성 방향을 제어하여 테스트 용이성, 유지보수성 및 확장성이 높은 애플리케이션을 구축하기 위한 설계 원칙입니다.

### 1.1 핵심 원칙

#### 의존성 규칙

모든 의존성은 외부에서 내부로 향합니다. 내부 계층은 외부 계층에 대해 알지 못하며, 외부 계층이 내부 계층에 의존합니다.

```csharp
// 올바른 예: 애플리케이션 계층이 도메인 계층에 의존
namespace UnivFI.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository; // 도메인 계층의 인터페이스

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // 서비스 로직...
    }
}

// 잘못된 예: 도메인 계층이 인프라스트럭처 계층에 의존
// 이러한 의존성은 허용되지 않음
namespace UnivFI.Domain.Entities
{
    public class UserEntity
    {
        // SqlConnection과 같은 인프라스트럭처 의존성이 있으면 안 됨
        // private SqlConnection _connection; // 이렇게 하면 안 됨!
    }
}
```

#### 추상화 원칙

내부 계층은 인터페이스를 제공하고, 외부 계층은 이를 구현합니다. 이를 통해 내부 계층은 외부 계층의 구체적인 구현에 의존하지 않을 수 있습니다.

```csharp
// 도메인 계층: 인터페이스 정의
namespace UnivFI.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<UserEntity> GetByIdAsync(int id);
        Task<IEnumerable<UserEntity>> GetAllAsync();
        Task<int> CreateAsync(UserEntity user);
        Task<bool> UpdateAsync(UserEntity user);
        Task<bool> DeleteAsync(int id);
    }
}

// 인프라스트럭처 계층: 인터페이스 구현
namespace UnivFI.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnection _dbConnection;

        public UserRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<UserEntity> GetByIdAsync(int id)
        {
            // Dapper 구현...
            return await _dbConnection.QuerySingleOrDefaultAsync<UserEntity>(
                "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
        }

        // 나머지 메서드 구현...
    }
}
```

#### 엔티티 중심

비즈니스 엔티티와 규칙은 시스템의 중심에 있으며, 특정 프레임워크나 기술에 의존하지 않아야 합니다.

```csharp
// 순수한 도메인 엔티티 - 외부 의존성 없음
namespace UnivFI.Domain.Entities
{
    public class UserEntity
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 비즈니스 규칙 메서드
        public bool ValidatePassword(string password, IPasswordHasher hasher)
        {
            return hasher.VerifyPassword(password, PasswordHash, Salt);
        }

        public void SetPassword(string password, IPasswordHasher hasher)
        {
            var (hash, salt) = hasher.HashPassword(password);
            PasswordHash = hash;
            Salt = salt;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

### 1.2 아키텍처 계층

UnivFI 솔루션은 4개의 주요 계층으로 구성되어 있습니다:

#### 도메인 계층 (UnivFI.Domain)

- 핵심 비즈니스 모델과 규칙 정의
- 엔티티, 값 객체, 예외, 리포지토리 인터페이스 포함
- 외부 의존성 없음

```csharp
// UnivFI.Domain/Entities/MenuEntity.cs
namespace UnivFI.Domain.Entities
{
    public class MenuEntity
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string MenuKey { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public short? Levels { get; set; }
        public short? MenuOrder { get; set; }
        public bool? UseNewIcon { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 탐색 속성
        public virtual ICollection<MenuRoleEntity> MenuRoles { get; set; } = new List<MenuRoleEntity>();
    }
}
```

#### 애플리케이션 계층 (UnivFI.Application)

- 비즈니스 로직 구현 및 유스케이스 조정
- 서비스, DTO, 매핑 로직 포함
- 도메인 계층에만 의존

```csharp
// UnivFI.Application/Services/MenuService.cs
namespace UnivFI.Application.Services
{
    public class MenuService : IMenuService
    {
        private readonly IMenuRepository _menuRepository;
        private readonly IMapper _mapper;

        public MenuService(IMenuRepository menuRepository, IMapper mapper)
        {
            _menuRepository = menuRepository;
            _mapper = mapper;
        }

        public async Task<MenuDto> GetByIdAsync(int id)
        {
            var menu = await _menuRepository.GetByIdAsync(id);
            return _mapper.Map<MenuDto>(menu);
        }

        public async Task<IEnumerable<MenuDto>> GetAllAsync()
        {
            var menus = await _menuRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<MenuDto>>(menus);
        }

        public async Task<int> CreateAsync(CreateMenuDto createMenuDto)
        {
            var menuEntity = _mapper.Map<MenuEntity>(createMenuDto);
            menuEntity.CreatedAt = DateTime.UtcNow;

            return await _menuRepository.CreateAsync(menuEntity);
        }

        // 나머지 메서드 구현...
    }
}
```

#### 인프라스트럭처 계층 (UnivFI.Infrastructure)

- 데이터 액세스 및 외부 서비스 통합
- 리포지토리 구현, 인증 및 보안 구현
- 도메인 및 애플리케이션 계층에 의존

```csharp
// UnivFI.Infrastructure/Repositories/MenuRepository.cs
namespace UnivFI.Infrastructure.Repositories
{
    public class MenuRepository : IMenuRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<MenuRepository> _logger;

        public MenuRepository(IDbConnection dbConnection, ILogger<MenuRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<MenuEntity> GetByIdAsync(int id)
        {
            try
            {
                var sql = "SELECT * FROM Menus WHERE Id = @Id";
                _logger.LogDebug("Executing SQL: {Sql}, Parameters: {@Parameters}", sql, new { Id = id });

                return await _dbConnection.QuerySingleOrDefaultAsync<MenuEntity>(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu with ID {Id}", id);
                throw;
            }
        }

        // 나머지 메서드 구현...
    }
}
```

#### 프레젠테이션 계층 (UnivFI.WebUI)

- 사용자 인터페이스 및 API 엔드포인트
- 컨트롤러, 뷰, 뷰모델 포함
- 애플리케이션 계층에만 의존

```csharp
// UnivFI.WebUI/Areas/Admin/Controllers/MenuController.cs
namespace UnivFI.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrators")]
    public class MenuController : Controller
    {
        private readonly IMenuService _menuService;
        private readonly IMapper _mapper;

        public MenuController(IMenuService menuService, IMapper mapper)
        {
            _menuService = menuService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string searchTerm = null)
        {
            var menus = await _menuService.GetPagedListAsync(page, pageSize, searchTerm);
            var totalItems = await _menuService.CountAsync(searchTerm);

            var viewModel = new MenuListViewModel
            {
                Menus = _mapper.Map<IEnumerable<MenuViewModel>>(menus),
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                SearchTerm = searchTerm
            };

            return View(viewModel);
        }

        // 나머지 액션 구현...
    }
}
```

### 1.3 계층 간 통신

계층 간 통신은 주로 의존성 주입(DI)과 인터페이스를 통해 이루어집니다.

```csharp
// Program.cs에서 서비스 등록
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        //Mapster 활용
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IRoleService, RoleService>();
        // 기타 서비스...

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 데이터베이스 연결 설정
        services.AddScoped<IDbConnection>(provider =>
            new SqlConnection(configuration.GetConnectionString("DefaultConnection")));

        // 리포지토리 등록
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // 보안 서비스 등록
        services.AddSingleton<IEncryptionService>(provider =>
            new EncryptionService(configuration["Security:EncryptionKey"]));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
```

## 2. 프로젝트 구조 및 계층

UnivFI 솔루션은 다중 프로젝트 구조로 구성되어 있으며, 각 계층은 별도의 프로젝트로 분리되어 있습니다.

### 2.1 솔루션 구조

```
UnivFI.Solution/
├── UnivFI.Domain/            # 핵심 비즈니스 모델, 인터페이스 정의
├── UnivFI.Application/       # 비즈니스 로직, 유스케이스, 서비스
├── UnivFI.Infrastructure/    # 데이터 액세스, 외부 서비스 통합
├── UnivFI.WebUI/             # 사용자 인터페이스 (MVC)
└── UnivFI.SqlServer/         # 데이터베이스 스키마, 프로시저 정의
```

### 2.2 프로젝트별 세부 구조

#### 2.2.1 UnivFI.Domain

```
UnivFI.Domain/
├── Entities/                # 도메인 엔티티 클래스
│   ├── UserEntity.cs
│   ├── RoleEntity.cs
│   ├── MenuEntity.cs
│   ├── UserRoleEntity.cs
│   ├── MenuRoleEntity.cs
│   └── FileAttachEntity.cs
├── Interfaces/              # 리포지토리 및 서비스 인터페이스
│   ├── Repositories/
│   │   ├── IUserRepository.cs
│   │   ├── IRoleRepository.cs
│   │   └── IMenuRepository.cs
│   └── Services/
│       ├── IEncryptionService.cs
│       └── IPasswordHasher.cs
└── Exceptions/              # 도메인 특화 예외 클래스
    ├── EntityNotFoundException.cs
    └── DomainValidationException.cs
```

#### 2.2.2 UnivFI.Application

```
UnivFI.Application/
├── Services/                # 서비스 구현 클래스
│   ├── UserService.cs
│   ├── RoleService.cs
│   └── MenuService.cs
├── DTOs/                    # 데이터 전송 객체
│   ├── UserDto.cs
│   ├── RoleDto.cs
│   └── MenuDto.cs
├── Interfaces/              # 서비스 인터페이스
│   ├── IUserService.cs
│   ├── IRoleService.cs
│   └── IMenuService.cs
└── Mappings/                # 객체 매핑 프로필
    ├── MappingProfile.cs
    └── DtoMappingExtensions.cs
```

#### 2.2.3 UnivFI.Infrastructure

```
UnivFI.Infrastructure/
├── Data/                    # 데이터 액세스 관련 클래스
│   ├── ConnectionFactory.cs
│   └── EntityConfigurations/
├── Repositories/            # 리포지토리 구현 클래스
│   ├── BaseRepository.cs
│   ├── UserRepository.cs
│   ├── RoleRepository.cs
│   └── MenuRepository.cs
├── Security/                # 보안 구현 클래스
│   ├── EncryptionService.cs
│   └── PasswordHasher.cs
└── Extensions/              # 확장 메서드
    └── ServiceCollectionExtensions.cs
```

#### 2.2.4 UnivFI.WebUI

```
UnivFI.WebUI/
├── Areas/                   # 영역별 컨트롤러 및 뷰
│   └── Admin/               # 관리자 영역
│       ├── Controllers/
│       │   ├── UserController.cs
│       │   ├── RoleController.cs
│       │   └── MenuController.cs
│       └── Views/
│           ├── User/
│           ├── Role/
│           └── Menu/
├── Controllers/             # 일반 컨트롤러
│   ├── HomeController.cs
│   └── AccountController.cs
├── Models/                  # 뷰 모델
│   ├── UserViewModel.cs
│   ├── RoleViewModel.cs
│   └── MenuViewModel.cs
├── Views/                   # 뷰 템플릿
│   ├── Home/
│   ├── Account/
│   └── Shared/
└── wwwroot/                 # 정적 리소스
    ├── css/
    ├── js/
    └── lib/
```

### 2.3 프로젝트 상호작용 예시

다음은 메뉴 아이템을 조회하는 과정에서 각 계층이 어떻게 상호작용하는지 보여주는 예시입니다:

1. **WebUI 계층 (Controller)**

```csharp
// UnivFI.WebUI/Areas/Admin/Controllers/MenuController.cs
[Area("Admin")]
[Authorize(Roles = "Administrators")]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public async Task<IActionResult> Detail(int id)
    {
        try
        {
            var menuDto = await _menuService.GetByIdAsync(id);
            if (menuDto == null)
                return NotFound();

            var viewModel = menuDto.Adapt<MenuViewModel>();
            return View(viewModel);
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }
}
```

2. **Application 계층 (Service)**

```csharp
// UnivFI.Application/Services/MenuService.cs
public class MenuService : IMenuService
{
    private readonly IMenuRepository _menuRepository;
    private readonly IMapper _mapper;

    public MenuService(IMenuRepository menuRepository, IMapper mapper)
    {
        _menuRepository = menuRepository;
        _mapper = mapper;
    }

    public async Task<MenuDto> GetByIdAsync(int id)
    {
        var menuEntity = await _menuRepository.GetByIdAsync(id);
        if (menuEntity == null)
            throw new EntityNotFoundException($"Menu with ID {id} not found");

        return _mapper.Map<MenuDto>(menuEntity);
    }
}
```

3. **Infrastructure 계층 (Repository)**

```csharp
// UnivFI.Infrastructure/Repositories/MenuRepository.cs
public class MenuRepository : BaseRepository<MenuEntity, int>, IMenuRepository
{
    public MenuRepository(IDbConnection dbConnection, ILogger<MenuRepository> logger)
        : base(dbConnection, logger, "Menus")
    {
    }

    public override async Task<MenuEntity> GetByIdAsync(int id)
    {
        try
        {
            var sql = @"
                SELECT m.*, r.Id as RoleId, r.RoleName, r.Description as RoleDescription
                FROM Menus m
                LEFT JOIN MenuRoles mr ON m.Id = mr.MenuId
                LEFT JOIN Roles r ON mr.RoleId = r.Id
                WHERE m.Id = @Id";

            LogSqlQuery(sql, new { Id = id });

            // 결과와 관계 맵핑을 위한 Dictionary
            var menuDict = new Dictionary<int, MenuEntity>();

            // Dapper의 다중 맵핑 기능 사용
            await _dbConnection.QueryAsync<MenuEntity, RoleEntity, MenuEntity>(
                sql,
                (menu, role) => {
                    // Dictionary에 없으면 메뉴 추가
                    if (!menuDict.TryGetValue(menu.Id, out var menuEntry))
                    {
                        menuEntry = menu;
                        menuEntry.MenuRoles = new List<MenuRoleEntity>();
                        menuDict.Add(menu.Id, menuEntry);
                    }

                    // 역할이 있으면 메뉴 역할 관계 추가
                    if (role != null)
                    {
                        menuEntry.MenuRoles.Add(new MenuRoleEntity
                        {
                            MenuId = menu.Id,
                            RoleId = role.Id,
                            Role = role
                        });
                    }

                    return menuEntry;
                },
                new { Id = id },
                splitOn: "RoleId"
            );

            return menuDict.Values.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving menu with ID {Id}", id);
            throw;
        }
    }

    // 나머지 메서드 구현...
}
```

4. **Domain 계층 (Entity 및 Interface)**

```csharp
// UnivFI.Domain/Entities/MenuEntity.cs
[Table("Menus")]
public class MenuEntity
{
    [Key]
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string MenuKey { get; set; }
    public string Url { get; set; }
    public string Title { get; set; }
    public short? Levels { get; set; }
    public short? MenuOrder { get; set; }
    public bool? UseNewIcon { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [Write(false)]
    public virtual ICollection<MenuRoleEntity> MenuRoles { get; set; } = new List<MenuRoleEntity>();
}

// UnivFI.Domain/Interfaces/Repositories/IMenuRepository.cs
public interface IMenuRepository : IBaseRepository<MenuEntity, int>
{
    Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId);
    Task<IEnumerable<MenuEntity>> GetMenuTreeAsync();
    Task<bool> AssignRolesToMenuAsync(int menuId, IEnumerable<int> roleIds);
}
```

## 3. 명명 규칙 및 코드 스타일

UnivFI 프로젝트는 일관된 코드 스타일과 명명 규칙을 적용하여 코드의 가독성과 유지보수성을 높이고 있습니다.

### 3.1 일반 명명 규칙

- **영문 작성**: 모든 이름은 영어로 작성합니다.
- **의미 있는 이름**: 약어 사용을 최소화하고 의미가 명확한 이름을 사용합니다.
- **한국어 발음 금지**: 한글 발음을 그대로 영어로 작성하지 않습니다.
- **일관성**: 유사한 요소에는 일관된 명명 패턴을 사용합니다.

### 3.2 파일 및 클래스 명명

| 파일 유형   | 명명 패턴           | 예시                |
| ----------- | ------------------- | ------------------- |
| 엔티티      | [Name]Entity.cs     | UserEntity.cs       |
| 인터페이스  | I[Name].cs          | IUserRepository.cs  |
| DTO         | [Name]Dto.cs        | UserDto.cs          |
| 서비스      | [Name]Service.cs    | UserService.cs      |
| 리포지토리  | [Name]Repository.cs | UserRepository.cs   |
| 컨트롤러    | [Name]Controller.cs | UserController.cs   |
| 뷰모델      | [Name]ViewModel.cs  | UserViewModel.cs    |
| 확장 메서드 | [Name]Extensions.cs | StringExtensions.cs |
| 헬퍼 클래스 | [Name]Helper.cs     | ValidationHelper.cs |

### 3.3 메서드 명명

#### 일반 메서드

```csharp
// PascalCase 사용, 동사 또는 동사구로 시작
public void ProcessData() { }
public bool ValidateUser(User user) { }
public string FormatName(string firstName, string lastName) { }
```

#### 비동기 메서드

```csharp
// 'Async' 접미사 사용
public async Task<User> GetUserByIdAsync(int id) { }
public async Task SaveChangesAsync() { }
public async Task<bool> ValidateCredentialsAsync(string username, string password) { }
```

#### CRUD 작업 메서드

```csharp
// 생성 (Create)
public async Task<int> CreateAsync(UserEntity entity) { }

// 조회 (Read)
public async Task<UserEntity> GetByIdAsync(int id) { }
public async Task<IEnumerable<UserEntity>> GetListAsync() { }

// 수정 (Update)
public async Task<bool> UpdateAsync(UserEntity entity) { }

// 삭제 (Delete)
public async Task<bool> DeleteAsync(int id) { }
```

### 3.4 변수 및 매개변수 명명

```csharp
// 지역 변수는 camelCase 사용
var userId = 1;
string fullName = "John Doe";
bool isValid = true;

// 매개변수도 camelCase 사용
public void UpdateUser(int userId, string userName, bool isActive) { }

// 클래스의 private 필드는 '_' 접두사와 camelCase 사용
private readonly IUserRepository _userRepository;
private string _connectionString;
private User _currentUser;

// 모든 public 속성은 PascalCase 사용
public int Id { get; set; }
public string UserName { get; set; }
public DateTime CreatedAt { get; set; }
```

### 3.5 상수 및 열거형 명명

```csharp
// 상수는 대문자와 밑줄 사용
private const string CONNECTION_STRING = "...";
public const int MAX_RETRY_COUNT = 3;

// 열거형 타입과 값은 PascalCase 사용
public enum UserStatus
{
    Active,
    Inactive,
    Suspended,
    Deleted
}
```

### 3.6 코드 스타일 예시

다음은 UnivFI 프로젝트의 코드 스타일을 보여주는 예시입니다:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;
using UnivFI.Domain.Exceptions;

namespace UnivFI.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private const int PASSWORD_MIN_LENGTH = 8;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            var userEntity = await _userRepository.GetByIdAsync(id);
            if (userEntity == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                throw new EntityNotFoundException($"User with ID {id} not found");
            }

            return MapToDto(userEntity);
        }

        public async Task<IEnumerable<UserDto>> GetActiveUsersAsync()
        {
            try
            {
                var users = await _userRepository.GetActiveUsersAsync();
                return MapToDtoList(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active users");
                throw;
            }
        }

        public async Task<int> CreateUserAsync(CreateUserDto createUserDto)
        {
            ValidateUserData(createUserDto);

            var userEntity = new UserEntity
            {
                UserName = createUserDto.UserName,
                Email = createUserDto.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            userEntity.SetPassword(createUserDto.Password, _passwordHasher);

            var userId = await _userRepository.CreateAsync(userEntity);
            _logger.LogInformation("Created new user with ID {UserId}", userId);

            return userId;
        }

        private void ValidateUserData(CreateUserDto user)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
                throw new DomainValidationException("Username cannot be empty");

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new DomainValidationException("Email cannot be empty");

            if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < PASSWORD_MIN_LENGTH)
                throw new DomainValidationException($"Password must be at least {PASSWORD_MIN_LENGTH} characters long");
        }

        private UserDto MapToDto(UserEntity entity)
        {
            return new UserDto
            {
                Id = entity.Id,
                UserName = entity.UserName,
                Email = entity.Email,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private IEnumerable<UserDto> MapToDtoList(IEnumerable<UserEntity> entities)
        {
            var dtos = new List<UserDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapToDto(entity));
            }
            return dtos;
        }
    }
}
```

## 4. 데이터 액세스 패턴

UnivFI 프로젝트에서는 Dapper와 Dapper.Contrib를 사용하여 데이터 액세스 계층을 구현하고 있습니다. 이를 통해 SQL의 유연성과 ORM의 편의성을 모두 활용할 수 있습니다.

### 4.1 Dapper 기본 사용

Dapper는 마이크로 ORM으로, SQL 쿼리를 직접 작성하면서도 결과를 객체로 매핑하는 기능을 제공합니다.

```csharp
// 기본적인 Dapper 사용 예시
public async Task<UserEntity> GetByIdAsync(int id)
{
    using var connection = CreateConnection();

    var sql = "SELECT * FROM Users WHERE Id = @Id";

    LogSqlQuery(sql, new { Id = id });

    return await connection.QuerySingleOrDefaultAsync<UserEntity>(sql, new { Id = id });
}
```

### 4.2 BaseRepository 패턴

UnivFI 프로젝트에서는 `BaseRepository<TEntity, TKey>` 클래스를 통해 공통 CRUD 작업을 추상화하고 일관된 데이터 액세스 패턴을 적용합니다.

```csharp
// BaseRepository.cs
public abstract class BaseRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey> where TEntity : class
{
    protected readonly IDbConnection _dbConnection;
    protected readonly ILogger _logger;
    protected readonly string _tableName;

    protected BaseRepository(IDbConnection dbConnection, ILogger logger, string tableName)
    {
        _dbConnection = dbConnection;
        _logger = logger;
        _tableName = tableName;
    }

    protected void LogSqlQuery(string sql, object parameters = null)
    {
        _logger.LogDebug("Executing SQL: {Sql}", sql);
        if (parameters != null)
        {
            _logger.LogDebug("Parameters: {@Parameters}", parameters);
        }
    }

    public virtual async Task<TEntity> GetByIdAsync(TKey id)
    {
        using var connection = _dbConnection;
        var sql = $"SELECT * FROM {_tableName} WHERE Id = @Id";
        LogSqlQuery(sql, new { Id = id });

        return await connection.QuerySingleOrDefaultAsync<TEntity>(sql, new { Id = id });
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        using var connection = _dbConnection;
        var sql = $"SELECT * FROM {_tableName}";
        LogSqlQuery(sql);

        return await connection.QueryAsync<TEntity>(sql);
    }

    public virtual async Task<TKey> CreateAsync(TEntity entity)
    {
        using var connection = _dbConnection;
        LogSqlQuery($"INSERT INTO {_tableName}", entity);

        return (TKey)await connection.InsertAsync(entity);
    }

    public virtual async Task<bool> UpdateAsync(TEntity entity)
    {
        using var connection = _dbConnection;
        LogSqlQuery($"UPDATE {_tableName}", entity);

        return await connection.UpdateAsync(entity);
    }

    public virtual async Task<bool> DeleteAsync(TKey id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        using var connection = _dbConnection;
        LogSqlQuery($"DELETE FROM {_tableName} WHERE Id = @Id", new { Id = id });

        return await connection.DeleteAsync(entity);
    }

    // 페이징, 검색 등의 메서드 추가...
}
```

### 4.3 고급 쿼리 예시

#### 4.3.1 복합 쿼리 및 다중 결과 매핑

여러 테이블을 조인하여 관계 데이터를 포함한 결과를 가져오는 예시입니다.

```csharp
public async Task<UserEntity> GetUserWithRolesAsync(int userId)
{
    using var connection = _dbConnection;

    var sql = @"
        SELECT u.*, r.Id as RoleId, r.RoleName, r.Description
        FROM Users u
        LEFT JOIN UserRoles ur ON u.Id = ur.UserId
        LEFT JOIN Roles r ON ur.RoleId = r.Id
        WHERE u.Id = @UserId";

    LogSqlQuery(sql, new { UserId = userId });

    var userDict = new Dictionary<int, UserEntity>();

    await connection.QueryAsync<UserEntity, RoleEntity, UserEntity>(
        sql,
        (user, role) => {
            if (!userDict.TryGetValue(user.Id, out var userEntry))
            {
                userEntry = user;
                userEntry.UserRoles = new List<UserRoleEntity>();
                userDict.Add(user.Id, userEntry);
            }

            if (role != null)
            {
                userEntry.UserRoles.Add(new UserRoleEntity
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    Role = role
                });
            }

            return userEntry;
        },
        new { UserId = userId },
        splitOn: "RoleId"
    );

    return userDict.Values.FirstOrDefault();
}
```

#### 4.3.2 페이징 및 정렬

페이징과 정렬을 적용한 쿼리 예시입니다.

```csharp
public async Task<IEnumerable<MenuEntity>> GetPagedListAsync(
    int page, int pageSize, string searchTerm = null, string sortField = "MenuOrder", bool ascending = true)
{
    using var connection = _dbConnection;

    // 검색 조건 생성
    var whereClause = string.IsNullOrEmpty(searchTerm)
        ? ""
        : "WHERE (MenuKey LIKE @SearchTerm OR Title LIKE @SearchTerm OR Url LIKE @SearchTerm)";

    // 정렬 조건 생성 (SQL 인젝션 방지를 위한 화이트리스트 체크)
    var allowedSortFields = new[] { "Id", "MenuKey", "Title", "MenuOrder" };
    sortField = allowedSortFields.Contains(sortField) ? sortField : "MenuOrder";
    var orderByClause = $"ORDER BY {sortField} {(ascending ? "ASC" : "DESC")}";

    var sql = $@"
        SELECT *
        FROM {_tableName}
        {whereClause}
        {orderByClause}
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

    var parameters = new {
        SearchTerm = searchTerm == null ? null : $"%{searchTerm}%",
        Offset = (page - 1) * pageSize,
        PageSize = pageSize
    };

    LogSqlQuery(sql, parameters);

    return await connection.QueryAsync<MenuEntity>(sql, parameters);
}
```

#### 4.3.3 트랜잭션 처리

여러 작업을 하나의 트랜잭션으로 묶어 데이터 일관성을 보장하는 예시입니다.

```csharp
public async Task<bool> AssignRolesToMenuAsync(int menuId, IEnumerable<int> roleIds)
{
    using var connection = _dbConnection;
    connection.Open();

    using var transaction = connection.BeginTransaction();

    try
    {
        // 기존 메뉴-역할 매핑 삭제
        var deleteQuery = "DELETE FROM MenuRoles WHERE MenuId = @MenuId";
        LogSqlQuery(deleteQuery, new { MenuId = menuId });

        await connection.ExecuteAsync(deleteQuery, new { MenuId = menuId }, transaction);

        // 새 메뉴-역할 매핑 추가
        foreach (var roleId in roleIds)
        {
            var insertQuery = "INSERT INTO MenuRoles (MenuId, RoleId) VALUES (@MenuId, @RoleId)";
            LogSqlQuery(insertQuery, new { MenuId = menuId, RoleId = roleId });

            await connection.ExecuteAsync(
                insertQuery,
                new { MenuId = menuId, RoleId = roleId },
                transaction);
        }

        // 트랜잭션 커밋
        transaction.Commit();
        return true;
    }
    catch (Exception ex)
    {
        // 오류 발생 시 롤백
        transaction.Rollback();
        _logger.LogError(ex, "Error assigning roles to menu {MenuId}", menuId);
        return false;
    }
}
```

### 4.4 저장 프로시저 활용

UnivFI 프로젝트에서는 복잡한 비즈니스 로직이나 대량 데이터 처리가 필요한 경우 저장 프로시저를 활용합니다.

```csharp
// 저장 프로시저 호출 예시
public async Task<IEnumerable<MenuTreeItem>> GetMenuTreeAsync()
{
    using var connection = _dbConnection;

    var sql = "EXEC usp_Menu_GetMenuTree";
    LogSqlQuery(sql);

    return await connection.QueryAsync<MenuTreeItem>(sql);
}

// 매개변수가 있는 저장 프로시저 호출
public async Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId)
{
    using var connection = _dbConnection;

    var sql = "EXEC usp_Menu_GetMenusByRoleId @RoleId";
    LogSqlQuery(sql, new { RoleId = roleId });

    return await connection.QueryAsync<MenuEntity>(sql, new { RoleId = roleId });
}
```

### 4.5 Dapper.Contrib를 활용한 단순화된 CRUD

단순한 CRUD 작업에는 Dapper.Contrib를 사용하여 코드를 간결하게 유지합니다.

```csharp
// Dapper.Contrib 속성
[Table("Roles")]
public class RoleEntity
{
    [Key] // 자동 증가 기본 키
    public int Id { get; set; }

    public string RoleName { get; set; }
    public string Description { get; set; }

    [Computed] // 계산된 속성 (INSERT/UPDATE 시 무시)
    public bool IsSystem => Id <= 3; // 시스템 기본 역할

    [Write(false)] // 데이터베이스에 저장하지 않는 탐색 속성
    public ICollection<UserRoleEntity> UserRoles { get; set; }
}

// Dapper.Contrib 메서드 사용
public async Task<int> CreateAsync(RoleEntity entity)
{
    using var connection = _dbConnection;
    return await connection.InsertAsync(entity);
}

public async Task<bool> UpdateAsync(RoleEntity entity)
{
    using var connection = _dbConnection;
    return await connection.UpdateAsync(entity);
}

public async Task<bool> DeleteAsync(int id)
{
    using var connection = _dbConnection;
    var entity = await GetByIdAsync(id);
    if (entity == null)
        return false;

    return await connection.DeleteAsync(entity);
}
```

## 5. 보안 구현

UnivFI 프로젝트는 여러 계층에서의 보안 구현을 통해 안전한 애플리케이션을 제공합니다.

### 5.1 인증 시스템

UnivFI는 쿠키 기반 인증과 JWT 토큰 인증을 함께 사용하는 복합 인증 시스템을 구현하고 있습니다.

```csharp
// Program.cs에서 복합 인증 설정
services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = "MixedAuth";
    options.DefaultChallengeScheme = "MixedAuth";
})
.AddCookie(options => {
    // 쿠키 인증 설정 (웹 UI 사용자용)
    options.Cookie.Name = "UnivFI.Auth";
    options.Cookie.HttpOnly = true;                 // JavaScript 접근 방지
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS 통신 필수
    options.Cookie.SameSite = SameSiteMode.Strict;  // CSRF 방지
    options.LoginPath = "/Account/Login";
})
.AddJwtBearer(options => {
    // JWT 인증 설정 (API 접근용)
    var key = Encoding.UTF8.GetBytes(jwtSettings?.Key ?? "defaultKey");
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,            // 서명 키 검증
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,                      // 발급자 검증
        ValidateAudience = true,                    // 대상자 검증
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        ClockSkew = TimeSpan.Zero                   // 시간 오차 허용 안 함
    };
})
.AddPolicyScheme("MixedAuth", "Cookie or JWT Auth", options => {
    options.ForwardDefaultSelector = context => {
        // API 경로(/api/...)는 JWT 인증 사용, 그 외는 쿠키 인증 사용
        if (context.Request.Path.StartsWithSegments("/api"))
            return JwtBearerDefaults.AuthenticationScheme;
        return CookieAuthenticationDefaults.AuthenticationScheme;
    };
});
```

### 5.2 권한 부여 (인가)

역할 기반 접근 제어(RBAC)를 통해 메뉴와 기능에 대한 세밀한 권한 제어를 구현하고 있습니다.

```csharp
// 컨트롤러 수준 권한 부여
[Authorize(Roles = "Administrators")]
public class AdminController : Controller
{
    // 관리자 전용 기능
}

// 액션 수준 권한 부여
[Authorize(Roles = "Administrators,Managers")]
public IActionResult ManageUsers()
{
    // 관리자와 매니저가 접근 가능한 기능
}

// 여러 역할 조합 정책
services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("SuperAdministrators"));

    options.AddPolicy("ContentManagement", policy =>
        policy.RequireRole("Administrators", "ContentEditors"));

    // 모든 인증된 사용자에게 기본 정책 적용
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

### 5.3 비밀번호 해시

사용자 비밀번호는 PBKDF2 알고리즘을 사용하여 안전하게 해시하여 저장합니다.

```csharp
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;  // 128 bit
    private const int KeySize = 32;   // 256 bit
    private const int Iterations = 10000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public (string Hash, string Salt) HashPassword(string password)
    {
        // 랜덤 솔트 생성
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        string saltStr = Convert.ToBase64String(salt);

        // 비밀번호 해싱
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);
        string hashStr = Convert.ToBase64String(hash);

        return (hashStr, saltStr);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] hashBytes = Convert.FromBase64String(hash);

        // 입력된 비밀번호 해싱
        byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            Iterations,
            HashAlgorithm,
            KeySize);

        // 타이밍 공격 방지를 위한 일정 시간 비교
        return CryptographicOperations.FixedTimeEquals(hashBytes, testHash);
    }
}
```

### 5.4 데이터 암호화

민감한 데이터는 AES-256 알고리즘으로 암호화하여 저장합니다.

```csharp
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(string encryptionKey)
    {
        // 키 생성 (PBKDF2 사용)
        using (var deriveBytes = new Rfc2898DeriveBytes(
            encryptionKey,
            Encoding.UTF8.GetBytes("UnivFISalt"),
            10000))
        {
            _key = deriveBytes.GetBytes(32);  // AES-256
            _iv = deriveBytes.GetBytes(16);   // AES 블록 크기
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(
                    msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var msDecrypt = new MemoryStream(cipherBytes))
                using (var csDecrypt = new CryptoStream(
                    msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
        catch (FormatException)
        {
            // Base64 형식이 아닌 경우 원본 반환
            return cipherText;
        }
        catch (CryptographicException)
        {
            // 복호화 실패 시 원본 반환
            return cipherText;
        }
    }
}
```

### 5.5 CSRF 방지

CSRF 공격을 방지하기 위한 토큰 검증을 구현합니다.

```csharp
// 로그인 폼 뷰에서 CSRF 토큰 포함
@model LoginViewModel
<form asp-action="Login" asp-controller="Account" method="post">
    @Html.AntiForgeryToken()
    <!-- 폼 내용 -->
    <button type="submit">로그인</button>
</form>

// 컨트롤러에서 CSRF 토큰 검증
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model)
{
    // 로그인 로직...
}
```

### 5.6 웹 보안 헤더

다양한 웹 보안 헤더를 설정하여 보안 강화:

```csharp
// Program.cs에서 보안 헤더 설정
// HTTPS 강제 리디렉션
app.UseHttpsRedirection();

// HTTP Strict Transport Security 활성화
app.UseHsts();

// XSS 방지를 위한 Content Security Policy
app.Use(async (context, next) =>
{
    context.Response.Headers.Add(
        "Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' https://cdnjs.cloudflare.com 'unsafe-inline'; " +
        "style-src 'self' https://fonts.googleapis.com 'unsafe-inline'; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "frame-ancestors 'none'");

    // X-Content-Type-Options 헤더
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

    // X-Frame-Options 헤더 (클릭재킹 방지)
    context.Response.Headers.Add("X-Frame-Options", "DENY");

    // X-XSS-Protection 헤더
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

    await next();
});
```

## 6. Tailwind CSS를 활용한 UI 구현

UnivFI 프로젝트는 Tailwind CSS를 활용하여 반응형 사용자 인터페이스를 구현합니다.

### 6.1 기본 레이아웃

레이아웃은 좌측 사이드바와 우측 콘텐츠 영역으로 구성됩니다.

```html
<!-- _Layout.cshtml -->
<div class="flex h-screen bg-gray-100">
  <!-- 사이드바 -->
  <div
    id="sidebar"
    class="fixed inset-y-0 left-0 z-10 w-64 transition-all duration-300 transform bg-primary-700 lg:translate-x-0 lg:static lg:inset-0"
  >
    <!-- 로고 및 메뉴 내용 -->
  </div>

  <!-- 메인 콘텐츠 -->
  <div
    class="flex flex-col flex-1 w-full transition-all duration-300"
    id="mainContent"
  >
    <!-- 헤더 -->
    <header class="z-10 py-4 bg-white shadow-md">
      <!-- 헤더 내용 -->
    </header>

    <!-- 페이지 콘텐츠 -->
    <main class="flex-1 overflow-y-auto p-4">@RenderBody()</main>

    <!-- 푸터 -->
    <footer class="py-3 bg-white">
      <!-- 푸터 내용 -->
    </footer>
  </div>
</div>
```

### 6.2 반응형 디자인

Tailwind CSS의 반응형 클래스를 활용하여 다양한 디바이스에 최적화된 UI를 제공합니다.

```html
<!-- 모바일 우선 접근 방식 -->
<div class="w-full md:w-1/2 lg:w-1/3 xl:w-1/4">
  <!-- 콘텐츠 -->
</div>

<!-- 모바일에서는 일부 요소 숨김 -->
<div class="hidden md:block">
  <!-- 데스크톱에서만 표시될 콘텐츠 -->
</div>

<!-- 모바일에서는 컬럼 스택, 데스크톱에서는 가로 배열 -->
<div class="flex flex-col md:flex-row">
  <div class="w-full md:w-1/3"><!-- 콘텐츠 --></div>
  <div class="w-full md:w-2/3"><!-- 콘텐츠 --></div>
</div>
```

### 6.3 컴포넌트 스타일링

재사용 가능한 컴포넌트는 일관된 스타일로 구현합니다.

#### 6.3.1 버튼 컴포넌트

```html
<!-- 기본 버튼 스타일 -->
<button class="btn-primary">저장</button>
<button class="btn-secondary">취소</button>
<button class="btn-danger">삭제</button>

<!-- custom.css에서 정의 -->
.btn-primary { @apply px-4 py-2 font-bold text-white bg-primary-600 rounded
hover:bg-primary-700 focus:outline-none focus:shadow-outline transition
duration-150 ease-in-out; } .btn-secondary { @apply px-4 py-2 font-bold
text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none
focus:shadow-outline transition duration-150 ease-in-out; } .btn-danger { @apply
px-4 py-2 font-bold text-white bg-red-600 rounded hover:bg-red-700
focus:outline-none focus:shadow-outline transition duration-150 ease-in-out; }
```

#### 6.3.2 카드 컴포넌트

```html
<div class="card">
  <div class="card-header">사용자 정보</div>
  <div class="card-body">
    <!-- 카드 내용 -->
  </div>
  <div class="card-footer">
    <!-- 카드 푸터 -->
  </div>
</div>

<!-- custom.css에서 정의 -->
.card { @apply bg-white rounded-lg shadow-md overflow-hidden m-2; } .card-header
{ @apply bg-gray-100 px-4 py-3 border-b border-gray-200 font-medium
text-gray-700; } .card-body { @apply p-4; } .card-footer { @apply bg-gray-50
px-4 py-3 border-t border-gray-200 text-right; }
```

### 6.4 데이터 테이블

데이터 테이블은 일관된 스타일과 반응형 디자인으로 구현합니다.

```html
<div class="overflow-x-auto">
    <table class="data-table">
        <thead>
            <tr>
                <th>ID</th>
                <th>사용자명</th>
                <th>이메일</th>
                <th>상태</th>
                <th>작업</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model.Items)
            {
                <tr>
                    <td>@item.Id</td>
                    <td>@item.UserName</td>
                    <td>@item.Email</td>
                    <td>
                        <span class="status-badge @(item.IsActive ? "status-active" : "status-inactive")">
                            @(item.IsActive ? "활성" : "비활성")
                        </span>
                    </td>
                    <td class="actions">
                        <a href="@Url.Action("Edit", new { id = item.Id })" class="btn-action btn-edit">
                            <i class="fas fa-edit"></i>
                        </a>
                        <a href="@Url.Action("Detail", new { id = item.Id })" class="btn-action btn-view">
                            <i class="fas fa-eye"></i>
                        </a>
                        <a href="@Url.Action("Delete", new { id = item.Id })" class="btn-action btn-delete">
                            <i class="fas fa-trash"></i>
                        </a>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

<!-- custom.css에서 정의 -->
.data-table {
    @apply min-w-full divide-y divide-gray-200;
}

.data-table th {
    @apply px-6 py-3 bg-gray-50 text-left text-xs font-medium text-gray-500 uppercase tracking-wider;
}

.data-table td {
    @apply px-6 py-4 whitespace-nowrap text-sm text-gray-900;
}

.data-table tr {
    @apply hover:bg-gray-50;
}

.status-badge {
    @apply px-2 py-1 text-xs font-medium rounded-full;
}

.status-active {
    @apply bg-green-100 text-green-800;
}

.status-inactive {
    @apply bg-red-100 text-red-800;
}

.actions {
    @apply flex space-x-2;
}

.btn-action {
    @apply p-1 rounded-full text-gray-600 hover:bg-gray-100;
}

.btn-edit:hover {
    @apply text-blue-500;
}

.btn-view:hover {
    @apply text-green-500;
}

.btn-delete:hover {
    @apply text-red-500;
}
```

### 6.5 폼 컴포넌트

폼 요소들은 일관된 스타일과 유효성 검사 시각화를 포함합니다.

```html
<form asp-action="Create" class="form">
  <div class="form-group">
    <label asp-for="UserName" class="form-label">사용자명</label>
    <input asp-for="UserName" class="form-input" />
    <span asp-validation-for="UserName" class="form-error"></span>
  </div>

  <div class="form-group">
    <label asp-for="Email" class="form-label">이메일</label>
    <input asp-for="Email" class="form-input" />
    <span asp-validation-for="Email" class="form-error"></span>
  </div>

  <div class="form-group">
    <label asp-for="RoleIds" class="form-label">역할</label>
    <select
      asp-for="RoleIds"
      asp-items="@Model.AvailableRoles"
      class="form-select"
      multiple
    ></select>
    <span asp-validation-for="RoleIds" class="form-error"></span>
  </div>

  <div class="form-group">
    <div class="flex items-center">
      <input asp-for="IsActive" class="form-checkbox" />
      <label asp-for="IsActive" class="ml-2">활성화</label>
    </div>
  </div>

  <div class="form-actions">
    <button type="submit" class="btn-primary">저장</button>
    <a asp-action="Index" class="btn-secondary">취소</a>
  </div>
</form>

<!-- custom.css에서 정의 -->
.form { @apply space-y-4; } .form-group { @apply mb-4; } .form-label { @apply
block text-sm font-medium text-gray-700 mb-1; } .form-input { @apply mt-1
focus:ring-primary-500 focus:border-primary-500 block w-full shadow-sm
sm:text-sm border-gray-300 rounded-md; } .form-input.input-invalid { @apply
border-red-300 focus:ring-red-500 focus:border-red-500; } .form-select { @apply
mt-1 block w-full py-2 px-3 border border-gray-300 bg-white rounded-md shadow-sm
focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm; }
.form-checkbox { @apply h-4 w-4 text-primary-600 focus:ring-primary-500
border-gray-300 rounded; } .form-error { @apply mt-1 text-sm text-red-600; }
.form-actions { @apply pt-4 flex space-x-2 border-t border-gray-200; }
```

### 6.6 토스트 메시지

사용자 피드백을 위한 토스트 메시지 시스템을 구현합니다.

```html
<!-- _Layout.cshtml -->
<div id="toastContainer" class="fixed top-4 right-4 z-50 space-y-2">
  <!-- 자바스크립트로 동적 추가됨 -->
</div>

<script>
  // 토스트 메시지 표시 함수
  function showToast(message, type = 'info', duration = 3000) {
      const toastContainer = document.getElementById('toastContainer');

      // 토스트 요소 생성
      const toast = document.createElement('div');
      toast.className = `toast toast-${type} transform transition-all duration-300 ease-out opacity-0 translate-x-4`;

      // 아이콘 결정
      let icon = '';
      switch (type) {
          case 'success':
              icon = '<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"></path></svg>';
              break;
          case 'error':
              icon = '<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"></path></svg>';
              break;
          case 'warning':
              icon = '<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"></path></svg>';
              break;
          default:
              icon = '<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"></path></svg>';
      }

      // 토스트 내용 설정
      toast.innerHTML = `
          <div class="flex items-center">
              <div class="flex-shrink-0">${icon}</div>
              <div class="ml-3 mr-4">${message}</div>
              <button class="ml-auto" onclick="this.parentElement.parentElement.remove()">
                  <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path>
                  </svg>
              </button>
          </div>
      `;

      // 토스트 추가
      toastContainer.appendChild(toast);

      // 애니메이션 효과 (등장)
      setTimeout(() => {
          toast.classList.remove('opacity-0', 'translate-x-4');
      }, 10);

      // 지정 시간 후 자동 제거
      setTimeout(() => {
          toast.classList.add('opacity-0', 'translate-x-4');
          setTimeout(() => {
              toast.remove();
          }, 300);
      }, duration);
  }

  // 서버 측 메시지 처리
  document.addEventListener('DOMContentLoaded', function() {
      const messages = @Html.Raw(Json.Serialize(TempData["Messages"] ?? new List<object>()));
      messages.forEach(msg => {
          showToast(msg.text, msg.type);
      });
  });
</script>

<!-- custom.css에서 정의 -->
.toast { @apply px-4 py-3 rounded-lg shadow-lg flex items-center max-w-md; }
.toast-info { @apply bg-blue-50 text-blue-700; } .toast-success { @apply
bg-green-50 text-green-700; } .toast-warning { @apply bg-yellow-50
text-yellow-700; } .toast-error { @apply bg-red-50 text-red-700; }
```

### 6.7 다크 모드 지원

시스템 선호도에 따른 다크 모드를 지원합니다.

```html
<!-- tailwind.config.js -->
module.exports = { darkMode: 'class', // 또는 'media' // 기타 설정... }

<!-- HTML에 다크 모드 클래스 적용 -->
<html class="dark">
  <body class="bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200">
    <!-- 콘텐츠 -->
  </body>
</html>

<!-- 다크 모드 토글 버튼 -->
<button
  id="darkModeToggle"
  class="p-2 rounded-full focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
>
  <!-- 라이트 모드 아이콘 (다크 모드에서 표시) -->
  <svg
    class="w-5 h-5 hidden dark:block"
    fill="currentColor"
    viewBox="0 0 20 20"
  >
    <path
      fill-rule="evenodd"
      d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4 8a4 4 0 11-8 0 4 4 0 018 0zm-.464 4.95a1 1 0 10-1.415 1.414l.707.707a1 1 0 001.414-1.414l-.707-.707zm-7.071 0a1 1 0 00-1.415 1.414l.707.707a1 1 0 001.414-1.414l-.707-.707z"
      clip-rule="evenodd"
    ></path>
  </svg>
  <!-- 다크 모드 아이콘 (라이트 모드에서 표시) -->
  <svg
    class="w-5 h-5 block dark:hidden"
    fill="currentColor"
    viewBox="0 0 20 20"
  >
    <path
      d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z"
    ></path>
  </svg>
</button>

<script>
  // 다크 모드 토글 함수
  document
    .getElementById("darkModeToggle")
    .addEventListener("click", function () {
      if (document.documentElement.classList.contains("dark")) {
        document.documentElement.classList.remove("dark");
        localStorage.setItem("color-theme", "light");
      } else {
        document.documentElement.classList.add("dark");
        localStorage.setItem("color-theme", "dark");
      }
    });

  // 페이지 로드 시 저장된 테마 적용
  document.addEventListener("DOMContentLoaded", function () {
    if (
      localStorage.getItem("color-theme") === "dark" ||
      (!localStorage.getItem("color-theme") &&
        window.matchMedia("(prefers-color-scheme: dark)").matches)
    ) {
      document.documentElement.classList.add("dark");
    } else {
      document.documentElement.classList.remove("dark");
    }
  });
</script>
```

## 7. 실제 구현 사례 분석

실제 프로젝트에서 구현된 기능의 코드 흐름을 분석합니다.

### 7.1 사용자 역할 관리 기능

사용자 역할 관리 기능은 다음과 같은 흐름으로 구현되어 있습니다.

#### 7.1.1 컨트롤러 (MVC 프레젠테이션 계층)

```csharp
// UnivFI.WebUI/Areas/Admin/Controllers/UserRoleController.cs
[Area("Admin")]
[Authorize(Roles = "Administrators")]
public class UserRoleController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly ILogger<UserRoleController> _logger;

    public UserRoleController(
        IUserService userService,
        IRoleService roleService,
        ILogger<UserRoleController> logger)
    {
        _userService = userService;
        _roleService = roleService;
        _logger = logger;
    }

    // 사용자의 역할 관리 페이지
    public async Task<IActionResult> ManageUserRoles(int userId)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var allRoles = await _roleService.GetAllAsync();
            var userRoleIds = await _roleService.GetRoleIdsByUserIdAsync(userId);

            var viewModel = new ManageUserRolesViewModel
            {
                UserId = userId,
                UserName = user.UserName,
                AllRoles = allRoles.Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    IsAssigned = userRoleIds.Contains(r.Id)
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing roles for user {UserId}", userId);
            TempData["ErrorMessage"] = "역할 관리 중 오류가 발생했습니다.";
            return RedirectToAction("Index", "User");
        }
    }

    // 사용자 역할 업데이트
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRoles(ManageUserRolesViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var selectedRoleIds = model.AllRoles
                .Where(r => r.IsAssigned)
                .Select(r => r.Id)
                .ToList();

            var result = await _roleService.AssignRolesToUserAsync(model.UserId, selectedRoleIds);
            if (result)
            {
                _logger.LogInformation("Updated roles for user {UserId}", model.UserId);
                TempData["SuccessMessage"] = "사용자 역할이 성공적으로 업데이트되었습니다.";
            }
            else
            {
                TempData["ErrorMessage"] = "사용자 역할 업데이트 중 오류가 발생했습니다.";
            }

            return RedirectToAction("ManageUserRoles", new { userId = model.UserId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roles for user {UserId}", model.UserId);
            TempData["ErrorMessage"] = "사용자 역할 업데이트 중 오류가 발생했습니다.";
            return RedirectToAction("ManageUserRoles", new { userId = model.UserId });
        }
    }
}
```

#### 7.1.2 서비스 (애플리케이션 계층)

```csharp
// UnivFI.Application/Services/RoleService.cs
public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IUnitOfWork unitOfWork,
        ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<int>> GetRoleIdsByUserIdAsync(int userId)
    {
        var userRoles = await _userRoleRepository.GetByUserIdAsync(userId);
        return userRoles.Select(ur => ur.RoleId);
    }

    public async Task<bool> AssignRolesToUserAsync(int userId, IEnumerable<int> roleIds)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            // 기존 사용자-역할 관계 삭제
            await _userRoleRepository.DeleteByUserIdAsync(userId);

            // 새 사용자-역할 관계 추가
            foreach (var roleId in roleIds)
            {
                await _userRoleRepository.CreateAsync(new UserRoleEntity
                {
                    UserId = userId,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _unitOfWork.Commit();
            return true;
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Error assigning roles to user {UserId}", userId);
            return false;
        }
    }

    // 기타 메서드...
}
```

#### 7.1.3 리포지토리 (인프라스트럭처 계층)

```csharp
// UnivFI.Infrastructure/Repositories/UserRoleRepository.cs
public class UserRoleRepository : BaseRepository<UserRoleEntity, int>, IUserRoleRepository
{
    public UserRoleRepository(IDbConnection dbConnection, ILogger<UserRoleRepository> logger)
        : base(dbConnection, logger, "UserRoles")
    {
    }

    public async Task<IEnumerable<UserRoleEntity>> GetByUserIdAsync(int userId)
    {
        using var connection = _dbConnection;
        var sql = @"
            SELECT ur.*, r.RoleName, r.Description
            FROM UserRoles ur
            JOIN Roles r ON ur.RoleId = r.Id
            WHERE ur.UserId = @UserId";

        LogSqlQuery(sql, new { UserId = userId });

        var userRoles = new List<UserRoleEntity>();
        var roleDict = new Dictionary<int, RoleEntity>();

        await connection.QueryAsync<UserRoleEntity, RoleEntity, UserRoleEntity>(
            sql,
            (userRole, role) =>
            {
                userRole.Role = role;
                userRoles.Add(userRole);
                return userRole;
            },
            new { UserId = userId },
            splitOn: "RoleName"
        );

        return userRoles;
    }

    public async Task<bool> DeleteByUserIdAsync(int userId)
    {
        using var connection = _dbConnection;
        var sql = "DELETE FROM UserRoles WHERE UserId = @UserId";

        LogSqlQuery(sql, new { UserId = userId });

        var result = await connection.ExecuteAsync(sql, new { UserId = userId });
        return result > 0;
    }
}
```

#### 7.1.4 뷰 (HTML/Razor)

```html
<!-- UnivFI.WebUI/Areas/Admin/Views/UserRole/ManageUserRoles.cshtml -->
@model ManageUserRolesViewModel @{ ViewData["Title"] = "사용자 역할 관리"; }

<div class="card">
  <div class="card-header">
    <h2 class="text-xl font-semibold">@Model.UserName 사용자의 역할 관리</h2>
  </div>
  <div class="card-body">
    <form asp-action="UpdateUserRoles" method="post">
      <input type="hidden" asp-for="UserId" />

      <div class="space-y-3">
        @for (var i = 0; i < Model.AllRoles.Count; i++) {
        <div class="flex items-center space-x-3 p-3 bg-gray-50 rounded-md">
          <input type="hidden" asp-for="AllRoles[i].Id" />
          <input type="hidden" asp-for="AllRoles[i].RoleName" />
          <input type="hidden" asp-for="AllRoles[i].Description" />

          <div class="flex items-center">
            <input
              type="checkbox"
              asp-for="AllRoles[i].IsAssigned"
              class="form-checkbox"
            />
          </div>

          <div>
            <div class="font-medium">@Model.AllRoles[i].RoleName</div>
            <div class="text-sm text-gray-500">
              @Model.AllRoles[i].Description
            </div>
          </div>
        </div>
        }
      </div>

      <div class="form-actions mt-4">
        <button type="submit" class="btn-primary">저장</button>
        <a
          asp-controller="User"
          asp-action="Detail"
          asp-route-id="@Model.UserId"
          class="btn-secondary"
          >취소</a
        >
      </div>
    </form>
  </div>
</div>
```

### 7.2 메뉴 트리 관리 기능

계층적 메뉴 구조를 관리하는 기능의 구현 사례입니다.

#### 7.2.1 컨트롤러

```csharp
// UnivFI.WebUI/Areas/Admin/Controllers/MenuController.cs
[Area("Admin")]
[Authorize(Roles = "Administrators")]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;
    private readonly IRoleService _roleService;
    private readonly ILogger<MenuController> _logger;

    public MenuController(
        IMenuService menuService,
        IRoleService roleService,
        ILogger<MenuController> logger)
    {
        _menuService = menuService;
        _roleService = roleService;
        _logger = logger;
    }

    public async Task<IActionResult> Tree()
    {
        var menus = await _menuService.GetMenuTreeAsync();
        return View(menus);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? parentId)
    {
        var viewModel = new MenuViewModel();

        if (parentId.HasValue)
        {
            var parentMenu = await _menuService.GetByIdAsync(parentId.Value);
            if (parentMenu != null)
            {
                viewModel.ParentId = parentId;
                viewModel.ParentTitle = parentMenu.Title;
                viewModel.Levels = (short)((parentMenu.Levels ?? 0) + 1);
            }
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuViewModel viewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var menuDto = new MenuDto
                {
                    ParentId = viewModel.ParentId,
                    MenuKey = viewModel.MenuKey,
                    Url = viewModel.Url,
                    Title = viewModel.Title,
                    MenuOrder = viewModel.MenuOrder,
                    Levels = viewModel.Levels,
                    UseNewIcon = viewModel.UseNewIcon
                };

                var menuId = await _menuService.CreateAsync(menuDto);

                TempData["SuccessMessage"] = "메뉴가 성공적으로 생성되었습니다.";
                return RedirectToAction("Tree");
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu");
            ModelState.AddModelError("", "메뉴 생성 중 오류가 발생했습니다.");
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ManageMenuRoles(int id)
    {
        var menu = await _menuService.GetByIdAsync(id);
        if (menu == null)
            return NotFound();

        var allRoles = await _roleService.GetAllAsync();
        var menuRoleIds = await _menuService.GetRoleIdsByMenuIdAsync(id);

        var viewModel = new ManageMenuRolesViewModel
        {
            MenuId = id,
            MenuTitle = menu.Title,
            AllRoles = allRoles.Select(r => new RoleViewModel
            {
                Id = r.Id,
                RoleName = r.RoleName,
                Description = r.Description,
                IsAssigned = menuRoleIds.Contains(r.Id)
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMenuRoles(ManageMenuRolesViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var selectedRoleIds = model.AllRoles
                .Where(r => r.IsAssigned)
                .Select(r => r.Id)
                .ToList();

            var result = await _menuService.AssignRolesToMenuAsync(model.MenuId, selectedRoleIds);
            if (result)
            {
                TempData["SuccessMessage"] = "메뉴 역할이 성공적으로 업데이트되었습니다.";
            }
            else
            {
                TempData["ErrorMessage"] = "메뉴 역할 업데이트 중 오류가 발생했습니다.";
            }

            return RedirectToAction("ManageMenuRoles", new { id = model.MenuId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roles for menu {MenuId}", model.MenuId);
            TempData["ErrorMessage"] = "메뉴 역할 업데이트 중 오류가 발생했습니다.";
            return RedirectToAction("ManageMenuRoles", new { id = model.MenuId });
        }
    }
}
```

#### 7.2.2 서비스

```csharp
// UnivFI.Application/Services/MenuService.cs
public class MenuService : IMenuService
{
    private readonly IMenuRepository _menuRepository;
    private readonly IMenuRoleRepository _menuRoleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MenuService> _logger;

    public MenuService(
        IMenuRepository menuRepository,
        IMenuRoleRepository menuRoleRepository,
        IUnitOfWork unitOfWork,
        ILogger<MenuService> logger)
    {
        _menuRepository = menuRepository;
        _menuRoleRepository = menuRoleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<MenuTreeDto>> GetMenuTreeAsync()
    {
        var entities = await _menuRepository.GetMenuTreeAsync();
        return MapToMenuTreeDto(entities);
    }

    public async Task<IEnumerable<MenuDto>> GetUserAccessibleMenusAsync(int userId)
    {
        var entities = await _menuRepository.GetMenusByUserIdAsync(userId);
        return entities.Select(MapToDto);
    }

    public async Task<IEnumerable<int>> GetRoleIdsByMenuIdAsync(int menuId)
    {
        var menuRoles = await _menuRoleRepository.GetByMenuIdAsync(menuId);
        return menuRoles.Select(mr => mr.RoleId);
    }

    public async Task<bool> AssignRolesToMenuAsync(int menuId, IEnumerable<int> roleIds)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            // 기존 메뉴-역할 관계 삭제
            await _menuRoleRepository.DeleteByMenuIdAsync(menuId);

            // 새 메뉴-역할 관계 추가
            foreach (var roleId in roleIds)
            {
                await _menuRoleRepository.CreateAsync(new MenuRoleEntity
                {
                    MenuId = menuId,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _unitOfWork.Commit();
            return true;
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Error assigning roles to menu {MenuId}", menuId);
            return false;
        }
    }

    private MenuDto MapToDto(MenuEntity entity)
    {
        return new MenuDto
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            MenuKey = entity.MenuKey,
            Url = entity.Url,
            Title = entity.Title,
            Levels = entity.Levels,
            MenuOrder = entity.MenuOrder,
            UseNewIcon = entity.UseNewIcon,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private IEnumerable<MenuTreeDto> MapToMenuTreeDto(IEnumerable<MenuTreeItem> items)
    {
        var result = new List<MenuTreeDto>();

        // 최상위 메뉴 아이템 필터링
        var rootItems = items.Where(m => m.ParentId == null).OrderBy(m => m.MenuOrder);

        foreach (var rootItem in rootItems)
        {
            var treeItem = new MenuTreeDto
            {
                Id = rootItem.Id,
                ParentId = rootItem.ParentId,
                Title = rootItem.Title,
                MenuKey = rootItem.MenuKey,
                Url = rootItem.Url,
                Levels = rootItem.Levels,
                MenuOrder = rootItem.MenuOrder,
                UseNewIcon = rootItem.UseNewIcon,
                Children = GetChildMenus(items, rootItem.Id)
            };

            result.Add(treeItem);
        }

        return result;
    }

    private IEnumerable<MenuTreeDto> GetChildMenus(IEnumerable<MenuTreeItem> allItems, int parentId)
    {
        var children = allItems
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.MenuOrder)
            .Select(item => new MenuTreeDto
            {
                Id = item.Id,
                ParentId = item.ParentId,
                Title = item.Title,
                MenuKey = item.MenuKey,
                Url = item.Url,
                Levels = item.Levels,
                MenuOrder = item.MenuOrder,
                UseNewIcon = item.UseNewIcon
            })
            .ToList();

        // 자식 메뉴들의 자식 메뉴 설정 (재귀)
        foreach (var child in children)
        {
            child.Children = GetChildMenus(allItems, child.Id);
        }

        return children;
    }
}
```

#### 7.2.3 리포지토리

```csharp
// UnivFI.Infrastructure/Repositories/MenuRepository.cs
public class MenuRepository : BaseRepository<MenuEntity, int>, IMenuRepository
{
    public MenuRepository(IDbConnection dbConnection, ILogger<MenuRepository> logger)
        : base(dbConnection, logger, "Menus")
    {
    }

    public async Task<IEnumerable<MenuTreeItem>> GetMenuTreeAsync()
    {
        using var connection = _dbConnection;
        var sql = @"
            WITH MenuCTE AS (
                SELECT
                    Id, ParentId, MenuKey, Url, Title, Levels, MenuOrder, UseNewIcon,
                    CreatedAt, UpdatedAt
                FROM Menus
            )
            SELECT * FROM MenuCTE
            ORDER BY Levels, MenuOrder";

        LogSqlQuery(sql);

        return await connection.QueryAsync<MenuTreeItem>(sql);
    }

    public async Task<IEnumerable<MenuEntity>> GetMenusByUserIdAsync(int userId)
    {
        using var connection = _dbConnection;
        var sql = @"
            SELECT DISTINCT m.*
            FROM Menus m
            JOIN MenuRoles mr ON m.Id = mr.MenuId
            JOIN UserRoles ur ON mr.RoleId = ur.RoleId
            WHERE ur.UserId = @UserId
            ORDER BY m.Levels, m.MenuOrder";

        LogSqlQuery(sql, new { UserId = userId });

        return await connection.QueryAsync<MenuEntity>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId)
    {
        using var connection = _dbConnection;
        var sql = @"
            SELECT m.*
            FROM Menus m
            JOIN MenuRoles mr ON m.Id = mr.MenuId
            WHERE mr.RoleId = @RoleId
            ORDER BY m.Levels, m.MenuOrder";

        LogSqlQuery(sql, new { RoleId = roleId });

        return await connection.QueryAsync<MenuEntity>(sql, new { RoleId = roleId });
    }
}
```

#### 7.2.4 메뉴 트리 뷰 (JavaScript)

```html
<!-- UnivFI.WebUI/Areas/Admin/Views/Menu/Tree.cshtml -->
@model IEnumerable<MenuTreeDto>
  @{ ViewData["Title"] = "메뉴 트리"; }

  <div class="card">
    <div class="card-header flex justify-between items-center">
      <h2 class="text-xl font-semibold">메뉴 구조</h2>
      <a asp-action="Create" class="btn-primary">
        <i class="fas fa-plus mr-1"></i> 새 메뉴
      </a>
    </div>
    <div class="card-body">
      <div id="menuTree" class="menu-tree">
        @await Html.PartialAsync("_MenuTreePartial", Model)
      </div>
    </div>
  </div>

  @section Scripts {
  <script>
    document.addEventListener("DOMContentLoaded", function () {
      // 메뉴 확장/축소 토글
      document.querySelectorAll(".menu-toggle").forEach((button) => {
        button.addEventListener("click", function () {
          const menuId = this.dataset.menuId;
          const menuChildren = document.getElementById(
            `menu-children-${menuId}`
          );
          const toggleIcon = this.querySelector("i");

          if (menuChildren.classList.contains("hidden")) {
            menuChildren.classList.remove("hidden");
            toggleIcon.classList.remove("fa-chevron-right");
            toggleIcon.classList.add("fa-chevron-down");
          } else {
            menuChildren.classList.add("hidden");
            toggleIcon.classList.remove("fa-chevron-down");
            toggleIcon.classList.add("fa-chevron-right");
          }
        });
      });

      // 드래그 앤 드롭 기능
      // 이 부분은 드래그 앤 드롭 라이브러리가 필요하며 너무 복잡하여 예시에서는 생략
    });
  </script>
  }

  <!-- UnivFI.WebUI/Areas/Admin/Views/Menu/_MenuTreePartial.cshtml -->
  @model IEnumerable<MenuTreeDto>
    <ul class="menu-list">
      @foreach (var menu in Model) {
      <li class="menu-item">
        <div class="menu-item-header">
          @if (menu.Children.Any()) {
          <button type="button" class="menu-toggle" data-menu-id="@menu.Id">
            <i class="fas fa-chevron-down"></i>
          </button>
          } else {
          <span class="menu-toggle-placeholder"></span>
          }

          <span class="menu-title">@menu.Title</span>

          <div class="menu-actions">
            <a
              asp-action="Edit"
              asp-route-id="@menu.Id"
              class="btn-action btn-edit"
              title="편집"
            >
              <i class="fas fa-edit"></i>
            </a>
            <a
              asp-action="Detail"
              asp-route-id="@menu.Id"
              class="btn-action btn-view"
              title="상세"
            >
              <i class="fas fa-eye"></i>
            </a>
            <a
              asp-action="Create"
              asp-route-parentId="@menu.Id"
              class="btn-action btn-add"
              title="하위 메뉴 추가"
            >
              <i class="fas fa-plus"></i>
            </a>
            <a
              asp-action="ManageMenuRoles"
              asp-route-id="@menu.Id"
              class="btn-action btn-roles"
              title="역할 관리"
            >
              <i class="fas fa-user-shield"></i>
            </a>
            <a
              asp-action="Delete"
              asp-route-id="@menu.Id"
              class="btn-action btn-delete"
              title="삭제"
            >
              <i class="fas fa-trash"></i>
            </a>
          </div>
        </div>

        @if (menu.Children.Any()) {
        <div id="menu-children-@menu.Id" class="menu-children">
          @await Html.PartialAsync("_MenuTreePartial", menu.Children)
        </div>
        }
      </li>
      }
    </ul></MenuTreeDto
  ></MenuTreeDto
>
```

## 8. 참고 자료

### 8.1 아키텍처 및 디자인 패턴

1. Robert C. Martin, "Clean Architecture: A Craftsman's Guide to Software Structure and Design", Prentice Hall, 2017.
2. Eric Evans, "Domain-Driven Design: Tackling Complexity in the Heart of Software", Addison-Wesley, 2003.
3. Martin Fowler, "Patterns of Enterprise Application Architecture", Addison-Wesley, 2002.
4. Mark Seemann, "Dependency Injection in .NET", Manning Publications, 2011.

### 8.2 ASP.NET Core 관련

1. ASP.NET Core Documentation: https://docs.microsoft.com/en-us/aspnet/core/
2. ASP.NET Core MVC 보안 관련: https://docs.microsoft.com/en-us/aspnet/core/security/
3. Microsoft Identity: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity
4. Razor Pages: https://docs.microsoft.com/en-us/aspnet/core/tutorials/razor-pages/

### 8.3 데이터 액세스

1. Dapper 공식 문서: https://github.com/DapperLib/Dapper
2. SQL Server 성능 튜닝 가이드: https://docs.microsoft.com/en-us/sql/relational-databases/performance/
3. Entity Framework Core: https://docs.microsoft.com/en-us/ef/core/

### 8.4 프론트엔드

1. Tailwind CSS 공식 문서: https://tailwindcss.com/docs
2. JavaScript 최신 트렌드: https://developer.mozilla.org/en-US/docs/Web/JavaScript

### 8.5 보안

1. OWASP Top 10: https://owasp.org/www-project-top-ten/
2. NIST 암호화 가이드라인: https://csrc.nist.gov/publications/detail/sp/800-63/3/final
3. Microsoft Security 가이드라인: https://www.microsoft.com/en-us/security/
