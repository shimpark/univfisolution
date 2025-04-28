using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnivFI.Application.DTOs;
using UnivFI.Application.Helpers;
using UnivFI.Application.Interfaces.Services;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;

namespace UnivFI.Application.Services
{
    public class SystemInitService : ISystemInitService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ILogger<SystemInitService> _logger;

        public SystemInitService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            ILogger<SystemInitService> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _logger = logger;
        }

        public async Task InitializeAdminAccountAsync()
        {
            try
            {
                _logger.LogInformation("Checking and initializing admin account and Administrator role...");

                // 1. Check if admin user exists
                var adminUser = await _userRepository.GetByUserNameAsync("admin");
                int adminUserId = 0;

                // 2. Create admin user if not exists
                if (adminUser == null)
                {
                    _logger.LogInformation("Admin user not found. Creating admin account...");

                    var salt = PasswordHasher.GenerateSalt();
                    var hashedPassword = PasswordHasher.HashPassword("admin123!@#", salt);

                    var newUser = new UserEntity
                    {
                        UserName = "admin",
                        Password = hashedPassword,
                        Salt = salt,
                        Name = "Administrators",
                        Email = "admin@example.com",
                        CreatedAt = DateTime.UtcNow
                    };

                    adminUserId = await _userRepository.CreateAsync(newUser);
                    _logger.LogInformation("Admin user created successfully with ID: {AdminUserId}", adminUserId);
                }
                else
                {
                    adminUserId = adminUser.Id;
                    _logger.LogInformation("Admin user already exists with ID: {AdminUserId}", adminUserId);
                }

                // 3. Check if Administrator role exists
                var adminRole = (await _roleRepository.GetAllAsync())
                    .FirstOrDefault(r => r.RoleName == "Administrators");
                int adminRoleId = 0;

                // 4. Create Administrator role if not exists
                if (adminRole == null)
                {
                    _logger.LogInformation("Administrators role not found. Creating...");
                    var newRole = new RoleEntity
                    {
                        RoleName = "Administrators",
                        RoleComment = "슈퍼관리자"
                    };

                    adminRoleId = await _roleRepository.CreateAsync(newRole);
                    _logger.LogInformation("Administrators role created successfully with ID: {AdminRoleId}", adminRoleId);
                }
                else
                {
                    adminRoleId = adminRole.Id;
                    _logger.LogInformation("Administrators role already exists with ID: {AdminRoleId}", adminRoleId);
                }

                // 5. Assign Administrator role to admin user if not already assigned
                var userHasAdminRole = await _userRoleRepository.UserHasRoleAsync(adminUserId, adminRoleId);
                if (!userHasAdminRole)
                {
                    _logger.LogInformation("Assigning Administrator role to admin user...");
                    await _userRoleRepository.AssignRoleToUserAsync(adminUserId, adminRoleId);
                    _logger.LogInformation("Administrator role assigned to admin user successfully");
                }
                else
                {
                    _logger.LogInformation("Admin user already has Administrator role");
                }

                _logger.LogInformation("System initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during system initialization: {ErrorMessage}", ex.Message);
                throw;
            }
        }
    }
}