using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Models;

namespace UnivFI.Domain.Interfaces.Repositories
{
    public interface IMenuRepository
    {
        Task<IEnumerable<MenuEntity>> GetAllAsync();
        Task<MenuEntity> GetByIdAsync(int id);
        Task<int> CreateAsync(MenuEntity menu);
        Task<bool> UpdateAsync(MenuEntity menu);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId);
        Task<IEnumerable<MenuEntity>> GetMenusByUserIdAsync(int userId);
        Task<(IEnumerable<MenuEntity> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string searchTerm = null, string searchFields = null);

        /// <summary>
        /// 트리 구조를 위한 모든 메뉴를 가져옵니다.
        /// </summary>
        /// <returns>메뉴 항목 컬렉션</returns>
        Task<IEnumerable<MenuEntity>> GetAllForTreeAsync();

        /// <summary>
        /// 메뉴에 역할을 할당합니다.
        /// </summary>
        /// <param name="menuId">메뉴 ID</param>
        /// <param name="roleId">역할 ID</param>
        /// <returns>성공 여부</returns>
        Task<bool> AssignRoleToMenuAsync(int menuId, int roleId);

        /// <summary>
        /// 메뉴에서 역할을 제거합니다.
        /// </summary>
        /// <param name="menuId">메뉴 ID</param>
        /// <param name="roleId">역할 ID</param>
        /// <returns>성공 여부</returns>
        Task<bool> RemoveRoleFromMenuAsync(int menuId, int roleId);

        /// <summary>
        /// 메뉴에 특정 역할이 할당되어 있는지 확인합니다.
        /// </summary>
        /// <param name="menuId">메뉴 ID</param>
        /// <param name="roleId">역할 ID</param>
        /// <returns>역할 할당 여부</returns>
        Task<bool> MenuHasRoleAsync(int menuId, int roleId);

        /// <summary>
        /// 고급 검색 조건으로 페이징된 메뉴 목록을 가져옵니다.
        /// </summary>
        /// <param name="pageNumber">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지 크기</param>
        /// <param name="searchCriteria">검색 조건 목록</param>
        /// <param name="searchLogicalOperator">검색 조건간 논리 연산자 (AND/OR)</param>
        /// <returns>메뉴 목록과 전체 개수</returns>
        Task<(IEnumerable<MenuEntity> Items, int TotalCount)> GetPagedWithAdvancedSearchAsync(
            int pageNumber,
            int pageSize,
            List<SearchCriteria> searchCriteria,
            string searchLogicalOperator = "AND");

        // 계층적 메뉴 데이터 조회를 위한 메서드 추가
        Task<(IEnumerable<MenuEntity> Items, int TotalCount)> GetHierarchicalMenuDataAsync(
            int page,
            int pageSize,
            string searchTerm = "",
            string sortColumn = "",
            bool ascending = true);
    }
}