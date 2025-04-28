using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.ViewModels.UIElement
{
    public class UpdateUIElementViewModel : BaseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "요소 이름은 필수 입력값입니다.")]
        [Display(Name = "요소 이름")]
        [StringLength(100, ErrorMessage = "요소 이름은 최대 100자까지 입력 가능합니다.")]
        public string ElementName { get; set; }

        [Required(ErrorMessage = "요소 타입은 필수 입력값입니다.")]
        [Display(Name = "요소 타입")]
        [StringLength(50, ErrorMessage = "요소 타입은 최대 50자까지 입력 가능합니다.")]
        public string ElementType { get; set; }

        [Display(Name = "설명")]
        [StringLength(500, ErrorMessage = "설명은 최대 500자까지 입력 가능합니다.")]
        public string Description { get; set; }
    }
}