using System;
using System.Collections.Generic;

namespace ChecklistServer.Models
{
    public class Check
    {
        public Guid Id { get; set; }
        public string DataStorageUniqueId { get; set; } = string.Empty;
        public string TemplateUniqueId { get; set; } = string.Empty;
        public Template TemplateSnapshot { get; set; } = new();
        public List<string> CheckedElements { get; set; } = new();
        public List<CheckAnswer> Answers { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
        public string Status { get; set; } = "draft";
    }

    public class CheckAnswer
    {
        public Guid ItemId { get; set; }
        public string? Answer { get; set; }
        public string? Comment { get; set; }
        public string? ElementUniqueId { get; set; }
    }
}
