using System;
using System.Collections.Generic;

namespace UnivFI.WebUI.Areas.Admin.ViewModels.User
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        public string FormattedUpdatedAt => UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
        public string FormattedLastLoginAt => LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

        public List<string> Roles { get; set; } = new List<string>();
    }
}