using System;
using System.Collections.Generic;
using System.Linq;
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
    public class UIElementService : IUIElementService
    {
        private readonly IUIElementRepository _uiElementRepository;
        private readonly IUIElementUserPermissionRepository _permissionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UIElementService> _logger;

        public UIElementService(
            IUIElementRepository uiElementRepository,
            IUIElementUserPermissionRepository permissionRepository,
            IMapper mapper,
            ILogger<UIElementService> logger)
        {
            _uiElementRepository = uiElementRepository ?? throw new ArgumentNullException(nameof(uiElementRepository));
            _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UIElementDto> GetByIdAsync(int id)
        {
            var entity = await _uiElementRepository.GetByIdAsync(id);
            return _mapper.Map<UIElementDto>(entity);
        }

        public async Task<UIElementDto> GetByElementKeyAsync(string elementKey)
        {
            var entity = await _uiElementRepository.GetByElementKeyAsync(elementKey);
            return _mapper.Map<UIElementDto>(entity);
        }

        public async Task<IEnumerable<UIElementDto>> GetAllAsync()
        {
            var entities = await _uiElementRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UIElementDto>>(entities);
        }

        public async Task<IEnumerable<UIElementDto>> GetByElementTypeAsync(string elementType)
        {
            var entities = await _uiElementRepository.GetByElementTypeAsync(elementType);
            return _mapper.Map<IEnumerable<UIElementDto>>(entities);
        }

        public async Task<IEnumerable<UIElementWithPermissionDto>> GetWithUserPermissionsAsync(int userId)
        {
            var elements = await _uiElementRepository.GetWithUserPermissionsAsync(userId);
            var userPermissions = await _permissionRepository.GetByUserIdAsync(userId);

            var permissionDict = userPermissions.ToDictionary(p => p.ElementId, p => true);

            var result = _mapper.Map<IEnumerable<UIElementWithPermissionDto>>(elements);
            foreach (var item in result)
            {
                item.IsEnabled = permissionDict.ContainsKey(item.Id);
            }

            return result;
        }

        public async Task<int> CreateAsync(CreateUIElementDto dto)
        {
            try
            {
                var entity = _mapper.Map<UIElementEntity>(dto);
                return await _uiElementRepository.CreateAsync(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소 생성 중 오류 발생: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, UpdateUIElementDto dto)
        {
            try
            {
                var entity = await _uiElementRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("존재하지 않는 UI 요소(ID: {Id})를 업데이트하려고 했습니다.", id);
                    return false;
                }

                _mapper.Map(dto, entity);
                entity.UpdatedAt = DateTime.UtcNow;

                return await _uiElementRepository.UpdateAsync(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소(ID: {Id}) 업데이트 중 오류 발생: {Message}", id, ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                return await _uiElementRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI 요소(ID: {Id}) 삭제 중 오류 발생: {Message}", id, ex.Message);
                throw;
            }
        }
    }
}