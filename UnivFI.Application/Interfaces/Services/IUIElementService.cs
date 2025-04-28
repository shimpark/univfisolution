using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;

namespace UnivFI.Application.Interfaces.Services
{
    public interface IUIElementService
    {
        Task<UIElementDto> GetByIdAsync(int id);
        Task<UIElementDto> GetByElementKeyAsync(string elementKey);
        Task<IEnumerable<UIElementDto>> GetAllAsync();
        Task<IEnumerable<UIElementDto>> GetByElementTypeAsync(string elementType);
        Task<IEnumerable<UIElementWithPermissionDto>> GetWithUserPermissionsAsync(int userId);
        Task<int> CreateAsync(CreateUIElementDto dto);
        Task<bool> UpdateAsync(int id, UpdateUIElementDto dto);
        Task<bool> DeleteAsync(int id);
    }
}