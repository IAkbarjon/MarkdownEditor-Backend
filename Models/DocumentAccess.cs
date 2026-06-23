using System.Text.Json.Serialization;

namespace MarkdownEditor.Models
{
    public class DocumentAccess
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int DocumentId { get; set; }
        public int AccessLevel { get; set; }

        
        public User? User { get; set; }
        [JsonIgnore]
        public Document? Document { get; set; }
        [JsonIgnore]
        public ICollection<DocumentVersion>? SavedVersions { get; set; }
    }
}
