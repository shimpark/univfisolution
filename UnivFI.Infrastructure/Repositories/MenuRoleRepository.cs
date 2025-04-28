using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using UnivFI.Infrastructure.Helpers;

namespace UnivFI.Infrastructure.Repositories
{
    /// <summary>
    /// 메뉴-역할 연결 관계를 관리하는 리포지토리입니다.
    /// 특정 역할이 접근할 수 있는 메뉴 권한을 처리합니다.
    /// </summary>
    /// <remarks>
    /// 주요 기능:
    /// - 역할별 접근 가능한 메뉴 조회
    /// - 메뉴별 접근 가능한 역할 조회
    /// - 역할에 메뉴 권한 할당 및 제거
    /// - 메뉴 권한의 일괄 처리
    /// - 권한 매핑 검증 및 확인
    /// </remarks>
    public class MenuRoleRepository : BaseRepository<MenuRoleEntity, object, MenuRoleRepository>, IMenuRoleRepository
    {
        private const string TABLE_NAME = "MenuRoles";

        public MenuRoleRepository(IConnectionFactory connectionFactory, ILogger<MenuRoleRepository> logger)
            : base(connectionFactory, logger, TABLE_NAME)
        {
        }

        /// <summary>
        /// 모든 메뉴-역할 매핑 정보를 조회합니다.
        /// 메뉴 및 역할 정보를 함께 로드합니다.
        /// </summary>
        /// <returns>모든 메뉴-역할 매핑 목록</returns>
        public new async Task<IEnumerable<MenuRoleEntity>> GetAllAsync()
        {
            using var connection = CreateConnection();

            try
            {
                // 메뉴와 역할 정보를 함께 조회하는 조인 쿼리
                var query = @"
                    SELECT mr.MenuId, mr.RoleId, 
                           m.MenuKey, m.Url, m.Title,
                           r.RoleName, r.RoleComment
                    FROM MenuRoles mr
                    JOIN Menus m ON mr.MenuId = m.Id
                    JOIN Roles r ON mr.RoleId = r.Id
                    ORDER BY m.MenuOrder, r.RoleName";

                // SQL 쿼리 로깅
                LogQuery(query, new { });

                // 조인 쿼리 실행 및 객체 매핑
                var menuRoles = await connection.QueryAsync<MenuRoleEntity, MenuEntity, RoleEntity, MenuRoleEntity>(
                    query,
                    (menuRole, menu, role) =>
                    {
                        menuRole.Menu = menu;
                        menuRole.Role = role;
                        return menuRole;
                    },
                    splitOn: "MenuKey,RoleName");

                return menuRoles;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "모든 메뉴-역할 매핑 조회 중 오류 발생");
                throw;
            }
        }

        /// <summary>
        /// 특정 메뉴에 할당된 모든 역할 매핑을 조회합니다.
        /// 역할 정보를 함께 로드합니다.
        /// </summary>
        /// <param name="menuId">조회할 메뉴 ID</param>
        /// <returns>메뉴에 할당된 역할 매핑 목록</returns>
        public async Task<IEnumerable<MenuRoleEntity>> GetByMenuIdAsync(int menuId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT mr.MenuId, mr.RoleId, m.MenuKey, m.Title,
                       r.RoleName
                FROM MenuRoles mr
                JOIN Menus m ON mr.MenuId = m.Id
                JOIN Roles r ON mr.RoleId = r.Id
                WHERE mr.MenuId = @MenuId
                ORDER BY r.Id";

            var parameters = new { MenuId = menuId };

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            var menuRoles = await connection.QueryAsync<MenuRoleEntity, MenuEntity, RoleEntity, MenuRoleEntity>(
                query,
                (menuRole, menu, role) =>
                {
                    menuRole.Menu = menu;  // Menu 엔티티 설정
                    menuRole.Role = role;  // Role 엔티티 설정
                    return menuRole;
                },
                parameters,
                splitOn: "MenuKey,RoleName");

            return menuRoles;
        }

        /// <summary>
        /// 특정 역할에 할당된 모든 메뉴 매핑을 조회합니다.
        /// 메뉴 정보를 함께 로드합니다.
        /// </summary>
        /// <param name="roleId">조회할 역할 ID</param>
        /// <returns>역할에 할당된 메뉴 매핑 목록</returns>
        public async Task<IEnumerable<MenuRoleEntity>> GetByRoleIdAsync(int roleId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT mr.MenuId, mr.RoleId, 
                       m.MenuKey, m.Url, m.Title
                FROM MenuRoles mr
                JOIN Menus m ON mr.MenuId = m.Id
                WHERE mr.RoleId = @RoleId
                ORDER BY m.Id";

            var parameters = new { RoleId = roleId };

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            var menuRoles = await connection.QueryAsync<MenuRoleEntity, MenuEntity, MenuRoleEntity>(
                query,
                (menuRole, menu) =>
                {
                    menuRole.Menu = menu;
                    return menuRole;
                },
                parameters,
                splitOn: "MenuKey");

            return menuRoles;
        }

