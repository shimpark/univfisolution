using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnivFI.Application.Interfaces;
using UnivFI.Domain.Interfaces.Repositories;
using UnivFI.Infrastructure.Helpers;
using UnivFI.Infrastructure.Repositories;
using UnivFI.Infrastructure.Services;
using Dapper;
using System.Reflection;
using UnivFI.Domain.Entities;

namespace UnivFI.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Dapper 매핑 설정
            ConfigureDapperMappings();

            // 데이터베이스 연결 팩토리 등록
            services.AddSingleton<IConnectionFactory>(provider =>
                new ConnectionFactory(
                    configuration,
                    provider.GetRequiredService<UnivFI.Infrastructure.Security.IEncryptionService>()));

            // DAO(Repository) 등록 - IConnectionFactory 의존성 주입 사용
            services.AddScoped<IUserRepository, UserRepository>();        // 사용자 리포지토리
            services.AddScoped<IMenuRepository, MenuRepository>();        // 메뉴 리포지토리
            services.AddScoped<IRoleRepository, RoleRepository>();        // 역할 리포지토리
            services.AddScoped<IMenuRoleRepository, MenuRoleRepository>();// 메뉴 역할 리포지토리
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();// 사용자 역할 리포지토리
            services.AddScoped<IFileAttachRepository, FileAttachRepository>(); // 파일 첨부 리포지토리
            services.AddScoped<IUIElementRepository, UIElementRepository>(); // UI 요소 리포지토리
            services.AddScoped<IUIElementUserPermissionRepository, UIElementUserPermissionRepository>(); // UI 요소 사용자 권한 리포지토리

            // AWS S3 파일 서비스 등록
            services.AddScoped<IS3Service, S3Service>();
            services.AddScoped<S3Helper>();

            return services;
        }

        /// <summary>
        /// Dapper 매핑 설정을 구성합니다.
        /// </summary>
        private static void ConfigureDapperMappings()
        {
            // 테이블 매핑 설정
            Dapper.Contrib.Extensions.SqlMapperExtensions.TableNameMapper = (type) =>
            {
                if (type == typeof(UserEntity)) return "Users";
                if (type == typeof(RoleEntity)) return "Roles";
                if (type == typeof(MenuEntity)) return "Menus";
                if (type == typeof(UserRoleEntity)) return "UserRoles";
                if (type == typeof(MenuRoleEntity)) return "MenuRoles";
                if (type == typeof(FileAttachEntity)) return "FileAttaches";
                if (type == typeof(UIElementEntity)) return "UIElements";
                if (type == typeof(UIElementUserPermissionEntity)) return "UIElementUserPermissions";

                // 기본 매핑 (클래스 이름에서 Entity 제거)
                return type.Name.Replace("Entity", "");
            };
        }
    }
}