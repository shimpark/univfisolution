using System;
using System.ComponentModel.DataAnnotations;
using UnivFI.Application.DTOs;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.UserRole
{
    /// <summary>
    /// 사용자-역할 관계를 표현하는 뷰모델
    /// </summary>
    public class UserRoleViewModel
    {
        /// <summary>
        /// 사용자-역할 관계 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 사용자 ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 역할 ID
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// 아이디
        /// </summary>
        [Display(Name = "아이디")]
        public string UserName { get; set; }

        /// <summary>
        /// 사용자 이름
        /// </summary>
        [Display(Name = "이름")]
        public string Name { get; set; }

        /// <summary>
        /// 역할 이름
        /// </summary>
        [Display(Name = "역할")]
        public string RoleName { get; set; }

        /// <summary>
        /// 사용자 이메일
        /// </summary>
        [Display(Name = "이메일")]
        public string Email { get; set; }
    }
}