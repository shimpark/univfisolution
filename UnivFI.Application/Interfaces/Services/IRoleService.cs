using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;

namespace UnivFI.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<RoleDto> GetRoleByIdAsync(int id);
        Task<int> CreateRoleAsync(CreateRoleDto createRoleDto);
        Task<bool> UpdateRoleAsync(UpdateRoleDto updateRoleDto);
        Task<bool> DeleteRoleAsync(int id);
        Task<IEnumerable<RoleDto>> GetRolesByUserIdAsync(int userId);
        Task<IEnumerable<RoleDto>> GetRolesByMenuIdAsync(int menuId);

        /// <summary>
        /// 페이징된 역할 목록을 가져옵니다
        /// </summary>
        Task<IEnumerable<RoleDto>> GetPagedRolesAsync(int page, int pageSize, string searchTerm = null, string searchFields = null);

        /// <summary>
        /// 조건에 맞는 총 역할 수를 가져옵니다
        /// </summary>
        Task<int> GetTotalCountAsync(string searchTerm = null, string searchFields = null);

        Task<bool> RemoveMenuFromRoleAsync(int roleId, int menuId);
        Task<bool> RemoveUserFromRoleAsync(int roleId, int userId);

        Task<RoleDto> GetByIdAsync(int id);
        Task<(IEnumerable<MenuDto> Menus, int TotalCount)> GetAssignedMenusAsync(int roleId, int page, int pageSize, string searchTerm);
        Task<(IEnumerable<UserDto> Users, int TotalCount)> GetAssignedUsersAsync(int roleId, int page, int pageSize, string searchTerm);
    }
}