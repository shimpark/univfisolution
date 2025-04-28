using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;
using UnivFI.Domain.Models;
using UnivFI.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace UnivFI.Infrastructure.Repositories
{
    /// <summary>
    /// 메뉴 및 화면 구성 요소를 관리하는 리포지토리입니다.
    /// 시스템의 메뉴 구조, 네비게이션 및 접근 권한을 처리합니다.
    /// </summary>
    /// <remarks>
    /// 주요 기능:
    /// - 전체 메뉴 목록 조회 및 계층 구조 구성
    /// - 메뉴 생성, 수정, 삭제
    /// - 메뉴 트리 구성 및 정렬
    /// - 사용자 권한별 접근 가능한 메뉴 필터링
    /// - 부모-자식 메뉴 관계 관리
    /// - 메뉴 활성화/비활성화 상태 관리
    /// </remarks>
    public class MenuRepository : BaseRepository<MenuEntity, int, MenuRepository>, IMenuRepository
    {
        private const string TABLE_NAME = "Menus";

        public MenuRepository(IConnectionFactory connectionFactory, ILogger<MenuRepository> logger)
            : base(connectionFactory, logger, TABLE_NAME)
        {
        }

        /// <summary>
        /// 전체 메뉴를 MenuOrder 기준으로 정렬하여 조회합니다.
        /// </summary>
        /// <returns>정렬된 메뉴 목록</returns>
        public new async Task<IEnumerable<MenuEntity>> GetAllAsync()
        {
            using var connection = CreateConnection();

            // GetAllAsync는 ORDER BY를 지원하지 않으므로, MenuOrder로 정렬된 결과를 원하면 커스텀 쿼리를 사용
            var query = $@"SELECT * FROM {TableName} ORDER BY MenuOrder";

            // SQL 쿼리 로깅
            LogQuery(query, null);

            try
            {
                // TypeHandler가 Y/N 문자열을 Boolean으로 자동 변환
                return await connection.QueryAsync<MenuEntity>(query);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "메뉴 전체 조회 중 오류 발생");

                // 오류 발생 시 대체 방법으로 접근 (필요한 경우)
                if (ex.Message.Contains("String 'Y' was not recognized as a valid Boolean"))
                {
                    // 수동으로 매핑 처리
                    return await connection.QueryAsync<dynamic>(query)
                        .ContinueWith(t => t.Result.Select(item =>
                        {
                            var menu = new MenuEntity
                            {
                                Id = item.Id,
                                ParentId = item.ParentId,
                                MenuOrder = item.MenuOrder,
                                MenuKey = item.MenuKey,
                                Url = item.Url,
                                Title = item.Title,
                                Levels = item.Levels,
                                CreatedAt = item.CreatedAt,
                                UpdatedAt = item.UpdatedAt
                            };

                            // UseNewIcon 수동 변환
                            if (item.UseNewIcon != null)
                            {
                                string useNewIconStr = item.UseNewIcon.ToString();
                                menu.UseNewIcon = useNewIconStr.Equals("Y", StringComparison.OrdinalIgnoreCase);
                            }

                            return menu;
                        }));
                }

                throw; // 다른 오류인 경우 다시 throw
            }
        }

        /// <summary>
        /// 지정된 ID를 가진 메뉴를 조회합니다.
        /// </summary>
        /// <param name="id">조회할 메뉴 ID</param>
        /// <returns>조회된 메뉴 엔티티</returns>
        public async Task<MenuEntity> GetByIdAsync(int id)
        {
            using var connection = CreateConnection();

            // ID 기반 쿼리 로깅
            LogIdOperation("SELECT", id);

            return await connection.GetAsync<MenuEntity>(id);
        }

        /// <summary>
        /// 새로운 메뉴를 생성합니다.
        /// 생성/수정 시간을 자동으로 현재 시간으로 설정하고, UseNewIcon이 null인 경우 기본값을 설정합니다.
        /// </summary>
        /// <param name="menu">생성할 메뉴 정보가 담긴 엔티티</param>
        /// <returns>생성된 메뉴의 ID</returns>
        public async Task<int> CreateAsync(MenuEntity menu)
        {
            using var connection = CreateConnection();

            // 현재 시간을 기본값으로 설정
            if (menu.CreatedAt == null)
                menu.CreatedAt = DateTime.UtcNow;
            if (menu.UpdatedAt == null)
                menu.UpdatedAt = DateTime.UtcNow;

            // bit 타입에 대한 null 처리
            if (menu.UseNewIcon == null)
                menu.UseNewIcon = false;

            // 엔티티 작업 로깅
            LogEntityOperation("INSERT INTO", menu);

            return await connection.InsertAsync(menu);
        }

        /// <summary>
        /// 기존 메뉴 정보를 업데이트합니다.
        /// 수정 시간을 현재 시간으로 자동 설정하고, UseNewIcon이 null인 경우 기본값을 설정합니다.
        /// </summary>
        /// <param name="menu">업데이트할 메뉴 정보가 담긴 엔티티</param>
        /// <returns>업데이트 성공 여부</returns>
        public async Task<bool> UpdateAsync(MenuEntity menu)
        {
            using var connection = CreateConnection();

            // 업데이트 시 현재 시간으로 설정
            menu.UpdatedAt = DateTime.UtcNow;

            // bit 타입에 대한 null 처리
            if (menu.UseNewIcon == null)
                menu.UseNewIcon = false;

            // 엔티티 작업 로깅
            LogEntityOperation("UPDATE", menu);

            return await connection.UpdateAsync(menu);
        }

        /// <summary>
        /// 지정된 ID의 메뉴를 삭제합니다.
        /// 메뉴 삭제 전에 관련된 MenuRoles 테이블의 데이터를 먼저 삭제하여 무결성을 유지합니다.
        /// </summary>
        /// <param name="id">삭제할 메뉴 ID</param>
        /// <returns>삭제 성공 여부</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = CreateConnection();

            // 첫 번째로 해당 메뉴와 관련된 모든 역할 연결을 삭제
            // 외래 키 제약조건 때문에 먼저 자식 레코드를 삭제해야 함
            var deleteMenuRolesQuery = @"
                DELETE FROM MenuRoles
                WHERE MenuId = @Id";

            // SQL 쿼리 로깅
            LogQuery(deleteMenuRolesQuery, new { Id = id });

            await connection.ExecuteAsync(deleteMenuRolesQuery, new { Id = id });

            // 메뉴 삭제
            LogIdOperation("SELECT", id);
            var menu = await connection.GetAsync<MenuEntity>(id);

            if (menu == null)
                return false;

            LogIdOperation("DELETE FROM", id);
            return await connection.DeleteAsync(menu);
        }

        /// <summary>
        /// 특정 역할에 할당된 모든 메뉴를 조회합니다.
        /// </summary>
        /// <param name="roleId">조회할 역할 ID</param>
        /// <returns>해당 역할에 할당된 메뉴 목록</returns>
        public async Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId)
        {
            using var connection = CreateConnection();

            var query = $@"
                SELECT m.*
                FROM {TableName} m
                INNER JOIN MenuRoles mr ON m.Id = mr.MenuId
                WHERE mr.RoleId = @RoleId
                ORDER BY m.MenuOrder, m.Id";

            var parameters = new { RoleId = roleId };

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            try
            {
                // TypeHandler가 Y/N 문자열을 Boolean으로 자동 변환
                return await connection.QueryAsync<MenuEntity>(query, parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "역할 ID {RoleId}에 대한 메뉴 조회 중 오류 발생", roleId);

                // 오류 발생 시 대체 방법으로 접근 (필요한 경우)
                if (ex.Message.Contains("String 'Y' was not recognized as a valid Boolean"))
                {
                    // 수동으로 매핑 처리
                    return await connection.QueryAsync<dynamic>(query, parameters)
                        .ContinueWith(t => t.Result.Select(item =>
                        {
                            var menu = new MenuEntity
                            {
                                Id = item.Id,
                                ParentId = item.ParentId,
                                MenuOrder = item.MenuOrder,
                                MenuKey = item.MenuKey,
                                Url = item.Url,
                                Title = item.Title,
                                Levels = item.Levels,
                                CreatedAt = item.CreatedAt,
                                UpdatedAt = item.UpdatedAt
                            };

                            // UseNewIcon 수동 변환
                            if (item.UseNewIcon != null)
                            {
                                string useNewIconStr = item.UseNewIcon.ToString();
                                menu.UseNewIcon = useNewIconStr.Equals("Y", StringComparison.OrdinalIgnoreCase);
                            }

                            return menu;
                        }));
                }

                throw; // 다른 오류인 경우 다시 throw
            }
        }

        /// <summary>
        /// 특정 사용자가 접근할 수 있는 모든 메뉴를 조회합니다.
        /// 사용자의 역할에 따라 접근 가능한 메뉴를 필터링합니다.
        /// </summary>
        /// <param name="userId">조회할 사용자 ID</param>
        /// <returns>해당 사용자가 접근 가능한 메뉴 목록</returns>
        public async Task<IEnumerable<MenuEntity>> GetMenusByUserIdAsync(int userId)
        {
            using var connection = CreateConnection();

            var query = $@"
                SELECT DISTINCT m.*
                FROM {TableName} m
                INNER JOIN MenuRoles mr ON m.Id = mr.MenuId
                INNER JOIN UserRoles ur ON mr.RoleId = ur.RoleId
                WHERE ur.UserId = @UserId
                ORDER BY m.MenuOrder, m.Id";

            var parameters = new { UserId = userId };

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            try
            {
                // TypeHandler가 Y/N 문자열을 Boolean으로 자동 변환
                return await connection.QueryAsync<MenuEntity>(query, parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "사용자 ID {UserId}에 대한 메뉴 조회 중 오류 발생", userId);

                // 오류 발생 시 대체 방법으로 접근 (필요한 경우)
                if (ex.Message.Contains("String 'Y' was not recognized as a valid Boolean"))
                {
                    // 수동으로 매핑 처리
                    return await connection.QueryAsync<dynamic>(query, parameters)
                        .ContinueWith(t => t.Result.Select(item =>
                        {
                            var menu = new MenuEntity
                            {
                                Id = item.Id,
                                ParentId = item.ParentId,
                                MenuOrder = item.MenuOrder,
                                MenuKey = item.MenuKey,
                                Url = item.Url,
                                Title = item.Title,
                                Levels = item.Levels,
                                CreatedAt = item.CreatedAt,
                                UpdatedAt = item.UpdatedAt
                            };

                            // UseNewIcon 수동 변환
                            if (item.UseNewIcon != null)
                            {
                                string useNewIconStr = item.UseNewIcon.ToString();
                                menu.UseNewIcon = useNewIconStr.Equals("Y", StringComparison.OrdinalIgnoreCase);
                            }

                            return menu;
                        }));
                }

                throw; // 다른 오류인 경우 다시 throw
            }
        }

        /// <summary>
        /// 페이징 처리된 메뉴 목록을 조회합니다.
        /// 검색어와 검색 필드를 기준으로 필터링할 수 있습니다.
        /// </summary>
        /// <param name="pageNumber">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <param name="searchTerm">검색어</param>
        /// <param name="searchFields">검색할 필드 (쉼표로 구분, 기본값: MenuKey,Title,Url)</param>
        /// <returns>페이징된 메뉴 목록과 전체 항목 수</returns>
        public async Task<(IEnumerable<MenuEntity> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string searchTerm = null, string searchFields = null)
        {
            using var connection = CreateConnection();

            // 검색어와 검색 필드가 없으면 WHERE 절 없음
            string whereClause = "";

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchFieldsList = string.IsNullOrEmpty(searchFields)
                    ? new List<string> { "MenuKey", "Title", "Url" }
                    : searchFields.Split(',').ToList();

                var conditions = new List<string>();

                foreach (var field in searchFieldsList)
                {
                    conditions.Add($"{field} LIKE @SearchTerm");
                }

                if (conditions.Any())
                {
                    whereClause = $" WHERE {string.Join(" OR ", conditions)}";
                }
            }

            // 전체 개수 쿼리
            var countQuery = $@"
                SELECT COUNT(*)
                FROM {TableName}
                {whereClause}";

            // 페이징 쿼리
            var pageQuery = $@"
                SELECT *
                FROM {TableName}
                {whereClause}
                ORDER BY MenuOrder, Id
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            var parameters = new DynamicParameters();
            parameters.Add("Offset", (pageNumber - 1) * pageSize);
            parameters.Add("PageSize", pageSize);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                parameters.Add("SearchTerm", $"%{searchTerm}%");
            }

            // 전체 개수 쿼리 로깅
            LogQuery(countQuery, parameters);

            // 전체 개수 조회
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

            // 페이지 쿼리 로깅
            LogQuery(pageQuery, parameters);

            // 페이지 데이터 조회
            var items = await connection.QueryAsync<MenuEntity>(pageQuery, parameters);

            return (items, totalCount);
        }

        /// <summary>
        /// 고급 검색 조건을 적용한 페이징된 메뉴 목록을 조회합니다.
        /// 다양한 연산자(equals, contains, startswith, endswith)와 논리 조건(AND, OR)으로 세부 검색이 가능합니다.
        /// </summary>
        /// <param name="pageNumber">페이지 번호</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <param name="searchCriteria">검색 조건 목록</param>
        /// <param name="searchLogicalOperator">검색 조건 간 논리 연산자 (AND 또는 OR)</param>
        /// <returns>검색 조건에 맞는 페이징된 메뉴 목록과 전체 항목 수</returns>
        public async Task<(IEnumerable<MenuEntity> Items, int TotalCount)> GetPagedWithAdvancedSearchAsync(
            int pageNumber,
            int pageSize,
            List<SearchCriteria> searchCriteria,
            string searchLogicalOperator = "AND")
        {
            using var connection = CreateConnection();

            string whereClause = "";
            var parameters = new DynamicParameters();
            parameters.Add("Offset", (pageNumber - 1) * pageSize);
            parameters.Add("PageSize", pageSize);

            // 고급 검색 조건이 있는 경우
            if (searchCriteria != null && searchCriteria.Any())
            {
                var conditions = new List<string>();
                for (int i = 0; i < searchCriteria.Count; i++)
                {
                    var criteria = searchCriteria[i];
                    string paramName = $"Value{i}";
                    string condition = "";

                    switch (criteria.Operator.ToLower())
                    {
                        case "equals":
                            condition = $"{criteria.Field} = @{paramName}";
                            parameters.Add(paramName, criteria.Value);
                            break;
                        case "contains":
                            condition = $"{criteria.Field} LIKE @{paramName}";
                            parameters.Add(paramName, $"%{criteria.Value}%");
                            break;
                        case "startswith":
                            condition = $"{criteria.Field} LIKE @{paramName}";
                            parameters.Add(paramName, $"{criteria.Value}%");
                            break;
                        case "endswith":
                            condition = $"{criteria.Field} LIKE @{paramName}";
                            parameters.Add(paramName, $"%{criteria.Value}");
                            break;
                        default:
                            condition = $"{criteria.Field} LIKE @{paramName}";
                            parameters.Add(paramName, $"%{criteria.Value}%");
                            break;
                    }
                    conditions.Add(condition);
                }

                if (conditions.Any())
                {
                    string logicalOperator = searchLogicalOperator == "OR" ? " OR " : " AND ";
                    whereClause = $" WHERE {string.Join(logicalOperator, conditions)}";
                }
            }

            // 전체 개수 쿼리
            var countQuery = $@"
                SELECT COUNT(*)
                FROM {TableName}
                {whereClause}";

            // 페이징 쿼리
            var pageQuery = $@"
                SELECT *
                FROM {TableName}
                {whereClause}
                ORDER BY MenuOrder, Id
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            // 검색 조건 로깅
            Logger.LogDebug($"고급 검색 조건: {(searchCriteria != null ? string.Join(", ", searchCriteria.Select(c => $"{c.Field} {c.Operator} {c.Value}")) : "없음")}");

            // 전체 개수 쿼리 로깅
            LogQuery(countQuery, parameters);

            // 전체 개수 조회
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

            // 페이지 쿼리 로깅
            LogQuery(pageQuery, parameters);

            // 페이지 데이터 조회
            var items = await connection.QueryAsync<MenuEntity>(pageQuery, parameters);

            return (items, totalCount);
        }

        /// <summary>
        /// 트리 뷰에 적합한 형태로 모든 메뉴를 계층별 순서로 조회합니다.
        /// Levels와 MenuOrder에 따라 정렬하여 메뉴 계층 구조를 구성할 수 있게 합니다.
        /// </summary>
        /// <returns>계층 구조에 적합하게 정렬된 메뉴 목록</returns>
        public async Task<IEnumerable<MenuEntity>> GetAllForTreeAsync()
        {
            using var connection = CreateConnection();

            // 트리 뷰를 위해 Levels, MenuOrder로 정렬하여 조회
            var query = $@"
                SELECT * FROM {TableName} 
                ORDER BY Levels, MenuOrder, Id";

            // SQL 쿼리 로깅
            LogQuery(query, null);

            return await connection.QueryAsync<MenuEntity>(query);
        }

        /// <summary>
        /// 특정 메뉴에 역할을 할당합니다.
        /// 이미 할당된 경우는 중복 할당하지 않고 성공으로 처리합니다.
        /// </summary>
        /// <param name="menuId">역할을 할당할 메뉴 ID</param>
        /// <param name="roleId">메뉴에 할당할 역할 ID</param>
        /// <returns>할당 성공 여부</returns>
        public async Task<bool> AssignRoleToMenuAsync(int menuId, int roleId)
        {
            using var connection = CreateConnection();

            // 이미 존재하는지 확인
            var exists = await MenuHasRoleAsync(menuId, roleId);
            if (exists)
                return true; // 이미 할당된 경우 성공으로 간주

            try
            {
                // 메뉴-역할 관계 추가
                var menuRole = new MenuRoleEntity
                {
                    MenuId = menuId,
                    RoleId = roleId
                };

                // 엔티티 작업 로깅
                LogEntityOperation("INSERT INTO", menuRole);

                await connection.InsertAsync(menuRole);
                return true;
            }
            catch (Exception ex)
            {
                // 외래 키 제약 조건 위반 등의 오류 처리
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

                var query = @"
                    DELETE FROM MenuRoles
                    WHERE MenuId = @MenuId AND RoleId = @RoleId";

                var parameters = new { MenuId = menuId, RoleId = roleId };

                // SQL 쿼리 로깅
                LogQuery(query, parameters);

                await connection.ExecuteAsync(query, parameters);
                return true;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "메뉴에서 역할 제거 중 오류 발생 (메뉴 ID: {MenuId}, 역할 ID: {RoleId})", menuId, roleId);
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

        public async Task<(IEnumerable<MenuEntity> Items, int TotalCount)> GetHierarchicalMenuDataAsync(
            int page,
            int pageSize,
            string searchTerm = "",
            string sortColumn = "",
            bool ascending = true)
        {
            using var connection = CreateConnection();

            // 검색 조건 생성
            var whereClause = string.IsNullOrEmpty(searchTerm)
                ? "WHERE 1=1"
                : "WHERE 1=1 AND (MenuKey LIKE @SearchTerm OR Title LIKE @SearchTerm)";

            // 정렬 조건 생성
            var orderByClause = string.IsNullOrEmpty(sortColumn)
                ? "ORDER BY MenuOrder"
                : $"ORDER BY {sortColumn} {(ascending ? "ASC" : "DESC")}";

            // 총 개수 쿼리
            var countQuery = $@"
                SELECT COUNT(*)
                FROM Menus
                {whereClause}";

            // 데이터 쿼리
            var dataQuery = $@"
                WITH RecursiveCTE AS (
                    SELECT 
                        Id, MenuKey, Title, ParentId, Url, MenuOrder, 
                        Levels, UseNewIcon, CreatedAt, UpdatedAt,
                        CAST(MenuOrder AS VARCHAR(MAX)) AS Path
                    FROM Menus
                    WHERE ParentId IS NULL
                    
                    UNION ALL
                    
                    SELECT 
                        m.Id, m.MenuKey, m.Title, m.ParentId, m.Url, m.MenuOrder,
                        m.Levels, m.UseNewIcon, m.CreatedAt, m.UpdatedAt,
                        CAST(r.Path + '.' + CAST(m.MenuOrder AS VARCHAR(10)) AS VARCHAR(MAX))
                    FROM Menus m
                    INNER JOIN RecursiveCTE r ON m.ParentId = r.Id
                )
                SELECT *
                FROM RecursiveCTE
                {whereClause}
                {orderByClause}
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            var parameters = new DynamicParameters();
            parameters.Add("@Offset", (page - 1) * pageSize);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@SearchTerm", !string.IsNullOrEmpty(searchTerm) ? $"%{searchTerm}%" : null);

            // 쿼리 실행
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);
            var items = await connection.QueryAsync<MenuEntity>(dataQuery, parameters);

            return (items, totalCount);
        }
    }
}