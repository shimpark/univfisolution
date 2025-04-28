using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;

namespace UnivFI.Application.Services
{
    public class UIElementUserPermissionService : IUIElementUserPermissionService
    {
        private readonly IUIElementUserPermissionRepository _permissionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UIElementUserPermissionService> _logger;

        public UIElementUserPermissionService(
            IUIElementUserPermissionRepository permissionRepository,
            IMapper mapper,
            ILogger<UIElementUserPermissionService> logger)
        {
            _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UIElementUserPermissionDto> GetAsync(int elementId, int userId)
        {
            var entity = await _permissionRepository.GetAsync(elementId, userId);
            return _mapper.Map<UIElementUserPermissionDto>(entity);
        }

        public async Task<IEnumerable<UIElementUserPermissionDto>> GetByUserIdAsync(int userId)
        {
            var entities = await _permissionRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<UIElementUserPermissionDto>>(entities);
        }

        public async Task<IEnumerable<UIElementUserPermissionDto>> GetPermissionsByElementIdAsync(int elementId)
        {
            var entities = await _permissionRepository.GetByElementIdAsync(elementId);
            var dtos = _mapper.Map<IEnumerable<UIElementUserPermissionDto>>(entities);

            // User 정보 매핑
            foreach (var dto in dtos)
            {
                if (entities.FirstOrDefault(e => e.ElementId == dto.ElementId && e.UserId == dto.UserId)?.User != null)
                {
                    dto.User = _mapper.Map<UserDto>(entities.First(e => e.ElementId == dto.ElementId && e.UserId == dto.UserId).User);
                }
            }

            return dtos;
        }

        public async Task<bool> CreateOrUpdateAsync(CreateUIElementUserPermissionDto dto)
        {
            try
            {
                var existing = await _permissionRepository.GetAsync(dto.ElementId, dto.UserId);

                if (existing != null)
                {
                    // 이미 존재하는 권한이므로 아무것도 하지 않고 성공 반환
                    return true;
                }
                else
                {
                    var entity = _mapper.Map<UIElementUserPermissionEntity>(dto);
                    return await _permissionRepository.CreateAsync(entity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 권한 생성 중 오류 발생 - 요소: {ElementId}, 사용자: {UserId}: {Message}",
                    dto.ElementId, dto.UserId, ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int elementId, int userId)
        {
            try
            {
                return await _permissionRepository.DeleteAsync(elementId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 권한 삭제 중 오류 발생 - 요소: {ElementId}, 사용자: {UserId}: {Message}",
                    elementId, userId, ex.Message);
                throw;
            }
        }

        public async Task<bool> AssignPermissionsBatchAsync(UserElementPermissionBatchDto dto)
        {
            try
            {
                return await _permissionRepository.AssignPermissionsToUserAsync(
                    dto.UserId, dto.ElementIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 권한 일괄 할당 중 오류 발생 - 사용자: {UserId}, 요소 수: {Count}: {Message}",
                    dto.UserId, dto.ElementIds?.Count ?? 0, ex.Message);
                throw;
            }
        }
    }
}