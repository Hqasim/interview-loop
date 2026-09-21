namespace InterviewLoop.Api.Dtos;

public record SubmitAttemptRequest(int PromptId, string Code, string Language);

public record AttemptFeedbackDto(
    int Score,
    string Verdict,
    string CorrectnessNotes,
    string ComplexityNotes,
    string ClarityNotes,
    string Suggestions
);

public record AttemptDetailDto(
    int Id,
    int PromptId,
    string PromptTitle,
    string Code,
    string Language,
    DateTime CreatedAt,
    AttemptFeedbackDto Feedback
);

public record AttemptSummaryDto(
    int Id,
    int PromptId,
    string PromptTitle,
    int Score,
    string Verdict,
    DateTime CreatedAt
);
