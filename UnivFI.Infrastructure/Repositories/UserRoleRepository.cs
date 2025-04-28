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
    /// 사용자-역할 연결 관계를 관리하는 리포지토리입니다.
    /// 사용자와 역할 간의 매핑 정보를 처리합니다.
    /// </summary>
    /// <remarks>
    /// 주요 기능:
    /// - 사용자별 할당된 역할 조회
    /// - 역할별 할당된 사용자 조회
    /// - 사용자에 역할 할당 및 제거
    /// - 다중 역할 할당 및 일괄 처리
    /// - 사용자의 역할 기반 권한 확인
    /// </remarks>
    public class UserRoleRepository : BaseRepository<UserRoleEntity, object, UserRoleRepository>, IUserRoleRepository
    {
        private const string TABLE_NAME = "UserRoles";

        public UserRoleRepository(IConnectionFactory connectionFactory, ILogger<UserRoleRepository> logger)
            : base(connectionFactory, logger, TABLE_NAME)
        {
        }

        /// <summary>
        /// 모든 사용자-역할 매핑 정보를 조회합니다.
        /// 사용자와 역할 정보를 함께 로드합니다.
        /// </summary>
        /// <returns>모든 사용자-역할 매핑 목록</returns>
        public new async Task<IEnumerable<UserRoleEntity>> GetAllAsync()
        {
            using var connection = CreateConnection();

            try
            {
                // 사용자와 역할 정보를 함께 조회하는 조인 쿼리
                var query = @"
                    SELECT 
                        ur.UserId, ur.RoleId, 
                        u.UserName, u.Name, u.Email, 
                        r.RoleName, r.RoleComment
                    FROM UserRoles ur
                    JOIN Users u ON ur.UserId = u.Id
                    JOIN Roles r ON ur.RoleId = r.Id
                    ORDER BY u.UserName, r.RoleName";

                // SQL 쿼리 로깅
                LogQuery(query, new { });

                // 조인 쿼리 실행 및 객체 매핑
                var userRoles = await connection.QueryAsync<UserRoleEntity, UserEntity, RoleEntity, UserRoleEntity>(
                    query,
                    (userRole, user, role) =>
                    {
                        userRole.User = user;
                        userRole.Role = role;
                        return userRole;
                    },
                    splitOn: "UserName,RoleName");

                return userRoles;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "모든 사용자-역할 매핑 조회 중 오류 발생");
                throw;
            }
        }

        /// <summary>
        /// 사용자-역할 매핑에 관련된 사용자 및 역할 정보를 로드합니다.
        /// </summary>
        /// <param name="connection">데이터베이스 연결</param>
        /// <param name="userRoles">사용자-역할 매핑 목록</param>
        private async Task LoadUserRoleNavigation(IDbConnection connection, IEnumerable<UserRoleEntity> userRoles)
        {
            if (!userRoles.Any()) return;

            var userIds = userRoles.Select(ur => ur.UserId).Distinct().ToList();
            var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();

            // 사용자 정보 로드 쿼리
            var userQuery = "SELECT * FROM Users WHERE Id IN @Ids";
            var userParams = new { Ids = userIds };

            // SQL 쿼리 로깅
            LogQuery(userQuery, userParams);

            // 사용자 정보 로드
            var users = await connection.QueryAsync<UserEntity>(userQuery, userParams);

            // 역할 정보 로드 쿼리
            var roleQuery = "SELECT * FROM Roles WHERE Id IN @Ids";
            var roleParams = new { Ids = roleIds };

            // SQL 쿼리 로깅
            LogQuery(roleQuery, roleParams);

            // 역할 정보 로드
            var roles = await connection.QueryAsync<RoleEntity>(roleQuery, roleParams);

            var userDict = users.ToDictionary(u => u.Id);
            var roleDict = roles.ToDictionary(r => r.Id);

            // 탐색 속성 설정
            foreach (var userRole in userRoles)
            {
                if (userDict.TryGetValue(userRole.UserId, out var user))
                {
                    userRole.User = user;
                }

                if (roleDict.TryGetValue(userRole.RoleId, out var role))
                {
                    userRole.Role = role;
                }
            }
        }

        /// <summary>
        /// 사용자에게 역할을 할당합니다.
        /// 이미 할당된 경우 중복 할당하지 않고 성공으로 처리합니다.
        /// </summary>
        /// <param name="userId">역할을 할당할 사용자 ID</param>
        /// <param name="roleId">할당할 역할 ID</param>
        /// <returns>할당 성공 여부</returns>
        public async Task<bool> AssignRoleToUserAsync(int userId, int roleId)
        {
            try
            {
                using var connection = CreateConnection();

                // 사용자 존재 확인
                LogIdOperation("SELECT", userId);
                var userExists = await connection.GetAsync<UserEntity>(userId) != null;

                // 역할 존재 확인
                LogIdOperation("SELECT", roleId);
                var roleExists = await connection.GetAsync<RoleEntity>(roleId) != null;

                // 둘 다 존재하지 않으면 실패
                if (!userExists || !roleExists)
                {
                    Logger.LogWarning("사용자 역할 할당 실패: 사용자 ID {UserId} 또는 역할 ID {RoleId}가 존재하지 않음", userId, roleId);
                    return false;
                }

                // 이미 할당되었는지 확인
                var existsQuery = @"
                    SELECT COUNT(1)
                    FROM UserRoles
                    WHERE UserId = @UserId AND RoleId = @RoleId";

                var parameters = new { UserId = userId, RoleId = roleId };

                // SQL 쿼리 로깅
                LogQuery(existsQuery, parameters);

                var exists = await connection.ExecuteScalarAsync<int>(existsQuery, parameters) > 0;

                if (exists)
                {
                    Logger.LogDebug("이미 사용자 ID {UserId}에 역할 ID {RoleId}가 할당되어 있음", userId, roleId);
                    return true; // 이미 존재하면 성공으로 간주
                }

                // 새로 할당
                var userRole = new UserRoleEntity
                {
                    UserId = userId,
                    RoleId = roleId
                };

                // 엔티티 작업 로깅
                LogEntityOperation("INSERT INTO", userRole);

                await connection.InsertAsync(userRole);
                return true;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "사용자 역할 할당 중 오류 발생 (사용자 ID: {UserId}, 역할 ID: {RoleId})", userId, roleId);
            }
        }

        /// <summary>
        /// 사용자로부터 역할을 제거합니다.
        /// </summary>
        /// <param name="userId">역할을 제거할 사용자 ID</param>
        /// <param name="roleId">제거할 역할 ID</param>
        /// <returns>제거 성공 여부</returns>
        public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
        {
            try
            {
                using var connection = CreateConnection();

                // 삭제 쿼리 작성
                var query = @"
                    DELETE FROM UserRoles
                    WHERE UserId = @UserId AND RoleId = @RoleId";

                var parameters = new { UserId = userId, RoleId = roleId };

                // SQL 쿼리 로깅
                LogQuery(query, parameters);

                // 실행
                await connection.ExecuteAsync(query, parameters);
                return true;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "사용자 역할 제거 중 오류 발생 (사용자 ID: {UserId}, 역할 ID: {RoleId})", userId, roleId);
            }
        }

        /// <summary>
        /// 사용자가 특정 역할을 가지고 있는지 확인합니다.
        /// </summary>
        /// <param name="userId">확인할 사용자 ID</param>
        /// <param name="roleId">확인할 역할 ID</param>
        /// <returns>사용자의 역할 보유 여부</returns>
        public async Task<bool> UserHasRoleAsync(int userId, int roleId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT COUNT(1)
                FROM UserRoles
                WHERE UserId = @UserId AND RoleId = @RoleId";

            var parameters = new { UserId = userId, RoleId = roleId };

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            var count = await connection.ExecuteScalarAsync<int>(query, parameters);
            return count > 0;
        }

        /// <summary>
        /// 특정 사용자에게 할당된 모든 역할 매핑을 조회합니다.
        /// 사용자 및 역할 정보를 함께 로드합니다.
        /// </summary>
        /// <param name="userId">조회할 사용자 ID</param>
        /// <returns>사용자에게 할당된 역할 매핑 목록</returns>
        public async Task<IEnumerable<UserRoleEntity>> GetByUserIdAsync(int userId)
        {
            using var connection = CreateConnection();

            var query = $@"
                SELECT * FROM {TableName} WHERE UserId = @UserId";

            var parameters = new { UserId = userId };

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            var userRoles = await connection.QueryAsync<UserRoleEntity>(query, parameters);

            // 탐색 속성 채우기
            await LoadUserRoleNavigation(connection, userRoles);

            return userRoles;
        }

        /// <summary>
        /// 특정 역할이 할당된 모든 사용자 매핑을 조회합니다.
        /// 사용자 및 역할 정보를 함께 로드합니다.
        /// </summary>
        /// <param name="roleId">조회할 역할 ID</param>
        /// <returns>역할이 할당된 사용자 매핑 목록</returns>
        public async Task<IEnumerable<UserRoleEntity>> GetByRoleIdAsync(int roleId)
        {
            using var connection = CreateConnection();

            var query = $@"
                SELECT * FROM {TableName} WHERE RoleId = @RoleId";

            var parameters = new { RoleId = roleId };

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            var userRoles = await connection.QueryAsync<UserRoleEntity>(query, parameters);

            // 탐색 속성 채우기
            await LoadUserRoleNavigation(connection, userRoles);

            return userRoles;
        }

        /// <summary>
        /// 페이징 및 검색 기능을 적용한 사용자-역할 목록을 가져옵니다.
        /// </summary>
        /// <param name="searchTerm">검색어 (사용자명 또는 역할명)</param>
        /// <param name="userId">특정 사용자 ID로 필터링 (0이면 전체)</param>
        /// <param name="roleId">특정 역할 ID로 필터링 (0이면 전체)</param>
        /// <param name="page">페이지 번호</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <returns>페이징된 사용자-역할 목록과 총 항목 수</returns>
        public async Task<(IEnumerable<UserRoleEntity> Items, int TotalCount)> GetPagedAsync(string searchTerm, int userId, int roleId, int page, int pageSize)
        {
            using var connection = CreateConnection();

            // WHERE 절 조건 구성
            var whereConditions = new List<string>();
            var parameters = new DynamicParameters();

            // 사용자 ID 필터링
            if (userId > 0)
            {
                whereConditions.Add("ur.UserId = @UserId");
                parameters.Add("UserId", userId);
            }

            // 역할 ID 필터링
            if (roleId > 0)
            {
                whereConditions.Add("ur.RoleId = @RoleId");
                parameters.Add("RoleId", roleId);
            }

            // 검색어 처리
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereConditions.Add("(u.Name LIKE @SearchTerm OR u.Email LIKE @SearchTerm OR r.RoleName LIKE @SearchTerm)");
                parameters.Add("SearchTerm", $"%{searchTerm}%");
            }

            // WHERE 절 구성
            var whereClause = whereConditions.Count > 0
                ? $"WHERE {string.Join(" AND ", whereConditions)}"
                : string.Empty;

            // 총 레코드 수 쿼리
            var countQuery = $@"
                SELECT COUNT(*)
                FROM UserRoles ur
                JOIN Users u ON ur.UserId = u.Id
                JOIN Roles r ON ur.RoleId = r.Id
                {whereClause}";

            // SQL 쿼리 로깅
            LogQuery(countQuery, parameters);

            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

            // 페이징 처리된 데이터 쿼리
            var offset = (page - 1) * pageSize;
            var query = $@"
                SELECT 
                    ur.UserId, ur.RoleId, 
                    u.UserName, u.Name, u.Email, 
                    r.RoleName, r.RoleComment
                FROM UserRoles ur
                JOIN Users u ON ur.UserId = u.Id
                JOIN Roles r ON ur.RoleId = r.Id
                {whereClause}
                ORDER BY u.Name, r.RoleName
                OFFSET {offset} ROWS
                FETCH NEXT {pageSize} ROWS ONLY";

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            // 조인 쿼리 실행 및 객체 매핑
            var userRoles = await connection.QueryAsync<UserRoleEntity, UserEntity, RoleEntity, UserRoleEntity>(
                query,
                (userRole, user, role) =>
                {
                    userRole.User = user;
                    userRole.Role = role;
                    return userRole;
                },
                parameters,
                splitOn: "UserName,RoleName");

            return (userRoles, totalCount);
        }

        public async Task<IEnumerable<UserEntity>> GetUsersByRoleIdAsync(int roleId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT u.*
                FROM Users u
                INNER JOIN UserRoles ur ON u.Id = ur.UserId
                WHERE ur.RoleId = @RoleId
                ORDER BY u.UserName";

            var parameters = new { RoleId = roleId };

            LogQuery(query, parameters);

            return await connection.QueryAsync<UserEntity>(query, parameters);
        }
    }
}