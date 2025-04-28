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
using System.Linq;

namespace UnivFI.Application.Services
{
    public class MenuRoleService : IMenuRoleService
    {
        private readonly IMenuRoleRepository _menuRoleRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<MenuRoleService> _logger;

        public MenuRoleService(
            IMenuRoleRepository menuRoleRepository,
            IMapper mapper,
            ILogger<MenuRoleService> logger)
        {
            _menuRoleRepository = menuRoleRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<MenuRoleDto>> GetAllMenuRolesAsync()
        {
            var menuRoles = await _menuRoleRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<MenuRoleDto>>(menuRoles);
        }

        public async Task<IEnumerable<MenuRoleDto>> GetMenuRolesAsync(int page, int pageSize, string searchTerm = null, string searchFields = null)
        {
            var menuRoles = await _menuRoleRepository.GetPagedListAsync(page, pageSize, searchTerm, searchFields);
            return _mapper.Map<IEnumerable<MenuRoleDto>>(menuRoles);
        }

        public async Task<int> GetTotalCountAsync(string searchTerm = null, string searchFields = null)
        {
            return await _menuRoleRepository.GetTotalCountAsync(searchTerm, searchFields);
        }

        public async Task<bool> AssignRoleToMenuAsync(AssignMenuRoleDto assignMenuRoleDto)
        {
            return await _menuRoleRepository.AssignRoleToMenuAsync(assignMenuRoleDto.MenuId, assignMenuRoleDto.RoleId);
        }

        public async Task<bool> RemoveRoleFromMenuAsync(int menuId, int roleId)
        {
            return await _menuRoleRepository.RemoveRoleFromMenuAsync(menuId, roleId);
        }

        public async Task<bool> MenuHasRoleAsync(int menuId, int roleId)
        {
            return await _menuRoleRepository.MenuHasRoleAsync(menuId, roleId);
        }

        public async Task<IEnumerable<MenuRoleDto>> GetMenuRolesByMenuIdAsync(int menuId)
        {
            var menuRoles = await _menuRoleRepository.GetByMenuIdAsync(menuId);
            return _mapper.Map<IEnumerable<MenuRoleDto>>(menuRoles);
        }

        public async Task<IEnumerable<MenuRoleDto>> GetMenuRolesByMenuIdAsync(int menuId, int page, int pageSize)
        {
            try
            {
                _logger.LogInformation("메뉴 ID {MenuId}의 역할 목록을 페이징 처리하여 조회합니다. 페이지: {Page}, 페이지 크기: {PageSize}", menuId, page, pageSize);

                // 전체 목록을 가져와서 메모리에서 페이징 처리 (필요한 경우 실제 DB 쿼리로 개선 가능)
                var allMenuRoles = await _menuRoleRepository.GetByMenuIdAsync(menuId);
                var pagedMenuRoles = allMenuRoles
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                return _mapper.Map<IEnumerable<MenuRoleDto>>(pagedMenuRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메뉴 ID {MenuId}의 역할 목록 페이징 조회 중 오류가 발생했습니다.", menuId);
                throw;
            }
        }

        public async Task<int> GetTotalCountByMenuIdAsync(int menuId)
        {
            try
            {
                _logger.LogInformation("메뉴 ID {MenuId}의 총 역할 수를 조회합니다.", menuId);
                var menuRoles = await _menuRoleRepository.GetByMenuIdAsync(menuId);
                return menuRoles.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메뉴 ID {MenuId}의 총 역할 수 조회 중 오류가 발생했습니다.", menuId);
                throw;
            }
        }

        public async Task<IEnumerable<MenuRoleDto>> GetMenuRolesByRoleIdAsync(int roleId)
        {
            var menuRoles = await _menuRoleRepository.GetByRoleIdAsync(roleId);
            return _mapper.Map<IEnumerable<MenuRoleDto>>(menuRoles);
        }

        public async Task<IEnumerable<MenuRoleDto>> GetMenuRolesByRoleIdAsync(int roleId, int page, int pageSize)
        {
            try
            {
                _logger.LogInformation("역할 ID {RoleId}의 메뉴 목록을 페이징 처리하여 조회합니다. 페이지: {Page}, 페이지 크기: {PageSize}", roleId, page, pageSize);

                // 전체 목록을 가져와서 메모리에서 페이징 처리 (필요한 경우 실제 DB 쿼리로 개선 가능)
                var allMenuRoles = await _menuRoleRepository.GetByRoleIdAsync(roleId);
                var pagedMenuRoles = allMenuRoles
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                return _mapper.Map<IEnumerable<MenuRoleDto>>(pagedMenuRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}의 메뉴 목록 페이징 조회 중 오류가 발생했습니다.", roleId);
                throw;
            }
        }

        public async Task<int> GetTotalCountByRoleIdAsync(int roleId)
        {
            try
            {
                _logger.LogInformation("역할 ID {RoleId}의 총 메뉴 수를 조회합니다.", roleId);
                var menuRoles = await _menuRoleRepository.GetByRoleIdAsync(roleId);
                return menuRoles.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}의 총 메뉴 수 조회 중 오류가 발생했습니다.", roleId);
                throw;
            }
        }

        public async Task<IEnumerable<MenuDto>> GetMenusByRoleIdAsync(int roleId)
        {
            try
            {
                _logger.LogInformation("역할 ID {RoleId}에 할당된 메뉴 목록을 조회합니다.", roleId);
                var menus = await _menuRoleRepository.GetMenusByRoleIdAsync(roleId);
                return _mapper.Map<IEnumerable<MenuDto>>(menus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}의 메뉴 목록 조회 중 오류가 발생했습니다.", roleId);
                throw;
            }
        }
    }
}