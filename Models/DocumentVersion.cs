using System.Text.Json.Serialization;

namespace MarkdownEditor.Models
{
    public class DocumentVersion
    {
        public int Id { get; set; }
        public int AccessId { get; set; }
        public string Content { get; set; }
        public DateTime SavedAt {  get; set; }

        [JsonIgnore]
        public DocumentAccess? Access { get; set; }
    }
}
