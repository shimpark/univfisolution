using System;
using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.Menu
{
    public class MenuCreateViewModel
    {
        [Required(ErrorMessage = "메뉴 키는 필수 입력 항목입니다.")]
        [Display(Name = "메뉴 키")]
        public string MenuKey { get; set; }

        [Required(ErrorMessage = "URL은 필수 입력 항목입니다.")]
        [Display(Name = "URL")]
        public string Url { get; set; }

        [Display(Name = "제목")]
        public string Title { get; set; }

        [Display(Name = "새 아이콘 사용")]
        public bool? UseNewIcon { get; set; }
    }
}