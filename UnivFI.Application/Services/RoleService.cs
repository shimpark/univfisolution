using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mapster;
using MapsterMapper;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;


namespace UnivFI.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IRoleRepository roleRepository, IMapper mapper, ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<RoleDto>>(roles);
        }

        public async Task<RoleDto> GetRoleByIdAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            return _mapper.Map<RoleDto>(role);
        }

        public async Task<int> CreateRoleAsync(CreateRoleDto createRoleDto)
        {
            var roleEntity = _mapper.Map<RoleEntity>(createRoleDto);
            return await _roleRepository.CreateAsync(roleEntity);
        }

        public async Task<bool> UpdateRoleAsync(UpdateRoleDto updateRoleDto)
        {
            var roleEntity = _mapper.Map<RoleEntity>(updateRoleDto);
            return await _roleRepository.UpdateAsync(roleEntity);
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            return await _roleRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<RoleDto>> GetRolesByUserIdAsync(int userId)
        {
            var roles = await _roleRepository.GetRolesByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<RoleDto>>(roles);
        }

        public async Task<IEnumerable<RoleDto>> GetRolesByMenuIdAsync(int menuId)
        {
            var roles = await _roleRepository.GetRolesByMenuIdAsync(menuId);
            return _mapper.Map<IEnumerable<RoleDto>>(roles);
        }

        /// <summary>
        /// 페이징된 역할 목록을 가져옵니다
        /// </summary>
        public async Task<IEnumerable<RoleDto>> GetPagedRolesAsync(int page, int pageSize, string searchTerm = null, string searchFields = null)
        {
            var result = await _roleRepository.GetPagedAsync(page, pageSize, searchTerm, searchFields);
            return _mapper.Map<IEnumerable<RoleDto>>(result.Items);
        }

        /// <summary>
        /// 조건에 맞는 총 역할 수를 가져옵니다
        /// </summary>
        public async Task<int> GetTotalCountAsync(string searchTerm = null, string searchFields = null)
        {
            var result = await _roleRepository.GetPagedAsync(1, 1, searchTerm, searchFields);
            return result.TotalCount;
        }

        public async Task<bool> RemoveMenuFromRoleAsync(int roleId, int menuId)
        {
            try
            {
                var role = await _roleRepository.GetByIdAsync(roleId);
                if (role == null)
                {
                    _logger.LogWarning("역할 ID {RoleId}를 찾을 수 없습니다.", roleId);
                    return false;
                }

                return await _roleRepository.RemoveMenuFromRoleAsync(roleId, menuId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing menu {MenuId} from role {RoleId}", menuId, roleId);
                throw;
            }
        }

        public async Task<bool> RemoveUserFromRoleAsync(int roleId, int userId)
        {
            try
            {
                var role = await _roleRepository.GetByIdAsync(roleId);
                if (role == null)
                {
                    _logger.LogWarning("역할 ID {RoleId}를 찾을 수 없습니다.", roleId);
                    return false;
                }

                return await _roleRepository.RemoveUserFromRoleAsync(roleId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from role {RoleId}", userId, roleId);
                throw;
            }
        }

        public async Task<RoleDto> GetByIdAsync(int id)
        {
            try
            {
                var role = await _roleRepository.GetByIdAsync(id);
                return _mapper.Map<RoleDto>(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {Id}를 조회하는 중 오류가 발생했습니다.", id);
                throw;
            }
        }

        public async Task<(IEnumerable<MenuDto> Menus, int TotalCount)> GetAssignedMenusAsync(int roleId, int page, int pageSize, string searchTerm)
        {
            try
            {
                var (menus, totalCount) = await _roleRepository.GetAssignedMenusPagedAsync(roleId, page, pageSize, searchTerm);
                return (_mapper.Map<IEnumerable<MenuDto>>(menus), totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}의 메뉴 목록을 조회하는 중 오류가 발생했습니다.", roleId);
                throw;
            }
        }

        public async Task<(IEnumerable<UserDto> Users, int TotalCount)> GetAssignedUsersAsync(int roleId, int page, int pageSize, string searchTerm)
        {
            try
            {
                var (users, totalCount) = await _roleRepository.GetAssignedUsersPagedAsync(roleId, page, pageSize, searchTerm);
                return (_mapper.Map<IEnumerable<UserDto>>(users), totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}의 사용자 목록을 조회하는 중 오류가 발생했습니다.", roleId);
                throw;
            }
        }
    }
}