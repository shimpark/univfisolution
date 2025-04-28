using UnivFI.Application.DTOs;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.UserRole
{
    /// <summary>
    /// 사용자 역할 관리 화면에 사용되는 ViewModel
    /// </summary>
    public class UserRoleManageViewModel
    {
        /// <summary>
        /// 사용자 ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 사용자 이름
        /// </summary>
        public required string UserName { get; set; }

        /// <summary>
        /// 사용자 이메일
        /// </summary>
        public required string UserEmail { get; set; }

        /// <summary>
        /// 사용자에게 현재 할당된 역할 목록
        /// </summary>
        public required List<UserRoleDto> AssignedRoles { get; set; }

        /// <summary>
        /// 사용자에게 할당할 수 있는 역할 목록
        /// </summary>
        public required List<RoleDto> AvailableRoles { get; set; }

        /// <summary>
        /// 역할이 하나 이상 할당되어 있는지 여부
        /// </summary>
        public bool HasAssignedRoles => AssignedRoles != null && AssignedRoles.Any();

        /// <summary>
        /// 할당 가능한 역할이 있는지 여부
        /// </summary>
        public bool HasAvailableRoles => AvailableRoles != null && AvailableRoles.Any();
    }
}