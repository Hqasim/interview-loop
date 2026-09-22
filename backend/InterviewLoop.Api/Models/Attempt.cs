namespace InterviewLoop.Api.Models;

/// <summary>
/// One graded submission: the candidate's code for a <see cref="Prompt"/>, plus the AI
/// feedback it received. Immutable once created - there's no edit/re-grade endpoint, so an
/// attempt is a permanent record of "this code got this feedback at this time," which is what
/// powers the history view.
/// </summary>
public class Attempt
{
    public int Id { get; set; }
    public int PromptId { get; set; }
    public Prompt? Prompt { get; set; }

    public required string Code { get; set; }

    /// <summary>Language the candidate selected in the editor (e.g. "javascript", "python") -
    /// stored per-attempt since a candidate can retry the same prompt in a different language.</summary>
    public required string Language { get; set; }

    // The five fields below mirror InterviewLoop.Api.Dtos.AttemptFeedbackDto field-for-field -
    // they're what the AI grading service (see Services/IGradingService.cs) returns, persisted
    // flat on the attempt rather than as a separate related table, since feedback never exists
    // independently of the attempt it was generated for.
    public int Score { get; set; }
    public required string Verdict { get; set; }
    public required string CorrectnessNotes { get; set; }
    public required string ComplexityNotes { get; set; }
    public required string ClarityNotes { get; set; }
    public required string Suggestions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
