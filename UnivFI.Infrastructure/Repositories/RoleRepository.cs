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
using Microsoft.Extensions.Logging;
using UnivFI.Infrastructure.Helpers;

namespace UnivFI.Infrastructure.Repositories
{
    /// <summary>
    /// 역할(Role) 관련 데이터베이스 작업을 처리하는 리포지토리입니다.
    /// 시스템 내 역할 권한의 생성, 조회, 수정 및 삭제 기능을 제공합니다.
    /// </summary>
    /// <remarks>
    /// 주요 기능:
    /// - 역할 목록 조회 및 페이징 처리
    /// - 역할 생성, 수정, 삭제
    /// - 역할명 기반 검색
    /// - 역할별 메뉴 권한 관리
    /// - 역할에 할당된 사용자 조회
    /// </remarks>
    [Table("Roles")]
    public class RoleRepository : BaseRepository<RoleEntity, int, RoleRepository>, IRoleRepository
    {
        private readonly ILogger<RoleRepository> _logger;

        public RoleRepository(IConnectionFactory connectionFactory, ILogger<RoleRepository> logger)
            : base(connectionFactory, logger, "Roles")
        {
            _logger = logger;
        }

        /// <summary>
        /// 모든 역할 목록을 조회합니다.
        /// </summary>
        /// <returns>역할 목록</returns>
        public async Task<IEnumerable<RoleEntity>> GetAllAsync()
        {
            try
            {
                using var connection = CreateConnection();
                return await connection.GetAllAsync<RoleEntity>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "모든 역할을 가져오는 중 오류가 발생했습니다.");
                throw;
            }
        }

        /// <summary>
        /// ID로 특정 역할을 조회합니다.
        /// </summary>
        /// <param name="id">역할 ID</param>
        /// <returns>조회된 역할 엔티티</returns>
        public async Task<RoleEntity> GetByIdAsync(int id)
        {
            try
            {
                using var connection = CreateConnection();
                return await connection.GetAsync<RoleEntity>(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {Id}로 역할을 가져오는 중 오류가 발생했습니다.", id);
                throw;
            }
        }

        /// <summary>
        /// 새로운 역할을 생성합니다.
        /// </summary>
        /// <param name="role">생성할 역할 정보가 담긴 엔티티</param>
        /// <returns>생성된 역할의 ID</returns>
        public async Task<int> CreateAsync(RoleEntity role)
        {
            try
            {
                // 생성 시간 설정
                role.CreatedAt = DateTime.Now;

                using var connection = CreateConnection();
                return await connection.InsertAsync(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 '{RoleName}'을 생성하는 중 오류가 발생했습니다.", role.RoleName);
                throw;
            }
        }

        /// <summary>
        /// 기존 역할 정보를 업데이트합니다.
        /// </summary>
        /// <param name="role">업데이트할 역할 정보가 담긴 엔티티</param>
        /// <returns>업데이트 성공 여부</returns>
        public async Task<bool> UpdateAsync(RoleEntity role)
        {
            try
            {
                // 업데이트 시간 설정
                role.UpdatedAt = DateTime.Now;

                using var connection = CreateConnection();
                return await connection.UpdateAsync(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {Id}를 업데이트하는 중 오류가 발생했습니다.", role.Id);
                throw;
            }
        }

        /// <summary>
        /// 지정된 ID의 역할을 삭제합니다.
        /// 역할 삭제 전에 관련된 UserRoles, MenuRoles 테이블의 데이터도 함께 삭제하여 무결성을 유지합니다.
        /// </summary>
        /// <param name="id">삭제할 역할 ID</param>
        /// <returns>삭제 성공 여부</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var connection = CreateConnection();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. 역할에 관련된 UserRoles 데이터 삭제
                    await connection.ExecuteAsync(
                        "DELETE FROM UserRoles WHERE RoleId = @RoleId",
                        new { RoleId = id },
                        transaction);

                    // 2. 역할에 관련된 MenuRoles 데이터 삭제
                    await connection.ExecuteAsync(
                        "DELETE FROM MenuRoles WHERE RoleId = @RoleId",
                        new { RoleId = id },
                        transaction);

                    // 3. 역할 엔티티 조회
                    var role = await connection.GetAsync<RoleEntity>(id, transaction);
                    if (role == null)
                        return false;

                    // 4. 역할 삭제
                    var result = await connection.DeleteAsync(role, transaction);

                    transaction.Commit();
                    return result;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "역할 삭제 트랜잭션 중 오류가 발생했습니다. 롤백 수행됨. 역할 ID: {Id}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {Id}를 삭제하는 중 오류가 발생했습니다.", id);
                throw;
            }
        }

