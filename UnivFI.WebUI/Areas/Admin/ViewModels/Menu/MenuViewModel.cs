using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mapster;
using Microsoft.AspNetCore.Mvc.Rendering;
using UnivFI.Domain.Entities;
using UnivFI.WebUI.Areas.Admin.ViewModels.Role;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.Menu
{
    /// <summary>
    /// 메뉴 정보를 표현하는 ViewModel
    /// </summary>
    public class MenuViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "메뉴 키는 필수입니다.")]
        [StringLength(50, ErrorMessage = "메뉴 키는 최대 50자까지 가능합니다.")]
        [Display(Name = "메뉴 키")]
        public string MenuKey { get; set; }

        [Required(ErrorMessage = "제목은 필수입니다.")]
        [StringLength(100, ErrorMessage = "제목은 최대 100자까지 가능합니다.")]
        [Display(Name = "제목")]
        public string Title { get; set; }

        [Display(Name = "URL")]
        [StringLength(500, ErrorMessage = "URL은 최대 500자까지 가능합니다.")]
        public string? Url { get; set; }

        [Display(Name = "상위 메뉴")]
        public int? ParentId { get; set; }

        [Display(Name = "메뉴 순서")]
        public short? MenuOrder { get; set; }

        [Display(Name = "레벨")]
        public short? Levels { get; set; }

        [Display(Name = "새 아이콘 사용")]
        public bool? UseNewIcon { get; set; }

        [Display(Name = "생성일")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "수정일")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "선택된 역할")]
        public int[] SelectedRoles { get; set; }
    }
}