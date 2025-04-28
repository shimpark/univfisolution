using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;

namespace UnivFI.Domain.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<IEnumerable<RoleEntity>> GetAllAsync();
        Task<RoleEntity> GetByIdAsync(int id);
        Task<int> CreateAsync(RoleEntity role);
        Task<bool> UpdateAsync(RoleEntity role);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<RoleEntity>> GetRolesByUserIdAsync(int userId);
        Task<IEnumerable<RoleEntity>> GetRolesByMenuIdAsync(int menuId);
        Task<(IEnumerable<RoleEntity> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string searchTerm = null, string searchFields = null);
        Task<bool> RemoveMenuFromRoleAsync(int roleId, int menuId);
        Task<bool> RemoveUserFromRoleAsync(int roleId, int userId);
        Task<(IEnumerable<MenuEntity> Menus, int TotalCount)> GetAssignedMenusPagedAsync(int roleId, int page, int pageSize, string searchTerm);
        Task<(IEnumerable<UserEntity> Users, int TotalCount)> GetAssignedUsersPagedAsync(int roleId, int page, int pageSize, string searchTerm);
    }
}