        /// <summary>
        /// 특정 사용자에게 할당된 모든 역할을 조회합니다.
        /// </summary>
        /// <param name="userId">조회할 사용자 ID</param>
        /// <returns>사용자에게 할당된 역할 목록</returns>
        public async Task<IEnumerable<RoleEntity>> GetRolesByUserIdAsync(int userId)
        {
            try
            {
                using var connection = CreateConnection();
                var userRoles = await connection.QueryAsync<RoleEntity>(@"
                    SELECT r.*
                    FROM [Roles] r
                    INNER JOIN [UserRoles] ur ON r.Id = ur.RoleId
                    WHERE ur.UserId = @UserId", new { UserId = userId });

                return userRoles ?? Enumerable.Empty<RoleEntity>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 ID {UserId}의 역할을 조회하는 중 오류가 발생했습니다.", userId);
                throw;
            }
        }

        /// <summary>
        /// 특정 메뉴에 접근 가능한 모든 역할을 조회합니다.
        /// </summary>
        /// <param name="menuId">조회할 메뉴 ID</param>
        /// <returns>메뉴에 접근 가능한 역할 목록</returns>
        public async Task<IEnumerable<RoleEntity>> GetRolesByMenuIdAsync(int menuId)
        {
            try
            {
                using var connection = CreateConnection();
                var menuRoles = await connection.QueryAsync<RoleEntity>(@"
                    SELECT r.*
                    FROM [Roles] r
                    INNER JOIN [MenuRoles] rm ON r.Id = rm.RoleId
                    WHERE rm.MenuId = @MenuId", new { MenuId = menuId });

                return menuRoles ?? Enumerable.Empty<RoleEntity>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메뉴 ID {MenuId}의 역할을 조회하는 중 오류가 발생했습니다.", menuId);
                throw;
            }
        }

        /// <summary>
        /// 페이징 처리된 역할 목록을 조회합니다.
        /// 검색어와 검색 필드를 기준으로 필터링할 수 있습니다.
        /// </summary>
        /// <param name="pageNumber">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <param name="searchTerm">검색어</param>
        /// <param name="searchFields">검색할 필드 (쉼표로 구분, 기본값: RoleName)</param>
        /// <returns>페이징된 역할 목록과 전체 항목 수</returns>
        public async Task<(IEnumerable<RoleEntity> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string searchTerm = null, string searchFields = null)
        {
            try
            {
                using var connection = CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Offset", (pageNumber - 1) * pageSize);
                parameters.Add("@PageSize", pageSize);

                var whereClause = BuildSearchWhereClause(searchTerm, searchFields, parameters);

                var totalCount = await connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM [Roles] {whereClause}", parameters);

                var items = await connection.QueryAsync<RoleEntity>($@"
                    SELECT *
                    FROM [Roles]
                    {whereClause}
                    ORDER BY RoleName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY", parameters);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "페이징된 역할 목록을 조회하는 중 오류가 발생했습니다. 페이지: {PageNumber}, 크기: {PageSize}",
                    pageNumber, pageSize);
                throw;
            }
        }

        private string BuildSearchWhereClause(string searchTerm, string searchFields, DynamicParameters parameters)
        {
            var whereClause = string.Empty;

            if (!string.IsNullOrEmpty(searchTerm) && !string.IsNullOrEmpty(searchFields))
            {
                var searchConditions = searchFields.Split(',')
                    .Select(field => $"{field.Trim()} LIKE @SearchTerm")
                    .ToList();

                whereClause = $"WHERE {string.Join(" OR ", searchConditions)}";
                parameters.Add("@SearchTerm", $"%{searchTerm}%");
            }

            return whereClause;
        }

        /// <summary>
        /// 역할명으로 특정 역할을 조회합니다.
        /// </summary>
        /// <param name="roleName">조회할 역할명</param>
        /// <returns>조회된 역할 엔티티</returns>
        public async Task<RoleEntity> GetByNameAsync(string roleName)
        {
            try
            {
                using var connection = CreateConnection();
                var roles = await connection.QueryAsync<RoleEntity>(
                    "SELECT * FROM [Roles] WHERE RoleName = @RoleName",
                    new { RoleName = roleName });

                return roles.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할명 '{RoleName}'으로 역할을 조회하는 중 오류가 발생했습니다.", roleName);
                throw;
            }
        }

        /// <summary>
        /// 특정 역할에 사용자를 할당합니다.
        /// 이미 할당된 경우 중복 할당하지 않고 성공으로 처리합니다.
        /// </summary>
        /// <param name="userId">할당할 사용자 ID</param>
        /// <param name="roleId">역할 ID</param>
        /// <returns>할당 성공 여부</returns>
        public async Task<bool> AssignUserToRoleAsync(int userId, int roleId)
        {
            try
            {
                using var connection = CreateConnection();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 역할 존재 여부 확인
                    var role = await connection.GetAsync<RoleEntity>(roleId, transaction);
                    if (role == null)
                    {
                        return false;
                    }

                    // 이미 할당되어 있는지 확인
                    var exists = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId",
                        new { UserId = userId, RoleId = roleId },
                        transaction);

                    if (exists > 0)
                    {
                        transaction.Commit();
                        return true;
                    }

                    // 새로운 할당 추가
                    await connection.ExecuteAsync(
                        "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
                        new { UserId = userId, RoleId = roleId },
                        transaction);

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 ID {UserId}를 역할 ID {RoleId}에 할당하는 중 오류가 발생했습니다.", userId, roleId);
                throw;
            }
        }

        /// <summary>
        /// 역할에서 사용자를 제거합니다.
        /// </summary>
        /// <param name="roleId">역할 ID</param>
        /// <param name="userId">제거할 사용자 ID</param>
        /// <returns>제거 성공 여부</returns>
        public async Task<bool> RemoveUserFromRoleAsync(int roleId, int userId)
        {
            const string sql = @"
                DELETE FROM UserRoles 
                WHERE RoleId = @RoleId AND UserId = @UserId";

            try
            {
                using var connection = CreateConnection();
                var affectedRows = await connection.ExecuteAsync(sql, new { RoleId = roleId, UserId = userId });
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}에서 사용자 ID {UserId}를 제거하는 중 오류가 발생했습니다.", roleId, userId);
                throw;
            }
        }

