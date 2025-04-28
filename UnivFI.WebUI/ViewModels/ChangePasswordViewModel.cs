using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "현재 비밀번호는 필수입니다.")]
        [DataType(DataType.Password)]
        [Display(Name = "현재 비밀번호")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "새 비밀번호는 필수입니다.")]
        [StringLength(100, ErrorMessage = "{0}은(는) 최소 {2}자 이상이어야 합니다.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "새 비밀번호")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "새 비밀번호 확인")]
        [Compare("NewPassword", ErrorMessage = "새 비밀번호와 확인 비밀번호가 일치하지 않습니다.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}