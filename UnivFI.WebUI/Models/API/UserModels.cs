using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using UnivFI.Application.DTOs;

namespace UnivFI.WebUI.Models.API
{
    /// <summary>
    /// 사용자 생성 요청 모델 (역할 할당 포함)
    /// </summary>
    public class CreateUserWithRolesRequest
    {
        [Required(ErrorMessage = "사용자 이름은 필수입니다.")]
        [StringLength(50, ErrorMessage = "사용자 이름은 최대 50자까지 가능합니다.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "비밀번호는 필수입니다.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "비밀번호는 6자 이상이어야 합니다.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "이름은 필수입니다.")]
        [StringLength(50, ErrorMessage = "이름은 최대 50자까지 가능합니다.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "이메일은 필수입니다.")]
        [EmailAddress(ErrorMessage = "유효한 이메일 주소를 입력하세요.")]
        [StringLength(100, ErrorMessage = "이메일은 최대 100자까지 가능합니다.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 할당할 역할 ID 목록
        /// </summary>
        [Required(ErrorMessage = "최소 하나 이상의 역할을 선택해야 합니다.")]
        public List<int> RoleIds { get; set; } = new List<int>();

        /// <summary>
        /// CreateUserDto로 변환
        /// </summary>
        public CreateUserDto ToCreateUserDto()
        {
            return new CreateUserDto
            {
                UserName = this.UserName,
                Password = this.Password,
                Name = this.Name,
                Email = this.Email
            };
        }
    }
}