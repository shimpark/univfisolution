using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UnivFI.Application.DTOs;

namespace UnivFI.WebUI.ViewModels.UIElement
{
    public class UIElementViewModel : BaseViewModel
    {
        public int Id { get; set; }

        [Display(Name = "요소 키")]
        public string ElementKey { get; set; }

        [Display(Name = "요소 이름")]
        public string ElementName { get; set; }

        [Display(Name = "요소 타입")]
        public string ElementType { get; set; }

        [Display(Name = "설명")]
        public string Description { get; set; }

        [Display(Name = "생성일")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "수정일")]
        public DateTime? UpdatedAt { get; set; }

        public List<UIElementUserPermissionViewModel> UserPermissions { get; set; } = new List<UIElementUserPermissionViewModel>();

        public static UIElementViewModel FromDto(UIElementDto dto)
        {
            if (dto == null) return null;

            var viewModel = new UIElementViewModel
            {
                Id = dto.Id,
                ElementKey = dto.ElementKey,
                ElementName = dto.ElementName,
                ElementType = dto.ElementType,
                Description = dto.Description,
                CreatedAt = dto.CreatedAt
            };

            return viewModel;
        }

        public static List<UIElementViewModel> FromDtoList(IEnumerable<UIElementDto> dtos)
        {
            if (dtos == null) return new List<UIElementViewModel>();

            var viewModels = new List<UIElementViewModel>();
            foreach (var dto in dtos)
            {
                viewModels.Add(FromDto(dto));
            }

            return viewModels;
        }
    }

    public class UIElementUserPermissionViewModel
    {
        public int ElementId { get; set; }
        public int UserId { get; set; }
        public bool IsEnabled { get; set; } // UI 표시 용도로만 사용 (DB에는 저장되지 않음)

        public string ElementKey { get; set; }
        public string ElementName { get; set; }
        public string ElementType { get; set; }

        // 사용자 정보
        public UserDto User { get; set; }

        // 뷰에서 사용하는 속성들
        public string UserName => User?.UserName;
        public string Name => User?.Name;
        public string Email => User?.Email;
        public DateTime? GrantedAt => User?.CreatedAt;

        public static UIElementUserPermissionViewModel FromDto(UIElementUserPermissionDto dto)
        {
            if (dto == null) return null;

            return new UIElementUserPermissionViewModel
            {
                ElementId = dto.ElementId,
                UserId = dto.UserId,
                IsEnabled = true, // 레코드가 있으면 항상 활성화 상태
                ElementKey = dto.ElementKey,
                ElementName = dto.ElementName,
                ElementType = dto.ElementType,
                User = dto.User
            };
        }
    }
}