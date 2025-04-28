using System.ComponentModel.DataAnnotations;
using UnivFI.Application.DTOs;

namespace UnivFI.WebUI.ViewModels.UIElement
{
    public class CreateUIElementViewModel : BaseViewModel
    {
        [Required(ErrorMessage = "요소 키는 필수 항목입니다.")]
        [Display(Name = "요소 키")]
        public string ElementKey { get; set; }

        [Required(ErrorMessage = "요소 이름은 필수 항목입니다.")]
        [Display(Name = "요소 이름")]
        public string ElementName { get; set; }

        [Required(ErrorMessage = "요소 타입은 필수 항목입니다.")]
        [Display(Name = "요소 타입")]
        public string ElementType { get; set; }

        [Display(Name = "설명")]
        public string Description { get; set; }

        public CreateUIElementDto ToDto()
        {
            return new CreateUIElementDto
            {
                ElementKey = this.ElementKey,
                ElementName = this.ElementName,
                ElementType = this.ElementType,
                Description = this.Description
            };
        }
    }
}