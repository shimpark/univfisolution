using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mapster;
using MapsterMapper;
using UnivFI.Application.DTOs;
using UnivFI.Application.Interfaces.Services;
using UnivFI.Domain.Entities;
using UnivFI.Application.Helpers;
using Microsoft.Extensions.Logging;
using UnivFI.Domain.Interfaces.Repositories;
using System.Linq;

namespace UnivFI.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IMapper mapper)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetListAsync(1, int.MaxValue);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<IEnumerable<UserDto>> GetUsersAsync(int page, int pageSize, string? searchTerm = null, string? searchFields = null, string? sortOrder = null)
        {
            var users = await _userRepository.GetListAsync(page, pageSize, searchTerm, searchFields, sortOrder);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null, string? searchFields = null)
        {
            return await _userRepository.GetTotalCountAsync(searchTerm, searchFields);
        }

        public async Task<AuthResultDto> AuthenticateAsync(string userName, string password)
        {
            var result = new AuthResultDto { Success = false };

            try
            {
                var user = await _userRepository.GetByUserNameAsync(userName);

                if (user == null)
                {
                    result.ErrorMessage = "사용자를 찾을 수 없습니다.";
                    return result;
                }

                var hashedPassword = PasswordHasher.HashPassword(password, user.Salt);
                if (hashedPassword != user.Password)
                {
                    result.ErrorMessage = "비밀번호가 일치하지 않습니다.";
                    return result;
                }

                result.Success = true;
                result.User = _mapper.Map<UserDto>(user);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 인증 중 오류 발생: {Message}", ex.Message);
                result.ErrorMessage = "인증 중 오류가 발생했습니다.";
                return result;
            }
        }

        public async Task<int> CreateUserAsync(CreateUserDto createUserDto)
        {
            var salt = PasswordHasher.GenerateSalt();
            var hashedPassword = PasswordHasher.HashPassword(createUserDto.Password, salt);

            var user = new UserEntity
            {
                UserName = createUserDto.UserName,
                Email = createUserDto.Email,
                Password = hashedPassword,
                Salt = salt,
                CreatedAt = DateTime.UtcNow
            };

            return await _userRepository.CreateAsync(user);
        }

        public async Task<bool> UpdateUserAsync(UpdateUserDto updateUserDto)
        {
            var existingUser = await _userRepository.GetByIdAsync(updateUserDto.Id);
            if (existingUser == null)
            {
                return false;
            }

            existingUser.UserName = updateUserDto.UserName;
            existingUser.Email = updateUserDto.Email;
            existingUser.UpdatedAt = DateTime.UtcNow;

            return await _userRepository.UpdateAsync(existingUser);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _userRepository.DeleteAsync(id);
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("비밀번호 변경 실패: 사용자를 찾을 수 없음 (ID: {UserId})", userId);
                    return false;
                }

                var hashedCurrentPassword = PasswordHasher.HashPassword(currentPassword, user.Salt);
                if (hashedCurrentPassword != user.Password)
                {
                    _logger.LogWarning("비밀번호 변경 실패: 현재 비밀번호가 일치하지 않음 (ID: {UserId})", userId);
                    return false;
                }

                var newSalt = PasswordHasher.GenerateSalt();
                var hashedNewPassword = PasswordHasher.HashPassword(newPassword, newSalt);

                return await _userRepository.UpdatePasswordAsync(userId, hashedNewPassword, newSalt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "비밀번호 변경 중 오류 발생: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiryDate)
        {
            return await _userRepository.SaveRefreshTokenAsync(userId, refreshToken, expiryDate);
        }

        public async Task<bool> RevokeRefreshTokenAsync(int userId)
        {
            return await _userRepository.RevokeRefreshTokenAsync(userId);
        }

        public async Task<AuthResultDto> RefreshTokenAsync(string token, string refreshToken)
        {
            var result = new AuthResultDto { Success = false };

            try
            {
                var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
                if (user == null)
                {
                    result.ErrorMessage = "유효하지 않은 리프레시 토큰입니다.";
                    return result;
                }

                result.Success = true;
                result.User = _mapper.Map<UserDto>(user);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "토큰 갱신 중 오류 발생: {Message}", ex.Message);
                result.ErrorMessage = "토큰 갱신 중 오류가 발생했습니다.";
                return result;
            }
        }

        public async Task<bool> RevokeTokenAsync(int userId)
        {
            return await _userRepository.RevokeRefreshTokenAsync(userId);
        }
    }
}

