using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;

namespace UnivFI.Domain.Entities
{
    [Table("UIElements")] // 인프라스트럭처 관련 속성 - 점진적 개선 대상
    public class UIElementEntity
    {
        [Key] // 인프라스트럭처 관련 속성 - 점진적 개선 대상
        public int Id { get; set; }
        public string ElementKey { get; set; }
        public string ElementName { get; set; }
        public string ElementType { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [Write(false)] // 인프라스트럭처 관련 속성 - 점진적 개선 대상
        public ICollection<UIElementUserPermissionEntity> UserPermissions { get; set; }
    }
}