using Microsoft.Extensions.DependencyInjection;
using UnivFI.Application.Interfaces;
using UnivFI.Application.Interfaces.Services;
using UnivFI.Application.Mappings;
using UnivFI.Application.Services;

namespace UnivFI.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // 서비스 등록
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IMenuRoleService, MenuRoleService>();
            services.AddScoped<IUserRoleService, UserRoleService>();
            services.AddScoped<ISystemInitService, SystemInitService>();
            services.AddScoped<IUIElementService, UIElementService>();
            services.AddScoped<IUIElementUserPermissionService, UIElementUserPermissionService>();

            // Mapster 매핑 설정 등록
            services.AddApplicationMappings();

            return services;
        }
    }
}