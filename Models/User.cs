using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MarkdownEditor.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public ICollection<Document>? Documents { get; set; }
        [JsonIgnore]
        public ICollection<DocumentAccess>? AccessToDocuments { get; set; }
    }
}
