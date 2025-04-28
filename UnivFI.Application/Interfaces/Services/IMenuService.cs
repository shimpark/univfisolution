using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;
using UnivFI.Domain.Entities;

namespace UnivFI.Application.Interfaces.Services
{
    public interface IMenuService
    {
        Task<IEnumerable<MenuDto>> GetAllMenusAsync();
        Task<MenuDto> GetMenuByIdAsync(int id);
        Task<int> CreateMenuAsync(CreateMenuDto createMenuDto);
        Task<bool> UpdateMenuAsync(UpdateMenuDto updateMenuDto);
        Task<bool> DeleteMenuAsync(int menuId);
        Task<IEnumerable<MenuDto>> GetMenusByRoleIdAsync(int roleId);
        Task<IEnumerable<MenuDto>> GetMenusByUserIdAsync(int userId);
        Task<IEnumerable<MenuDto>> GetAllForTreeAsync();
        Task<bool> AssignRoleToMenuAsync(int menuId, int roleId);
        Task<bool> RemoveRoleFromMenuAsync(int menuId, int roleId);
        Task<bool> HasChildrenAsync(int menuId);
        Task<IEnumerable<MenuDto>> GetChildrenAsync(int menuId);
        Task<(IEnumerable<MenuEntity> Items, int TotalCount)> GetHierarchicalMenuDataAsync(
            int page,
            int pageSize,
            string searchTerm = "",
            string sortColumn = "",
            bool ascending = true
        );

        // ... 기존 메서드들 ...
    }
}