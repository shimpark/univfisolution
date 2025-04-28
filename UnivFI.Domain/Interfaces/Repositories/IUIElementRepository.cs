using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;

namespace UnivFI.Domain.Interfaces.Repositories
{
    public interface IUIElementRepository
    {
        // 기본 CRUD 메서드
        Task<IEnumerable<UIElementEntity>> GetAllAsync();
        Task<UIElementEntity> GetByIdAsync(int id);
        Task<int> CreateAsync(UIElementEntity entity);
        Task<bool> UpdateAsync(UIElementEntity entity);
        Task<bool> DeleteAsync(int id);

        // 특화된 메서드
        Task<UIElementEntity> GetByElementKeyAsync(string elementKey);
        Task<IEnumerable<UIElementEntity>> GetByElementTypeAsync(string elementType);
        Task<IEnumerable<UIElementEntity>> GetWithUserPermissionsAsync(int userId);
    }
}