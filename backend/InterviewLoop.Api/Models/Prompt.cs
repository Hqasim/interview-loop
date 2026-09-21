namespace InterviewLoop.Api.Models;

public class Prompt
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Difficulty { get; set; } // Easy | Medium | Hard
    public required string Description { get; set; }
    public string? StarterCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
