using System;
using Dapper.Contrib.Extensions;

namespace UnivFI.Domain.Entities
{
    [Table("UIElementUserPermissions")] // 인프라스트럭처 관련 속성 - 점진적 개선 대상
    public class UIElementUserPermissionEntity
    {
        [ExplicitKey] // 인프라스트럭처 관련 속성 - 점진적 개선 대상
        public int ElementId { get; set; }

        [ExplicitKey] // 인프라스트럭처 관련 속성 - 점진적 개선 대상
        public int UserId { get; set; }

        [Write(false)] // 인프라스트럭처 관련 속성 - 점진적 개선 대상
        public UIElementEntity UIElement { get; set; }

        [Write(false)] // 인프라스트럭처 관련 속성 - 점진적 개선 대상
        public UserEntity User { get; set; }
    }
}