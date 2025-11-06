using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuestionService.Models;

public class Answer
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required]
    public string QuestionId { get; set; }
    [MaxLength(5000)]
    public string Content { get; set; }
    [MaxLength(36)]
    public string UserId { get; set; }
    [MaxLength(300)]
    public string UserDisplayName { get; set; }

    public bool Accepted { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore] public Question Question { get; set; } = null!;
}