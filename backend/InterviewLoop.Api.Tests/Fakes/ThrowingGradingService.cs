using InterviewLoop.Api.Dtos;
using InterviewLoop.Api.Services;

namespace InterviewLoop.Api.Tests.Fakes;

/// <summary>
/// An IGradingService that always fails - used via ApiTestFactory.GradingServiceOverride to
/// test AttemptsController's 502 path (a real grading outage) without needing the Gemini
/// service itself to actually be down.
/// </summary>
public class ThrowingGradingService : IGradingService
{
    public Task<AttemptFeedbackDto> GradeAsync(string promptTitle, string promptDescription, string code, string language, CancellationToken ct = default)
        => throw new GradingException("Simulated grading outage for testing.");
}
