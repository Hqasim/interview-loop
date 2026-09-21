namespace InterviewLoop.Api.Models;

public class Attempt
{
    public int Id { get; set; }
    public int PromptId { get; set; }
    public Prompt? Prompt { get; set; }

    public required string Code { get; set; }
    public required string Language { get; set; }

    public int Score { get; set; }
    public required string Verdict { get; set; }
    public required string CorrectnessNotes { get; set; }
    public required string ComplexityNotes { get; set; }
    public required string ClarityNotes { get; set; }
    public required string Suggestions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
