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
    public class UserRoleService : IUserRoleService
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserRoleService> _logger;

        public UserRoleService(
            IUserRoleRepository userRoleRepository,
            IMapper mapper,
            ILogger<UserRoleService> logger)
        {
            _userRoleRepository = userRoleRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<UserRoleDto>> GetAllUserRolesAsync()
        {
            var userRoles = await _userRoleRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserRoleDto>>(userRoles);
        }

        public async Task<bool> AssignRoleToUserAsync(int userId, int roleId)
        {
            if (userId <= 0)
                throw new ArgumentException("사용자 ID는 0보다 커야 합니다.", nameof(userId));

            if (roleId <= 0)
                throw new ArgumentException("역할 ID는 0보다 커야 합니다.", nameof(roleId));

            try
            {
                return await _userRoleRepository.AssignRoleToUserAsync(userId, roleId);
            }
            catch (Exception ex)
            {
                // 로깅 코드를 추가할 수 있음
                Console.WriteLine($"Error in UserRoleService.AssignRoleToUserAsync: {ex.Message}");
                throw; // 예외 다시 던지기
            }
        }

        public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
        {
            return await _userRoleRepository.RemoveRoleFromUserAsync(userId, roleId);
        }

        public async Task<bool> UserHasRoleAsync(int userId, int roleId)
        {
            return await _userRoleRepository.UserHasRoleAsync(userId, roleId);
        }

        public async Task<IEnumerable<UserRoleDto>> GetUserRolesByUserIdAsync(int userId)
        {
            var userRoles = await _userRoleRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<UserRoleDto>>(userRoles);
        }

        public async Task<IEnumerable<UserRoleDto>> GetUserRolesByRoleIdAsync(int roleId)
        {
            var userRoles = await _userRoleRepository.GetByRoleIdAsync(roleId);
            return _mapper.Map<IEnumerable<UserRoleDto>>(userRoles);
        }

        /// <summary>
        /// 페이징 및 검색 기능을 적용한 사용자-역할 목록을 가져옵니다.
        /// </summary>
        /// <param name="searchTerm">검색어 (사용자명 또는 역할명)</param>
        /// <param name="userId">특정 사용자 ID로 필터링 (0이면 전체)</param>
        /// <param name="roleId">특정 역할 ID로 필터링 (0이면 전체)</param>
        /// <param name="page">페이지 번호</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <returns>페이징된 사용자-역할 목록</returns>
        public async Task<PagedResultDto<UserRoleDto>> GetPagedUserRolesAsync(string searchTerm, int userId, int roleId, int page, int pageSize)
        {
            var result = await _userRoleRepository.GetPagedAsync(searchTerm, userId, roleId, page, pageSize);
            var userRoleDtos = _mapper.Map<IEnumerable<UserRoleDto>>(result.Items);

            return new PagedResultDto<UserRoleDto>(userRoleDtos, result.TotalCount, page, pageSize);
        }

        public async Task<IEnumerable<UserDto>> GetUsersByRoleIdAsync(int roleId)
        {
            try
            {
                _logger.LogInformation("역할 ID {RoleId}에 할당된 사용자 목록을 조회합니다.", roleId);
                var users = await _userRoleRepository.GetUsersByRoleIdAsync(roleId);
                return _mapper.Map<IEnumerable<UserDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "역할 ID {RoleId}의 사용자 목록 조회 중 오류가 발생했습니다.", roleId);
                throw;
            }
        }
    }
}