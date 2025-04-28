using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;

namespace UnivFI.Domain.Interfaces.Repositories
{
    public interface IUserRoleRepository
    {
        Task<IEnumerable<UserRoleEntity>> GetAllAsync();
        Task<bool> AssignRoleToUserAsync(int userId, int roleId);
        Task<bool> RemoveRoleFromUserAsync(int userId, int roleId);
        Task<bool> UserHasRoleAsync(int userId, int roleId);
        Task<IEnumerable<UserRoleEntity>> GetByUserIdAsync(int userId);
        Task<IEnumerable<UserRoleEntity>> GetByRoleIdAsync(int roleId);

        /// <summary>
        /// 페이징 및 검색 기능을 적용한 사용자-역할 목록을 가져옵니다.
        /// </summary>
        /// <param name="searchTerm">검색어 (사용자명 또는 역할명)</param>
        /// <param name="userId">특정 사용자 ID로 필터링 (0이면 전체)</param>
        /// <param name="roleId">특정 역할 ID로 필터링 (0이면 전체)</param>
        /// <param name="page">페이지 번호</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <returns>페이징된 사용자-역할 목록과 총 항목 수</returns>
        Task<(IEnumerable<UserRoleEntity> Items, int TotalCount)> GetPagedAsync(string searchTerm, int userId, int roleId, int page, int pageSize);

        /// <summary>
        /// 특정 역할에 할당된 사용자 목록을 조회합니다.
        /// </summary>
        /// <param name="roleId">역할 ID</param>
        /// <returns>사용자 엔티티 목록</returns>
        Task<IEnumerable<UserEntity>> GetUsersByRoleIdAsync(int roleId);
    }
}