        /// <summary>
        /// 역할에서 메뉴를 제거합니다.
        /// </summary>
        /// <param name="roleId">역할 ID</param>
        /// <param name="menuId">제거할 메뉴 ID</param>
        /// <returns>제거 성공 여부</returns>
        public async Task<bool> RemoveMenuFromRoleAsync(int roleId, int menuId)
        {
            const string sql = @"
                DELETE FROM MenuRoles 
                WHERE RoleId = @RoleId AND MenuId = @MenuId";

            try
            {
                using var connection = CreateConnection();
                var affectedRows = await connection.ExecuteAsync(sql, new { RoleId = roleId, MenuId = menuId });
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}에서 메뉴 ID {MenuId}를 제거하는 중 오류가 발생했습니다.", roleId, menuId);
                throw;
            }
        }

        public async Task<(IEnumerable<MenuEntity> Menus, int TotalCount)> GetAssignedMenusPagedAsync(
            int roleId, int page, int pageSize, string searchTerm)
        {
            try
            {
                using var connection = CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", roleId);
                parameters.Add("@Offset", (page - 1) * pageSize);
                parameters.Add("@PageSize", pageSize);

                var whereClause = "WHERE rm.RoleId = @RoleId";
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    whereClause += " AND (m.Title LIKE @SearchTerm OR m.Url LIKE @SearchTerm)";
                    parameters.Add("@SearchTerm", $"%{searchTerm}%");
                }

                // 전체 개수 조회
                var countSql = $@"
                    SELECT COUNT(*)
                    FROM Menus m
                    INNER JOIN MenuRoles rm ON m.Id = rm.MenuId
                    {whereClause}";

                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // 페이징된 데이터 조회
                var sql = $@"
                    SELECT m.*
                    FROM Menus m
                    INNER JOIN MenuRoles rm ON m.Id = rm.MenuId
                    {whereClause}
                    ORDER BY m.Title
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var menus = await connection.QueryAsync<MenuEntity>(sql, parameters);
                return (menus, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}의 메뉴 목록을 조회하는 중 오류가 발생했습니다.", roleId);
                throw;
            }
        }

        public async Task<(IEnumerable<UserEntity> Users, int TotalCount)> GetAssignedUsersPagedAsync(
            int roleId, int page, int pageSize, string searchTerm)
        {
            try
            {
                using var connection = CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", roleId);
                parameters.Add("@Offset", (page - 1) * pageSize);
                parameters.Add("@PageSize", pageSize);

                var whereClause = "WHERE ur.RoleId = @RoleId";
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    whereClause += " AND (u.Name LIKE @SearchTerm OR u.Email LIKE @SearchTerm)";
                    parameters.Add("@SearchTerm", $"%{searchTerm}%");
                }

                // 전체 개수 조회
                var countSql = $@"
                    SELECT COUNT(*)
                    FROM Users u
                    INNER JOIN UserRoles ur ON u.Id = ur.UserId
                    {whereClause}";

                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // 페이징된 데이터 조회
                var sql = $@"
                    SELECT u.*
                    FROM Users u
                    INNER JOIN UserRoles ur ON u.Id = ur.UserId
                    {whereClause}
                    ORDER BY u.Name
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var users = await connection.QueryAsync<UserEntity>(sql, parameters);
                return (users, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}의 사용자 목록을 조회하는 중 오류가 발생했습니다.", roleId);
                throw;
            }
        }
    }
}