using System;

namespace UnivFI.Application.DTOs
{
    public class UIElementDto
    {
        public int Id { get; set; }
        public string ElementKey { get; set; }
        public string ElementName { get; set; }
        public string ElementType { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateUIElementDto
    {
        public string ElementKey { get; set; }
        public string ElementName { get; set; }
        public string ElementType { get; set; }
        public string Description { get; set; }
    }

    public class UpdateUIElementDto
    {
        public string ElementName { get; set; }
        public string ElementType { get; set; }
        public string Description { get; set; }
    }

    public class UIElementWithPermissionDto : UIElementDto
    {
        public bool IsEnabled { get; set; }
    }
}