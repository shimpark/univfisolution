using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;

namespace UnivFI.Domain.Interfaces.Services
{
    public interface IUserRoleService
    {
        // ... existing code ...

        /// <summary>
        /// 특정 역할에 할당된 사용자 목록을 조회합니다.
        /// </summary>
        /// <param name="roleId">역할 ID</param>
        /// <returns>사용자 엔티티 목록</returns>
        Task<IEnumerable<UserEntity>> GetUsersByRoleIdAsync(int roleId);
    }
}