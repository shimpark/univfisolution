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
    public class UIElementRepository : BaseRepository<UIElementEntity, int, UIElementRepository>, IUIElementRepository
    {
        private const string TABLE_NAME = "UIElements";

        public UIElementRepository(IConnectionFactory connectionFactory, ILogger<UIElementRepository> logger)
            : base(connectionFactory, logger, TABLE_NAME)
        {
        }

        public async Task<UIElementEntity> GetByElementKeyAsync(string elementKey)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT * 
                FROM UIElements 
                WHERE ElementKey = @ElementKey";

            var parameters = new { ElementKey = elementKey };
            LogQuery(query, parameters);

            return await connection.QuerySingleOrDefaultAsync<UIElementEntity>(query, parameters);
        }

        public async Task<IEnumerable<UIElementEntity>> GetByElementTypeAsync(string elementType)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT * 
                FROM UIElements 
                WHERE ElementType = @ElementType
                ORDER BY ElementName";

            var parameters = new { ElementType = elementType };
            LogQuery(query, parameters);

            return await connection.QueryAsync<UIElementEntity>(query, parameters);
        }

        public async Task<IEnumerable<UIElementEntity>> GetWithUserPermissionsAsync(int userId)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT e.*, p.UserId, p.ElementId
                FROM UIElements e
                LEFT JOIN UIElementUserPermissions p 
                    ON e.Id = p.ElementId AND p.UserId = @UserId
                ORDER BY e.ElementType, e.ElementName";

            var parameters = new { UserId = userId };
            LogQuery(query, parameters);

            var uiElementDict = new Dictionary<int, UIElementEntity>();

            await connection.QueryAsync<UIElementEntity, UIElementUserPermissionEntity, UIElementEntity>(
                query,
                (element, permission) =>
                {
                    if (!uiElementDict.TryGetValue(element.Id, out var uiElement))
                    {
                        uiElement = element;
                        uiElement.UserPermissions = new List<UIElementUserPermissionEntity>();
                        uiElementDict.Add(element.Id, uiElement);
                    }

                    if (permission != null && permission.UserId != 0)
                    {
                        permission.UIElement = uiElement;
                        ((List<UIElementUserPermissionEntity>)uiElement.UserPermissions).Add(permission);
                    }

                    return uiElement;
                },
                parameters,
                splitOn: "UserId"
            );

            return uiElementDict.Values;
        }

        public override async Task<UIElementEntity> GetByIdAsync(int id)
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT * 
                FROM UIElements 
                WHERE Id = @Id";

            var parameters = new { Id = id };
            LogQuery(query, parameters);

            return await connection.QuerySingleOrDefaultAsync<UIElementEntity>(query, parameters);
        }

        public override async Task<IEnumerable<UIElementEntity>> GetAllAsync()
        {
            using var connection = CreateConnection();

            var query = @"
                SELECT * 
                FROM UIElements 
                ORDER BY ElementType, ElementName";

            LogQuery(query, null);

            return await connection.QueryAsync<UIElementEntity>(query);
        }

        public override async Task<int> CreateAsync(UIElementEntity entity)
        {
            using var connection = CreateConnection();

            entity.CreatedAt = DateTime.UtcNow;
            LogEntityOperation("Creating", entity);

            return await connection.InsertAsync(entity);
        }

        public override async Task<bool> UpdateAsync(UIElementEntity entity)
        {
            using var connection = CreateConnection();

            entity.UpdatedAt = DateTime.UtcNow;
            LogEntityOperation("Updating", entity);

            return await connection.UpdateAsync(entity);
        }

        public override async Task<bool> DeleteAsync(int id)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 먼저 관련된 모든 권한 삭제
                var deletePermissionsQuery = @"DELETE FROM UIElementUserPermissions WHERE ElementId = @Id";
                await connection.ExecuteAsync(deletePermissionsQuery, new { Id = id }, transaction);

                // 그 다음 UI 요소 삭제
                var entity = await GetByIdAsync(id);
                if (entity == null)
                    return false;

                LogEntityOperation("Deleting", entity);
                var result = await connection.DeleteAsync(entity, transaction);

                transaction.Commit();
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.LogError(ex, "UI 요소 삭제 중 오류 발생 - ID: {Id}: {Message}", id, ex.Message);
                throw;
            }
        }
    }
}