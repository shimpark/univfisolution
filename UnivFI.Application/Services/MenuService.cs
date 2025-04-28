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
using UnivFI.Application.Interfaces;

namespace UnivFI.Application.Services
{
    public class MenuService : IMenuService
    {
        private readonly IMenuRepository _menuRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<MenuService> _logger;

        public MenuService(IMenuRepository menuRepository, IMapper mapper, ILogger<MenuService> logger)
        {
            _menuRepository = menuRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<MenuDto>> GetAllMenusAsync()
        {
            var menus = await _menuRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<MenuDto>>(menus);
        }

        public async Task<MenuDto> GetMenuByIdAsync(int id)
        {
            var menu = await _menuRepository.GetByIdAsync(id);
            return _mapper.Map<MenuDto>(menu);
        }

        public async Task<int> CreateMenuAsync(CreateMenuDto createMenuDto)
        {
            try
            {
                // 부모 메뉴가 지정된 경우, 존재하는지 확인
                if (createMenuDto.ParentId.HasValue)
                {
                    var parentMenu = await GetMenuByIdAsync(createMenuDto.ParentId.Value);
                    if (parentMenu == null)
                    {
                        throw new InvalidOperationException($"부모 메뉴 ID {createMenuDto.ParentId}가 존재하지 않습니다.");
                    }
                }

                var menuEntity = _mapper.Map<MenuEntity>(createMenuDto);

                // UseNewIcon 속성은 이제 직접 bool? 타입으로 매핑됨
                // 기본값 처리
                if (createMenuDto.UseNewIcon == null)
                {
                    menuEntity.UseNewIcon = false;
                }

                var result = await _menuRepository.CreateAsync(menuEntity);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"메뉴 생성 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateMenuAsync(UpdateMenuDto updateMenuDto)
        {
            try
            {
                // 현재 메뉴가 존재하는지 확인
                var existingMenu = await GetMenuByIdAsync(updateMenuDto.Id);
                if (existingMenu == null)
                {
                    throw new InvalidOperationException($"메뉴 ID {updateMenuDto.Id}가 존재하지 않습니다.");
                }

                // 부모 메뉴가 변경되는 경우
                if (updateMenuDto.ParentId != existingMenu.ParentId)
                {
                    // 새로운 부모가 지정된 경우, 존재하는지 확인
                    if (updateMenuDto.ParentId.HasValue)
                    {
                        var newParent = await GetMenuByIdAsync(updateMenuDto.ParentId.Value);
                        if (newParent == null)
                        {
                            throw new InvalidOperationException($"새 부모 메뉴 ID {updateMenuDto.ParentId}가 존재하지 않습니다.");
                        }

                        // 순환 참조 확인
                        if (await IsCircularReference(updateMenuDto.Id, updateMenuDto.ParentId.Value))
                        {
                            throw new InvalidOperationException("순환 참조가 발생할 수 있는 부모-자식 관계입니다.");
                        }
                    }
                }

                var menuEntity = _mapper.Map<MenuEntity>(updateMenuDto);

                // 기존 레코드의 CreatedAt 값을 유지
                menuEntity.CreatedAt = existingMenu.CreatedAt;

                // UseNewIcon 속성은 이제 직접 bool? 타입으로 매핑됨
                // 기본값 처리
                if (updateMenuDto.UseNewIcon == null)
                {
                    menuEntity.UseNewIcon = false;
                }

                return await _menuRepository.UpdateAsync(menuEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"메뉴 업데이트 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteMenuAsync(int menuId)
        {
            try
            {
                // 하위 메뉴가 있는지 확인
                var hasChildren = await HasChildrenAsync(menuId);
                if (hasChildren)
                {
                    // 하위 메뉴들을 모두 가져옴
                    var children = await GetChildrenAsync(menuId);

                    // 하위 메뉴들을 재귀적으로 삭제
                    foreach (var child in children)
                    {
                        await DeleteMenuAsync(child.Id);
                    }
                }

                // 현재 메뉴 삭제
                return await _menuRepository.DeleteAsync(menuId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"메뉴 삭제 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<MenuDto>> GetMenusByRoleIdAsync(int roleId)
        {
            var menus = await _menuRepository.GetMenusByRoleIdAsync(roleId);
            return _mapper.Map<IEnumerable<MenuDto>>(menus);
        }

        public async Task<IEnumerable<MenuDto>> GetMenusByUserIdAsync(int userId)
        {
            var menus = await _menuRepository.GetMenusByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<MenuDto>>(menus);
        }

        public async Task<IEnumerable<MenuDto>> GetAllForTreeAsync()
        {
            var menus = await _menuRepository.GetAllForTreeAsync();
            return _mapper.Map<IEnumerable<MenuDto>>(menus);
        }

        public async Task<bool> AssignRoleToMenuAsync(int menuId, int roleId)
        {
            return await _menuRepository.AssignRoleToMenuAsync(menuId, roleId);
        }

        public async Task<bool> RemoveRoleFromMenuAsync(int menuId, int roleId)
        {
            return await _menuRepository.RemoveRoleFromMenuAsync(menuId, roleId);
        }

        public async Task<bool> HasChildrenAsync(int menuId)
        {
            var children = await _menuRepository.GetAllAsync();
            return children.Any(m => m.ParentId == menuId);
        }

        public async Task<IEnumerable<MenuDto>> GetChildrenAsync(int menuId)
        {
            var allMenus = await _menuRepository.GetAllAsync();
            var children = allMenus.Where(m => m.ParentId == menuId);
            return _mapper.Map<IEnumerable<MenuDto>>(children);
        }

        private async Task<bool> IsCircularReference(int menuId, int newParentId)
        {
            var currentParentId = newParentId;
            var visited = new HashSet<int>();

            while (currentParentId != 0)
            {
                if (!visited.Add(currentParentId))
                {
                    return true; // 순환 발견
                }

                if (currentParentId == menuId)
                {
                    return true; // 순환 발견
                }

                var parent = await GetMenuByIdAsync(currentParentId);
                if (parent == null || !parent.ParentId.HasValue)
                {
                    break;
                }

                currentParentId = parent.ParentId.Value;
            }

            return false;
        }

        public async Task<(IEnumerable<MenuEntity> Items, int TotalCount)> GetHierarchicalMenuDataAsync(
            int page,
            int pageSize,
            string searchTerm = "",
            string sortColumn = "",
            bool ascending = true)
        {
            try
            {
                _logger.LogDebug("GetHierarchicalMenuDataAsync 호출: page={Page}, pageSize={PageSize}, searchTerm={SearchTerm}, sortColumn={SortColumn}, ascending={Ascending}",
                    page, pageSize, searchTerm, sortColumn, ascending);

                var result = await _menuRepository.GetHierarchicalMenuDataAsync(page, pageSize, searchTerm, sortColumn, ascending);

                _logger.LogDebug("GetHierarchicalMenuDataAsync 완료: {Count}개의 항목 반환", result.Items.Count());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHierarchicalMenuDataAsync 실행 중 오류 발생");
                throw;
            }
        }
    }
}