using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;

namespace UnivFI.Domain.Interfaces.Repositories
{
    public interface IMenuRoleRepository
    {
        Task<IEnumerable<MenuRoleEntity>> GetAllAsync();
        Task<IEnumerable<MenuRoleEntity>> GetPagedListAsync(int page, int pageSize, string searchTerm = null, string searchFields = null);
        Task<int> GetTotalCountAsync(string searchTerm = null, string searchFields = null);
        Task<IEnumerable<MenuRoleEntity>> GetByMenuIdAsync(int menuId);
        Task<IEnumerable<MenuRoleEntity>> GetByRoleIdAsync(int roleId);
        Task<bool> AssignRoleToMenuAsync(int menuId, int roleId);
        Task<bool> RemoveRoleFromMenuAsync(int menuId, int roleId);
        Task<bool> RemoveAllRolesFromMenuAsync(int menuId);
        Task<bool> MenuHasRoleAsync(int menuId, int roleId);
        Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId);
    }
}