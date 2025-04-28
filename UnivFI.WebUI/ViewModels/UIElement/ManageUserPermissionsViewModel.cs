using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UnivFI.Application.DTOs;

namespace UnivFI.WebUI.ViewModels.UIElement
{
    public class ManageUserPermissionsViewModel : PaginationBaseViewModel
    {
        // 사용자 ID (권한을 부여할 특정 사용자 ID를 관리할 때 사용)
        [Required]
        public int UserId { get; set; }

        // UI 요소 ID
        public int ElementId { get; set; }

        // UI 요소 이름
        public string ElementName { get; set; }

        // UI 요소 키
        public string ElementKey { get; set; }

        // UI 요소 목록 (권한 정보 포함)
        public IEnumerable<UIElementWithPermissionDto> Elements { get; set; }

        // 사용자 목록
        public List<UserDto> Users { get; set; } = new List<UserDto>();

        // 사용자 권한 목록 (UI 상태용 뷰모델 사용)
        public List<UIElementUserPermissionViewModel> UserPermissions { get; set; } = new List<UIElementUserPermissionViewModel>();

        // 선택된 UI 요소 ID 목록
        public List<int> SelectedElementIds { get; set; } = new List<int>();

        // 페이징 속성 (호환성 유지)
        public int CurrentPage
        {
            get => Page;
            set => Page = value;
        }

        // 정렬 관련 속성
        public string SortField { get; set; }
        public string SortOrder { get; set; } = "asc";
    }
}