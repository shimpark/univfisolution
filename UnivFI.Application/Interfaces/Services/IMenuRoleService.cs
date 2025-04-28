using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;
using UnivFI.Domain.Entities;

namespace UnivFI.Application.Interfaces.Services
{
    public interface IMenuRoleService
    {
        Task<IEnumerable<MenuRoleDto>> GetAllMenuRolesAsync();
        Task<IEnumerable<MenuRoleDto>> GetMenuRolesAsync(int page, int pageSize, string searchTerm = null, string searchFields = null);
        Task<int> GetTotalCountAsync(string searchTerm = null, string searchFields = null);
        Task<bool> AssignRoleToMenuAsync(AssignMenuRoleDto assignMenuRoleDto);
        Task<bool> RemoveRoleFromMenuAsync(int menuId, int roleId);
        Task<bool> MenuHasRoleAsync(int menuId, int roleId);
        Task<IEnumerable<MenuRoleDto>> GetMenuRolesByMenuIdAsync(int menuId);
        Task<IEnumerable<MenuRoleDto>> GetMenuRolesByMenuIdAsync(int menuId, int page, int pageSize);
        Task<int> GetTotalCountByMenuIdAsync(int menuId);
        Task<IEnumerable<MenuRoleDto>> GetMenuRolesByRoleIdAsync(int roleId);
        Task<IEnumerable<MenuRoleDto>> GetMenuRolesByRoleIdAsync(int roleId, int page, int pageSize);
        Task<int> GetTotalCountByRoleIdAsync(int roleId);
        Task<IEnumerable<MenuDto>> GetMenusByRoleIdAsync(int roleId);
    }
}