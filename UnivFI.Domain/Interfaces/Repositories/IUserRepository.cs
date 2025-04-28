using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;

namespace UnivFI.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserEntity>> GetAllAsync();
        Task<IEnumerable<UserEntity>> GetListAsync(int page, int pageSize, string? searchTerm = null, string? searchFields = null, string? sortOrder = null);
        Task<int> GetTotalCountAsync(string? searchTerm = null, string? searchFields = null);
        Task<UserEntity?> GetByIdAsync(int id);
        Task<int> CreateAsync(UserEntity user);
        Task<bool> UpdateAsync(UserEntity user);
        Task<bool> DeleteAsync(int id);

        // 인증 관련 메서드 추가
        Task<UserEntity?> GetByUserNameAsync(string userName);

        // 비밀번호 업데이트 메서드
        /// <summary>
        /// 사용자의 비밀번호를 업데이트합니다.
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="hashedPassword">해시된 새 비밀번호</param>
        /// <param name="salt">새로운 salt 값</param>
        /// <returns>업데이트 성공 여부</returns>
        Task<bool> UpdatePasswordAsync(int userId, string hashedPassword, string salt);

        // 리프레시 토큰 관련 메서드
        Task<bool> SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiryDate);
        Task<UserEntity?> GetByRefreshTokenAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(int userId);
    }
}
