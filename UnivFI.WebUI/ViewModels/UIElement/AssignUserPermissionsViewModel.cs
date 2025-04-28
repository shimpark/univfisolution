using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UnivFI.WebUI.ViewModels.UIElement
{
    /// <summary>
    /// UI 요소에 사용자 권한을 할당하기 위한 뷰모델
    /// </summary>
    public class AssignUserPermissionsViewModel
    {
        /// <summary>
        /// UI 요소 ID
        /// </summary>
        [Required(ErrorMessage = "UI 요소 ID는 필수입니다.")]
        public int ElementId { get; set; }

        /// <summary>
        /// 권한을 할당할 사용자 ID 목록
        /// </summary>
        [Required(ErrorMessage = "권한을 할당할 사용자를 선택해주세요.")]
        public List<int> UserIds { get; set; }
    }
}