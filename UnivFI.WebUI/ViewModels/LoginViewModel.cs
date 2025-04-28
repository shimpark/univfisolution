using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "사용자 이름은 필수입니다.")]
        [Display(Name = "사용자 이름")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "비밀번호는 필수입니다.")]
        [DataType(DataType.Password)]
        [Display(Name = "비밀번호")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "로그인 상태 유지")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}