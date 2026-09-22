namespace InterviewLoop.Api.Dtos;

/// <summary>Request body for <c>POST /api/attempts</c> - a candidate's solution to grade.</summary>
public record SubmitAttemptRequest(int PromptId, string Code, string Language);

/// <summary>
/// The AI grading service's structured verdict on a submission. <see cref="Verdict"/> is one of
/// "Correct", "Partially Correct", "Incorrect" - matched by an explicit schema enum on the Gemini
/// request (see GeminiGradingService.FeedbackSchema), so the model can't return an arbitrary string.
/// </summary>
public record AttemptFeedbackDto(
    int Score,
    string Verdict,
    string CorrectnessNotes,
    string ComplexityNotes,
    string ClarityNotes,
    string Suggestions
);

/// <summary>Shape returned by <c>POST /api/attempts</c> and <c>GET /api/attempts/{id}</c> - the
/// full submission plus its feedback, used for both the immediate post-submit panel and the
/// history detail view.</summary>
public record AttemptDetailDto(
    int Id,
    int PromptId,
    string PromptTitle,
    string Code,
    string Language,
    DateTime CreatedAt,
    AttemptFeedbackDto Feedback
);

/// <summary>Shape returned by <c>GET /api/attempts</c> (the history list) - just the score/verdict
/// needed for the list row, not the full code and feedback text.</summary>
public record AttemptSummaryDto(
    int Id,
    int PromptId,
    string PromptTitle,
    int Score,
    string Verdict,
    DateTime CreatedAt
);
