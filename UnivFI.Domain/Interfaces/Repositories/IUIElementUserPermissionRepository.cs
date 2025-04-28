using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;

namespace UnivFI.Domain.Interfaces.Repositories
{
    public interface IUIElementUserPermissionRepository
    {
        Task<UIElementUserPermissionEntity> GetAsync(int elementId, int userId);
        Task<IEnumerable<UIElementUserPermissionEntity>> GetByUserIdAsync(int userId);
        Task<IEnumerable<UIElementUserPermissionEntity>> GetByElementIdAsync(int elementId);
        Task<bool> CreateAsync(UIElementUserPermissionEntity entity);
        Task<bool> UpdateAsync(UIElementUserPermissionEntity entity);
        Task<bool> DeleteAsync(int elementId, int userId);
        Task<bool> AssignPermissionsToUserAsync(int userId, IEnumerable<int> elementIds);
    }
}