using InterviewLoop.Api.Dtos;

namespace InterviewLoop.Api.Services;

/// <summary>
/// Abstraction over "grade this code" so AttemptsController never talks to Gemini directly.
/// Two implementations are registered, chosen once at startup based on whether a Gemini API key
/// is configured (see Program.cs): <see cref="GeminiGradingService"/> for real AI grading, and
/// <see cref="FakeGradingService"/> as a zero-setup demo-mode fallback. Tests substitute their
/// own fakes (see backend/InterviewLoop.Api.Tests/Fakes) to exercise failure paths without a
/// network call.
/// </summary>
public interface IGradingService
{
    Task<AttemptFeedbackDto> GradeAsync(string promptTitle, string promptDescription, string code, string language, CancellationToken ct = default);
}
