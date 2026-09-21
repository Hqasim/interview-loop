using InterviewLoop.Api.Dtos;

namespace InterviewLoop.Api.Services;

/// <summary>
/// Used when no Gemini API key is configured, so the app is fully clickable end-to-end
/// before you've obtained a key. Swapped for GeminiGradingService automatically once
/// Gemini:ApiKey is set (see Program.cs).
/// </summary>
public class FakeGradingService : IGradingService
{
    public Task<AttemptFeedbackDto> GradeAsync(string promptTitle, string promptDescription, string code, string language, CancellationToken ct = default)
    {
        var feedback = new AttemptFeedbackDto(
            Score: 72,
            Verdict: "Partially Correct",
            CorrectnessNotes: "[Demo mode - no Gemini API key configured] This is placeholder feedback so you can test the full flow. Set Gemini:ApiKey in appsettings or the GEMINI__APIKEY env var to get real AI-graded feedback.",
            ComplexityNotes: "Demo mode: complexity analysis unavailable.",
            ClarityNotes: "Demo mode: clarity analysis unavailable.",
            Suggestions: "Add your Gemini API key to switch from demo feedback to real AI grading."
        );
        return Task.FromResult(feedback);
    }
}