        /// <summary>
        /// 특정 메뉴에 역할을 할당합니다.
        /// 이미 할당된 경우 중복 할당하지 않고 성공으로 처리합니다.
        /// </summary>
        /// <param name="menuId">역할을 할당할 메뉴 ID</param>
        /// <param name="roleId">메뉴에 할당할 역할 ID</param>
        /// <returns>할당 성공 여부</returns>
        public async Task<bool> AssignRoleToMenuAsync(int menuId, int roleId)
        {
            try
            {
                using var connection = CreateConnection();

                // 먼저 이미 존재하는지 확인
                var exists = await MenuHasRoleAsync(menuId, roleId);
                if (exists)
                    return true; // 이미 할당된 경우 성공으로 간주

                // 새로운 메뉴-역할 할당
                var menuRole = new MenuRoleEntity
                {
                    MenuId = menuId,
                    RoleId = roleId
                };

                // 엔티티 작업 로깅
                LogEntityOperation("INSERT INTO", menuRole);

                var id = await connection.InsertAsync(menuRole);
                return id > 0;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "메뉴에 역할 할당 중 오류 발생 (메뉴 ID: {MenuId}, 역할 ID: {RoleId})", menuId, roleId);
            }
        }

        /// <summary>
        /// 메뉴에서 특정 역할을 제거합니다.
        /// </summary>
        /// <param name="menuId">역할을 제거할 메뉴 ID</param>
        /// <param name="roleId">메뉴에서 제거할 역할 ID</param>
        /// <returns>제거 성공 여부</returns>
        public async Task<bool> RemoveRoleFromMenuAsync(int menuId, int roleId)
        {
            try
            {
                using var connection = CreateConnection();

                var menuRole = new MenuRoleEntity
                {
                    MenuId = menuId,
                    RoleId = roleId
                };

                // 엔티티 작업 로깅
                LogEntityOperation("DELETE FROM", menuRole);

                return await connection.DeleteAsync(menuRole);
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "메뉴에서 역할 제거 중 오류 발생 (메뉴 ID: {MenuId}, 역할 ID: {RoleId})", menuId, roleId);
            }
        }

        /// <summary>
        /// 특정 메뉴에 할당된 모든 역할을 제거합니다.
        /// </summary>
        /// <param name="menuId">역할을 모두 제거할 메뉴 ID</param>
        /// <returns>제거 성공 여부</returns>
        public async Task<bool> RemoveAllRolesFromMenuAsync(int menuId)
        {
            try
            {
                using var connection = CreateConnection();

                var query = @"
                    DELETE FROM MenuRoles
                    WHERE MenuId = @MenuId";

                var parameters = new { MenuId = menuId };

                // SQL 쿼리 로깅
                LogQuery(query, parameters);

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "메뉴에서 모든 역할 제거 중 오류 발생 (메뉴 ID: {MenuId})", menuId);
            }
        }

        /// <summary>
        /// 특정 메뉴가 지정된 역할에 할당되어 있는지 확인합니다.
        /// </summary>
        /// <param name="menuId">확인할 메뉴 ID</param>
        /// <param name="roleId">확인할 역할 ID</param>
        /// <returns>메뉴-역할 연결 존재 여부</returns>
        public async Task<bool> MenuHasRoleAsync(int menuId, int roleId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT COUNT(1)
                FROM MenuRoles
                WHERE MenuId = @MenuId AND RoleId = @RoleId";

            var parameters = new { MenuId = menuId, RoleId = roleId };

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            var count = await connection.ExecuteScalarAsync<int>(query, parameters);
            return count > 0;
        }

        /// <summary>
        /// 특정 검색 조건에 맞는 메뉴-역할 매핑의 총 개수를 조회합니다.
        /// </summary>
        /// <param name="searchTerm">검색어 (선택사항)</param>
        /// <param name="searchFields">검색할 필드 (선택사항)</param>
        /// <returns>검색 조건에 맞는 총 레코드 수</returns>
        public async Task<int> GetTotalCountAsync(string searchTerm = null, string searchFields = null)
        {
            using var connection = CreateConnection();

            try
            {
                var whereClause = string.Empty;
                var parameters = new DynamicParameters();

                // 검색 조건이 있는 경우 WHERE 절 구성
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchConditions = new List<string>();

                    // 검색 필드가 지정된 경우 해당 필드만 검색
                    if (!string.IsNullOrWhiteSpace(searchFields))
                    {
                        var fields = searchFields.Split(',');
                        foreach (var field in fields)
                        {
                            switch (field.Trim().ToLower())
                            {
                                case "menuname":
                                case "menutitle":
                                    searchConditions.Add("m.Title LIKE @SearchTerm");
                                    break;
                                case "menukey":
                                    searchConditions.Add("m.MenuKey LIKE @SearchTerm");
                                    break;
                                case "rolename":
                                    searchConditions.Add("r.RoleName LIKE @SearchTerm");
                                    break;
                                case "rolecomment":
                                    searchConditions.Add("r.RoleComment LIKE @SearchTerm");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // 기본적으로 모든 관련 필드 검색
                        searchConditions.Add("m.Title LIKE @SearchTerm");
                        searchConditions.Add("m.MenuKey LIKE @SearchTerm");
                        searchConditions.Add("r.RoleName LIKE @SearchTerm");
                        searchConditions.Add("r.RoleComment LIKE @SearchTerm");
                    }

                    if (searchConditions.Count > 0)
                    {
                        whereClause = $" WHERE {string.Join(" OR ", searchConditions)}";
                        parameters.Add("@SearchTerm", $"%{searchTerm}%");
                    }
                }

                var query = $@"
                    SELECT COUNT(*)
                    FROM MenuRoles mr
                    JOIN Menus m ON mr.MenuId = m.Id
                    JOIN Roles r ON mr.RoleId = r.Id
                    {whereClause}";

                // SQL 쿼리 로깅
                LogQuery(query, parameters);

                return await connection.ExecuteScalarAsync<int>(query, parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴-역할 매핑 총 개수 조회 중 오류 발생");
                throw;
            }
        }

        /// <summary>
        /// 페이지네이션을 적용하여 메뉴-역할 매핑 목록을 조회합니다.
        /// </summary>
        /// <param name="page">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지당 레코드 수</param>
        /// <param name="searchTerm">검색어 (선택사항)</param>
        /// <param name="searchFields">검색할 필드 (선택사항)</param>
        /// <returns>페이지네이션이 적용된 메뉴-역할 매핑 목록</returns>
        public async Task<IEnumerable<MenuRoleEntity>> GetPagedListAsync(int page, int pageSize, string searchTerm = null, string searchFields = null)
        {
            using var connection = CreateConnection();

            try
            {
                // 페이지 유효성 검사 및 보정
                page = page < 1 ? 1 : page;
                pageSize = pageSize < 1 ? 10 : pageSize;

                var whereClause = string.Empty;
                var parameters = new DynamicParameters();
                parameters.Add("@Offset", (page - 1) * pageSize);
                parameters.Add("@PageSize", pageSize);

                // 검색 조건이 있는 경우 WHERE 절 구성
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchConditions = new List<string>();

                    // 검색 필드가 지정된 경우 해당 필드만 검색
                    if (!string.IsNullOrWhiteSpace(searchFields))
                    {
                        var fields = searchFields.Split(',');
                        foreach (var field in fields)
                        {
                            switch (field.Trim().ToLower())
                            {
                                case "menuname":
                                case "menutitle":
                                    searchConditions.Add("m.Title LIKE @SearchTerm");
                                    break;
                                case "menukey":
                                    searchConditions.Add("m.MenuKey LIKE @SearchTerm");
                                    break;
                                case "rolename":
                                    searchConditions.Add("r.RoleName LIKE @SearchTerm");
                                    break;
                                case "rolecomment":
                                    searchConditions.Add("r.RoleComment LIKE @SearchTerm");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // 기본적으로 모든 관련 필드 검색
                        searchConditions.Add("m.Title LIKE @SearchTerm");
                        searchConditions.Add("m.MenuKey LIKE @SearchTerm");
                        searchConditions.Add("r.RoleName LIKE @SearchTerm");
                        searchConditions.Add("r.RoleComment LIKE @SearchTerm");
                    }

                    if (searchConditions.Count > 0)
                    {
                        whereClause = $" WHERE {string.Join(" OR ", searchConditions)}";
                        parameters.Add("@SearchTerm", $"%{searchTerm}%");
                    }
                }

                // 메뉴와 역할 정보를 함께 조회하는 페이지네이션 쿼리
                var query = $@"
                    SELECT mr.MenuId, mr.RoleId, 
                           m.MenuKey, m.Url, m.Title,
                           r.RoleName, r.RoleComment
                    FROM MenuRoles mr
                    JOIN Menus m ON mr.MenuId = m.Id
                    JOIN Roles r ON mr.RoleId = r.Id
                    {whereClause}
                    ORDER BY m.MenuOrder, r.RoleName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                // SQL 쿼리 로깅
                LogQuery(query, parameters);

                // 조인 쿼리 실행 및 객체 매핑
                var menuRoles = await connection.QueryAsync<MenuRoleEntity, MenuEntity, RoleEntity, MenuRoleEntity>(
                    query,
                    (menuRole, menu, role) =>
                    {
                        menuRole.Menu = menu;
                        menuRole.Role = role;
                        return menuRole;
                    },
                    parameters,
                    splitOn: "MenuKey,RoleName");

                return menuRoles;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "페이지네이션 적용 메뉴-역할 조회 중 오류 발생");
                throw;
            }
        }

        public async Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT m.*
                FROM Menus m
                INNER JOIN MenuRoles mr ON m.Id = mr.MenuId
                WHERE mr.RoleId = @RoleId                
                ORDER BY m.MenuOrder";

            var parameters = new { RoleId = roleId };

            LogQuery(query, parameters);

            return await connection.QueryAsync<MenuEntity>(query, parameters);
        }
    }
}