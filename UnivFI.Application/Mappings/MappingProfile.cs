using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;
using UnivFI.Domain.Entities;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace UnivFI.Application.Mappings
{
    /// <summary>
    /// Mapster 설정 클래스 - 도메인 엔티티와 애플리케이션 DTO 간의 매핑 규칙 정의
    /// 클린 아키텍처에서 계층 간 데이터 변환을 담당
    /// </summary>
    public static class MapsterConfig
    {
        public static void ConfigureApplicationMappings(this TypeAdapterConfig config)
        {
            // ==============================================================================================
            // User 관련 매핑 설정
            // ==============================================================================================

            // UserEntity ↔ UserDto 간 양방향 매핑 설정
            config.NewConfig<UserEntity, UserDto>().TwoWays();

            // CreateUserDto → UserEntity 매핑 (사용자 생성 시 사용)
            config.NewConfig<CreateUserDto, UserEntity>()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Salt)
                .Ignore(dest => dest.RefreshToken)
                .Ignore(dest => dest.RefreshTokenExpiry)
                .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
                .Ignore(dest => dest.UpdatedAt)
                .Ignore(dest => dest.UserRoles);

            // UpdateUserDto → UserEntity 매핑 (사용자 정보 수정 시 사용)
            config.NewConfig<UpdateUserDto, UserEntity>()
                .Ignore(dest => dest.Salt)
                .Ignore(dest => dest.RefreshToken)
                .Ignore(dest => dest.RefreshTokenExpiry)
                .Ignore(dest => dest.CreatedAt)
                .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
                .Ignore(dest => dest.UserRoles);

            // ==============================================================================================
            // Menu 관련 매핑 설정
            // ==============================================================================================

            // MenuEntity ↔ MenuDto 간 양방향 매핑 설정
            config.NewConfig<MenuEntity, MenuDto>().TwoWays();

            // CreateMenuDto → MenuEntity 매핑 (메뉴 생성 시 사용)
            config.NewConfig<CreateMenuDto, MenuEntity>()
                .Ignore(dest => dest.Id)
                .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
                .Ignore(dest => dest.UpdatedAt)
                .Ignore(dest => dest.MenuRoles);

            // UpdateMenuDto → MenuEntity 매핑 (메뉴 수정 시 사용)
            config.NewConfig<UpdateMenuDto, MenuEntity>()
                .Ignore(dest => dest.CreatedAt)
                .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
                .Ignore(dest => dest.MenuRoles);

            // ==============================================================================================
            // Role 관련 매핑 설정
            // ==============================================================================================

            // RoleEntity ↔ RoleDto 간 양방향 매핑 설정
            config.NewConfig<RoleEntity, RoleDto>().TwoWays();

            // CreateRoleDto → RoleEntity 매핑 (역할 생성 시 사용)
            config.NewConfig<CreateRoleDto, RoleEntity>()
                .Ignore(dest => dest.Id)
                .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
                .Ignore(dest => dest.UpdatedAt)
                .Ignore(dest => dest.MenuRoles)
                .Ignore(dest => dest.UserRoles);

            // UpdateRoleDto → RoleEntity 매핑 (역할 수정 시 사용)
            config.NewConfig<UpdateRoleDto, RoleEntity>()
                .Ignore(dest => dest.CreatedAt)
                .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
                .Ignore(dest => dest.MenuRoles)
                .Ignore(dest => dest.UserRoles);

            // ==============================================================================================
            // 관계 엔티티 매핑 설정
            // ==============================================================================================

            // MenuRoleEntity → MenuRoleDto 매핑 (메뉴-역할 관계 조회 시 사용)
            config.NewConfig<MenuRoleEntity, MenuRoleDto>()
                .Ignore(dest => dest.Id)
                .Map(dest => dest.MenuKey, src => src.Menu.MenuKey)
                .Map(dest => dest.MenuTitle, src => src.Menu.Title)
                .Map(dest => dest.RoleName, src => src.Role.RoleName);

            // UserRoleEntity → UserRoleDto 매핑 (사용자-역할 관계 조회 시 사용)
            config.NewConfig<UserRoleEntity, UserRoleDto>()
                .Ignore(dest => dest.Id)
                .Map(dest => dest.UserName, src => src.User != null ? src.User.UserName : null)
                .Map(dest => dest.RoleName, src => src.Role != null ? src.Role.RoleName : null)
                .Map(dest => dest.User, src => src.User)
                .Map(dest => dest.Role, src => src.Role);

            // ==============================================================================================
            // UIElement 관련 매핑 설정
            // ==============================================================================================

            // UIElementEntity ↔ UIElementDto 간 매핑 설정
            config.NewConfig<UIElementEntity, UIElementDto>().TwoWays();

            // CreateUIElementDto → UIElementEntity 매핑 (UI 요소 생성 시 사용)
            config.NewConfig<CreateUIElementDto, UIElementEntity>()
                .Ignore(dest => dest.Id)
                .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
                .Ignore(dest => dest.UpdatedAt)
                .Ignore(dest => dest.UserPermissions);

            // UpdateUIElementDto → UIElementEntity 매핑 (UI 요소 수정 시 사용)
            config.NewConfig<UpdateUIElementDto, UIElementEntity>()
                .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.UserPermissions);

            // UIElementEntity → UIElementWithPermissionDto 매핑 (UI 요소와 권한 정보 포함)
            config.NewConfig<UIElementEntity, UIElementWithPermissionDto>()
                .Ignore(dest => dest.IsEnabled);

            // ==============================================================================================
            // UIElementUserPermission 관련 매핑 설정
            // ==============================================================================================

            // UIElementUserPermissionEntity → UIElementUserPermissionDto 매핑
            config.NewConfig<UIElementUserPermissionEntity, UIElementUserPermissionDto>()
                .Map(dest => dest.ElementKey, src => src.UIElement != null ? src.UIElement.ElementKey : null)
                .Map(dest => dest.ElementName, src => src.UIElement != null ? src.UIElement.ElementName : null)
                .Map(dest => dest.ElementType, src => src.UIElement != null ? src.UIElement.ElementType : null);

            // CreateUIElementUserPermissionDto → UIElementUserPermissionEntity 매핑
            config.NewConfig<CreateUIElementUserPermissionDto, UIElementUserPermissionEntity>()
                .Ignore(dest => dest.UIElement)
                .Ignore(dest => dest.User);
        }

        /// <summary>
        /// DI 설정 메서드
        /// </summary>
        public static IServiceCollection AddApplicationMappings(this IServiceCollection services)
        {
            // TypeAdapterConfig 설정 
            var config = TypeAdapterConfig.GlobalSettings;
            config.ConfigureApplicationMappings();

            // IMapper 등록
            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();

            return services;
        }
    }
}
