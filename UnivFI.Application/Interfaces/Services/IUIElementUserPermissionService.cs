using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;

namespace UnivFI.Application.Interfaces.Services
{
    public interface IUIElementUserPermissionService
    {
        Task<UIElementUserPermissionDto> GetAsync(int elementId, int userId);
        Task<IEnumerable<UIElementUserPermissionDto>> GetByUserIdAsync(int userId);
        Task<IEnumerable<UIElementUserPermissionDto>> GetPermissionsByElementIdAsync(int elementId);
        Task<bool> CreateOrUpdateAsync(CreateUIElementUserPermissionDto dto);
        Task<bool> DeleteAsync(int elementId, int userId);
        Task<bool> AssignPermissionsBatchAsync(UserElementPermissionBatchDto dto);
    }
}