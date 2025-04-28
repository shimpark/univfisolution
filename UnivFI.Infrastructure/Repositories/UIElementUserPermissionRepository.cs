using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;

namespace UnivFI.Infrastructure.Repositories
{
    public class UIElementUserPermissionRepository : BaseRepository<UIElementUserPermissionEntity, (int ElementId, int UserId), UIElementUserPermissionRepository>, IUIElementUserPermissionRepository
    {
        private const string TABLE_NAME = "UIElementUserPermissions";

        public UIElementUserPermissionRepository(
            IConnectionFactory connectionFactory,
            ILogger<UIElementUserPermissionRepository> logger)
            : base(connectionFactory, logger, TABLE_NAME)
        {
        }

        public async Task<UIElementUserPermissionEntity> GetAsync(int elementId, int userId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT p.*, e.*
                FROM UIElementUserPermissions p
                INNER JOIN UIElements e ON p.ElementId = e.Id
                WHERE p.ElementId = @ElementId AND p.UserId = @UserId";

            var parameters = new { ElementId = elementId, UserId = userId };
            LogQuery(query, parameters);

            var result = await connection.QueryAsync<UIElementUserPermissionEntity, UIElementEntity, UIElementUserPermissionEntity>(
                query,
                (permission, element) =>
                {
                    permission.UIElement = element;
                    return permission;
                },
                parameters,
                splitOn: "Id"
            );

            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<UIElementUserPermissionEntity>> GetByUserIdAsync(int userId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT p.*, e.*
                FROM UIElementUserPermissions p
                INNER JOIN UIElements e ON p.ElementId = e.Id
                WHERE p.UserId = @UserId";

            var parameters = new { UserId = userId };
            LogQuery(query, parameters);

            return await connection.QueryAsync<UIElementUserPermissionEntity, UIElementEntity, UIElementUserPermissionEntity>(
                query,
                (permission, element) =>
                {
                    permission.UIElement = element;
                    return permission;
                },
                parameters,
                splitOn: "Id"
            );
        }

        public async Task<IEnumerable<UIElementUserPermissionEntity>> GetByElementIdAsync(int elementId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT p.*, e.*, u.*
                FROM UIElementUserPermissions p
                INNER JOIN UIElements e ON p.ElementId = e.Id
                INNER JOIN Users u ON p.UserId = u.Id
                WHERE p.ElementId = @ElementId";

            var parameters = new { ElementId = elementId };
            LogQuery(query, parameters);

            return await connection.QueryAsync<UIElementUserPermissionEntity, UIElementEntity, UserEntity, UIElementUserPermissionEntity>(
                query,
                (permission, element, user) =>
                {
                    permission.UIElement = element;
                    permission.User = user;
                    return permission;
                },
                parameters,
                splitOn: "Id,Id"
            );
        }

        public async Task<bool> CreateAsync(UIElementUserPermissionEntity entity)
        {
            using var connection = CreateConnection();
            try
            {
                var result = await connection.InsertAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UI 요소 사용자 권한 생성 중 오류 발생 - ElementId: {ElementId}, UserId: {UserId}",
                    entity.ElementId, entity.UserId);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(UIElementUserPermissionEntity entity)
        {
            // 권한 테이블에서는 ElementId와 UserId가 기본 키이고 다른 컬럼이 없어졌으므로
            // 업데이트할 것이 없어 항상 true 반환
            return true;
        }

        public async Task<bool> DeleteAsync(int elementId, int userId)
        {
            using var connection = CreateConnection();
            try
            {
                var entity = new UIElementUserPermissionEntity
                {
                    ElementId = elementId,
                    UserId = userId
                };
                return await connection.DeleteAsync(entity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UI 요소 사용자 권한 삭제 중 오류 발생 - ElementId: {ElementId}, UserId: {UserId}",
                    elementId, userId);
                return false;
            }
        }

        public async Task<bool> AssignPermissionsToUserAsync(int userId, IEnumerable<int> elementIds)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 사용자에 대한 기존 권한 삭제
                var deleteQuery = @"DELETE FROM UIElementUserPermissions WHERE UserId = @UserId";
                await connection.ExecuteAsync(deleteQuery, new { UserId = userId }, transaction);

                // 새 권한 추가
                if (elementIds != null && elementIds.Any())
                {
                    var insertQuery = @"
                        INSERT INTO UIElementUserPermissions (ElementId, UserId)
                        VALUES (@ElementId, @UserId)";

                    foreach (var elementId in elementIds)
                    {
                        var parameters = new
                        {
                            ElementId = elementId,
                            UserId = userId
                        };

                        await connection.ExecuteAsync(insertQuery, parameters, transaction);
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.LogError(ex, "사용자 UI 요소 권한 일괄 할당 중 오류 발생 - 사용자: {UserId}: {Message}",
                    userId, ex.Message);
                throw;
            }
        }
    }
}