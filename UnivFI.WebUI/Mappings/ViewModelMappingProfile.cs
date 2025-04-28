using Mapster;
using MapsterMapper;
using UnivFI.Application.DTOs;
using UnivFI.WebUI.ViewModels;
using UnivFI.WebUI.Areas.Admin.ViewModels.Menu;
using UnivFI.WebUI.Areas.Admin.ViewModels.User;
using UnivFI.WebUI.Areas.Admin.ViewModels.Role;
using UnivFI.WebUI.Areas.Admin.ViewModels.MenuRole;
using UnivFI.WebUI.Areas.Admin.ViewModels.UserRole;
using UnivFI.WebUI.ViewModels.UIElement;
using UnivFI.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using UnivFI.WebUI.Areas.Admin.ViewModels.Member;

namespace UnivFI.WebUI.Mappings
{
    /// <summary>
    /// Mapster 설정 클래스 - 애플리케이션 계층 DTO와 웹 계층 ViewModel 간의 매핑 규칙 정의
    /// 클린 아키텍처에서 애플리케이션과 표현 계층 간 데이터 변환을 담당
    /// </summary>
    public static class MapsterConfig
    {
        public static void ConfigureWebMappings(this TypeAdapterConfig config)
        {
            // ==============================================================================================
            // User 관련 매핑 설정
            // ==============================================================================================

            // UserDto ↔ UserViewModel 간 양방향 매핑 설정
            config.NewConfig<UserDto, UserViewModel>()
                .Ignore(dest => dest.UpdatedAt)
                .Ignore(dest => dest.LastLoginAt)
                .Ignore(dest => dest.Roles)
                .TwoWays();

            // UserCreateViewModel → CreateUserDto 매핑 (사용자 생성 폼 제출 시 사용)
            config.NewConfig<UserCreateViewModel, CreateUserDto>();

            // UserViewModel → UpdateUserDto 매핑 (사용자 정보 업데이트 시 사용)
            config.NewConfig<UserViewModel, UpdateUserDto>()
                .Ignore(dest => dest.Password)
                .Ignore(dest => dest.ConfirmPassword);

            // ==============================================================================================
            // Menu 관련 매핑 설정
            // ==============================================================================================

            // MenuDto ↔ MenuViewModel 간 양방향 매핑 설정
            config.NewConfig<MenuDto, MenuViewModel>()
                .Ignore(dest => dest.SelectedRoles)
                .TwoWays();

            // MenuEntity ↔ MenuTreeViewModel 매핑 설정
            config.NewConfig<MenuEntity, MenuTreeViewModel>()
                .Ignore(dest => dest.Children);

            // MenuCreateViewModel → CreateMenuDto 매핑 (메뉴 생성 폼 제출 시 사용)
            config.NewConfig<MenuCreateViewModel, CreateMenuDto>()
                .Ignore(dest => dest.ParentId)
                .Ignore(dest => dest.MenuOrder)
                .Ignore(dest => dest.Levels)
                .Ignore(dest => dest.UseNewIcon);

            // MenuViewModel → UpdateMenuDto 매핑 (메뉴 수정 시 사용)
            config.NewConfig<MenuViewModel, UpdateMenuDto>();

            // ==============================================================================================
            // Role 관련 매핑 설정
            // ==============================================================================================

            // RoleDto ↔ RoleViewModel 간 양방향 매핑 설정
            config.NewConfig<RoleDto, RoleViewModel>().TwoWays();

            // RoleCreateViewModel → CreateRoleDto 매핑 (역할 생성 폼 제출 시 사용)
            config.NewConfig<RoleCreateViewModel, CreateRoleDto>();

            // RoleUpdateViewModel → UpdateRoleDto 매핑 (역할 수정 시 사용)
            config.NewConfig<RoleUpdateViewModel, UpdateRoleDto>();

            // RoleViewModel → UpdateRoleDto 매핑 (역할 수정 시 사용)
            config.NewConfig<RoleViewModel, UpdateRoleDto>()
                .Ignore(dest => dest.RoleComment);

            // ==============================================================================================
            // 관계 엔티티 매핑 설정
            // ==============================================================================================

            // MenuRoleDto ↔ MenuRoleViewModel 간 양방향 매핑 설정
            config.NewConfig<MenuRoleDto, MenuRoleViewModel>().TwoWays();

            // UserRoleDto ↔ UserRoleViewModel 간 양방향 매핑 설정
            config.NewConfig<UserRoleDto, UserRoleViewModel>()
                .Map(dest => dest.Email, src => src.User != null ? src.User.Email : null)
                .Map(dest => dest.Name, src => src.User != null ? src.User.Name : null)
                .TwoWays();

            // ==============================================================================================
            // UIElement 관련 매핑 설정
            // ==============================================================================================

            // UIElementUserPermissionDto ↔ UIElementUserPermissionViewModel 간 양방향 매핑 설정
            config.NewConfig<UIElementUserPermissionDto, UIElementUserPermissionViewModel>()
                .Map(dest => dest.IsEnabled, _ => true)
                .TwoWays();

            // Member 관련 매핑 설정
            config.NewConfig<UserDto, MemberViewModel>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.UserName, src => src.UserName)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt);
            //.Map(dest => dest.UpdatedAt, src => src.UpdatedAt);
        }

        /// <summary>
        /// DI 설정 메서드
        /// </summary>
        public static IServiceCollection AddWebMappings(this IServiceCollection services)
        {
            var config = TypeAdapterConfig.GlobalSettings;
            config.ConfigureWebMappings();

            return services;
        }
    }
}