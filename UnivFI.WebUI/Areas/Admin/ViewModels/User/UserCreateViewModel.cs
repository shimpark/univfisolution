using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.User
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "아이디는 필수입니다.")]
        [StringLength(50, ErrorMessage = "아이디는 최대 50자까지 가능합니다.")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "아이디는 영문자, 숫자, 하이픈(-), 언더스코어(_)만 사용 가능합니다.")]
        [Display(Name = "아이디")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "이름은 필수입니다.")]
        [StringLength(100, ErrorMessage = "이름은 최대 100자까지 가능합니다.")]
        [Display(Name = "이름")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "이메일은 필수입니다.")]
        [EmailAddress(ErrorMessage = "유효한 이메일 주소를 입력하세요.")]
        [Display(Name = "이메일")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "비밀번호는 필수입니다.")]
        [StringLength(100, ErrorMessage = "비밀번호는 최소 {2}자 이상이어야 합니다.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "비밀번호")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "비밀번호 확인")]
        [Compare("Password", ErrorMessage = "비밀번호와 확인 비밀번호가 일치하지 않습니다.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}