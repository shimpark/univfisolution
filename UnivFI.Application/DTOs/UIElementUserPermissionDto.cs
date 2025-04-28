using System;
using System.Collections.Generic;

namespace UnivFI.Application.DTOs
{
    public class UIElementUserPermissionDto
    {
        public int ElementId { get; set; }
        public int UserId { get; set; }

        // 네비게이션 속성
        public string ElementKey { get; set; }
        public string ElementName { get; set; }
        public string ElementType { get; set; }

        // 사용자 정보
        public UserDto User { get; set; }
    }

    public class CreateUIElementUserPermissionDto
    {
        public int ElementId { get; set; }
        public int UserId { get; set; }
    }

    public class UserElementPermissionBatchDto
    {
        public int UserId { get; set; }
        public List<int> ElementIds { get; set; }
    }
}