using System;
using System.Collections.Generic;

namespace ChecklistServer.Models
{
    public class Template
    {
        public Guid Id { get; set; }
        public string DataStorageUniqueId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public List<TemplateSection> Sections { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
        public bool Archived { get; set; }
    }

    public class TemplateSection
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<TemplateItem> Items { get; set; } = new();
    }

    public class TemplateItem
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "checkbox"; // e.g., checkbox, text, number, dropdown
        public List<string>? Options { get; set; }
    }
}
