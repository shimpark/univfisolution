using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;

namespace UnivFI.Application.Interfaces.Services
{
    public interface IUserRoleService
    {
        Task<IEnumerable<UserRoleDto>> GetAllUserRolesAsync();
        Task<bool> AssignRoleToUserAsync(int userId, int roleId);
        Task<bool> RemoveRoleFromUserAsync(int userId, int roleId);
        Task<bool> UserHasRoleAsync(int userId, int roleId);
        Task<IEnumerable<UserRoleDto>> GetUserRolesByUserIdAsync(int userId);
        Task<IEnumerable<UserRoleDto>> GetUserRolesByRoleIdAsync(int roleId);

        /// <summary>
        /// 페이징 및 검색 기능을 적용한 사용자-역할 목록을 가져옵니다.
        /// </summary>
        /// <param name="searchTerm">검색어 (사용자명 또는 역할명)</param>
        /// <param name="userId">특정 사용자 ID로 필터링 (0이면 전체)</param>
        /// <param name="roleId">특정 역할 ID로 필터링 (0이면 전체)</param>
        /// <param name="page">페이지 번호</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <returns>페이징된 사용자-역할 목록</returns>
        Task<PagedResultDto<UserRoleDto>> GetPagedUserRolesAsync(string searchTerm, int userId, int roleId, int page, int pageSize);
        Task<IEnumerable<UserDto>> GetUsersByRoleIdAsync(int roleId);
    }
}