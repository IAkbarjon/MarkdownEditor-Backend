using System.Text.Json.Serialization;

namespace MarkdownEditor.Models
{
    public class Document
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Content { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        
        [JsonIgnore]
        public User? Owner { get; set; }
        public int OwnerId { get; set; }
        
        public ICollection<DocumentAccess>? DocumentAccesses { get; set; }
        
        public ICollection<DocumentVersion>? DocumentVersions { get; set; }
    }
}
