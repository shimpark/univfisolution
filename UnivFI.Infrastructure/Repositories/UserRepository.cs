using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;
using UnivFI.Infrastructure.Helpers;

namespace UnivFI.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<UserEntity, int, UserRepository>, IUserRepository
    {
        private const string TABLE_NAME = "Users";

        public UserRepository(IConnectionFactory connectionFactory, ILogger<UserRepository> logger)
            : base(connectionFactory, logger, TABLE_NAME)
        {
        }

        public async Task<IEnumerable<UserEntity>> GetListAsync(int page, int pageSize, string? searchTerm = null, string? searchFields = null, string? sortOrder = null)
        {
            using var connection = CreateConnection();

            // 검색 필드 기본값 설정
            if (string.IsNullOrEmpty(searchFields))
            {
                searchFields = "Name,Email";
            }

            // 검색 파라미터 생성
            var parameters = CreatePagingParameters(page, pageSize);
            var whereClause = CreateSearchWhereClause(searchTerm, searchFields, parameters);

            // 정렬 순서 처리
            var orderByClause = "ORDER BY Id";
            if (!string.IsNullOrEmpty(sortOrder))
            {
                var allowedColumns = new[] { "Id", "Name", "Email", "UserName", "CreatedAt", "UpdatedAt" };

                // '_desc' 형식의 정렬 파라미터 처리
                var columnName = sortOrder.Split('_')[0];
                var direction = sortOrder.EndsWith("_desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

                if (allowedColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                {
                    orderByClause = $"ORDER BY {columnName} {direction}";
                }
            }

            var query = $@"
                SELECT *
                FROM {TableName}
                WHERE {whereClause}
                {orderByClause}
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            return await connection.QueryAsync<UserEntity>(query, parameters);
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null, string? searchFields = null)
        {
            using var connection = CreateConnection();

            // 검색 필드 기본값 설정
            if (string.IsNullOrEmpty(searchFields))
            {
                searchFields = "Name,Email";
            }

            // 검색 파라미터 생성
            var parameters = new DynamicParameters();
            var whereClause = CreateSearchWhereClause(searchTerm, searchFields, parameters);

            var query = $@"
                SELECT COUNT(*)
                FROM {TableName}
                WHERE {whereClause}";

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            return await connection.ExecuteScalarAsync<int>(query, parameters);
        }

        public async Task<UserEntity?> GetByIdAsync(int id)
        {
            using var connection = CreateConnection();

            // ID 기반 쿼리 로깅
            LogIdOperation("SELECT", id);

            return await connection.GetAsync<UserEntity>(id);
        }

        public async Task<int> CreateAsync(UserEntity user)
        {
            using var connection = CreateConnection();

            // 엔티티 작업 로깅 (민감 정보 마스킹)
            LogEntityOperation("INSERT INTO", user, "Password", "Salt", "RefreshToken");

            return await connection.InsertAsync(user);
        }

        public async Task<bool> UpdateAsync(UserEntity user)
        {
            try
            {
                using var connection = CreateConnection();

                // 직접 UPDATE 쿼리 작성 (UpdatedAt 필드 제거)
                var query = $@"
                    UPDATE {TableName} SET 
                        UserName = @UserName,
                        Name = @Name,
                        Email = @Email
                    WHERE Id = @Id";

                var parameters = new
                {
                    user.Id,
                    user.UserName,
                    user.Name,
                    user.Email
                };

                // 엔티티 작업 로깅 (민감 정보 마스킹)
                LogEntityOperation("UPDATE", user, "Password", "Salt", "RefreshToken");

                // 직접 쿼리 실행
                int rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "사용자 ID {UserId} 업데이트 중 오류 발생", user.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = CreateConnection();

            // 먼저 사용자와 관련된 UserRoles 데이터 삭제
            var deleteUserRolesQuery = @"
                DELETE FROM UserRoles
                WHERE UserId = @Id";

            // SQL 쿼리 로깅
            LogQuery(deleteUserRolesQuery, new { Id = id });

            await connection.ExecuteAsync(deleteUserRolesQuery, new { Id = id });

            // 사용자 정보 가져오기
            LogIdOperation("SELECT", id);
            var user = await connection.GetAsync<UserEntity>(id);

            if (user != null)
            {
                LogIdOperation("DELETE FROM", id);
                return await connection.DeleteAsync(user);
            }

            return false;
        }

        public async Task<UserEntity?> GetByUserNameAsync(string userName)
        {
            using var connection = CreateConnection();

            var query = $@"
                SELECT *
                FROM {TableName}
                WHERE UserName = @UserName";

            var parameters = new { UserName = userName };

            // SQL 쿼리 로깅
            LogQuery(query, parameters);

            return await connection.QuerySingleOrDefaultAsync<UserEntity>(query, parameters);
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string hashedPassword, string salt)
        {
            try
            {
                using var connection = CreateConnection();

                // 직접 UPDATE 쿼리 작성 (UpdatedAt 필드 제거)
                var query = @"
                    UPDATE Users SET 
                        Password = @Password,
                        Salt = @Salt
                    WHERE Id = @Id";

                var parameters = new
                {
                    Id = userId,
                    Password = hashedPassword,
                    Salt = salt
                };

                // 엔티티 작업 로깅 (민감 정보 마스킹)
                var userInfo = new
                {
                    Id = userId
                };

                LogEntityOperation("UPDATE", userInfo, "Password", "Salt");

                // 직접 쿼리 실행
                int rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "비밀번호 업데이트 중 오류 발생 (사용자 ID: {UserId})", userId);
            }
        }

        public async Task<bool> SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiryDate)
        {
            try
            {
                using var connection = CreateConnection();

                // 직접 UPDATE 쿼리 작성 (UpdatedAt 필드 제거)
                var query = @"
                    UPDATE Users SET 
                        RefreshToken = @RefreshToken,
                        RefreshTokenExpiry = @RefreshTokenExpiry
                    WHERE Id = @Id";

                var parameters = new
                {
                    Id = userId,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = expiryDate
                };

                // 엔티티 작업 로깅 (민감 정보 마스킹)
                var userInfo = new
                {
                    Id = userId,
                    RefreshTokenExpiry = expiryDate
                };

                LogEntityOperation("UPDATE", userInfo, "RefreshToken");

                // 직접 쿼리 실행
                int rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "리프레시 토큰 저장 중 오류 발생 (사용자 ID: {UserId})", userId);
            }
        }

        public async Task<UserEntity?> GetByRefreshTokenAsync(string refreshToken)
        {
            using var connection = CreateConnection();

            var query = $@"
                SELECT *
                FROM {TableName}
                WHERE RefreshToken = @RefreshToken
                AND RefreshTokenExpiry > @Now";

            var parameters = new
            {
                RefreshToken = refreshToken,
                Now = DateTime.UtcNow
            };

            // 토큰 마스킹 처리 후 로깅
            var maskedParams = new
            {
                RefreshToken = SqlQueryLogger.MaskSensitiveString(refreshToken),
                Now = DateTime.UtcNow
            };
            LogQuery(query, maskedParams);

            return await connection.QuerySingleOrDefaultAsync<UserEntity>(query, parameters);
        }

        public async Task<bool> RevokeRefreshTokenAsync(int userId)
        {
            try
            {
                using var connection = CreateConnection();

                // 직접 UPDATE 쿼리 작성 (UpdatedAt 필드 제거)
                var query = @"
                    UPDATE Users SET 
                        RefreshToken = NULL,
                        RefreshTokenExpiry = NULL
                    WHERE Id = @Id";

                var parameters = new
                {
                    Id = userId
                };

                LogEntityOperation("UPDATE", new { Id = userId, RefreshTokenRevoked = true });

                // 직접 쿼리 실행
                int rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "리프레시 토큰 무효화 중 오류 발생 (사용자 ID: {UserId})", userId);
            }
        }
    }
}
