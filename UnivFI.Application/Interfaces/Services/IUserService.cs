using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnivFI.Application.DTOs;

namespace UnivFI.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<IEnumerable<UserDto>> GetUsersAsync(int page, int pageSize, string? searchTerm = null, string? searchFields = null, string? sortOrder = null);
        Task<int> GetTotalCountAsync(string? searchTerm = null, string? searchFields = null);
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<int> CreateUserAsync(CreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);

        // 인증 관련 메서드 추가
        Task<AuthResultDto> AuthenticateAsync(string userName, string password);

        // 비밀번호 변경 메서드
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        // 토큰 관련 메서드
        Task<bool> SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiryDate);
        Task<AuthResultDto> RefreshTokenAsync(string token, string refreshToken);
        Task<bool> RevokeTokenAsync(int userId);
    }
}
