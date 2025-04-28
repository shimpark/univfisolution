using System;
using System.Collections.Generic;

namespace UnivFI.Application.DTOs
{
    public class MenuDto
    {
        public int Id { get; set; }
        public required string MenuKey { get; set; }
        public required string Url { get; set; }
        public required string Title { get; set; }
        public int? ParentId { get; set; }
        public int? MenuOrder { get; set; }
        public int? Levels { get; set; }
        public bool? UseNewIcon { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateMenuDto
    {
        public required string MenuKey { get; set; }
        public required string Url { get; set; }
        public required string Title { get; set; }
        public int? ParentId { get; set; }
        public int? MenuOrder { get; set; }
        public int? Levels { get; set; }
        public bool? UseNewIcon { get; set; }
    }

    public class UpdateMenuDto
    {
        public int Id { get; set; }
        public required string MenuKey { get; set; }
        public required string Url { get; set; }
        public required string Title { get; set; }
        public int? ParentId { get; set; }
        public int? MenuOrder { get; set; }
        public int? Levels { get; set; }
        public bool? UseNewIcon { get; set; }
    }
}