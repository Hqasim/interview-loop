using InterviewLoop.Api.Dtos;
using InterviewLoop.Api.Services;

namespace InterviewLoop.Api.Tests.Fakes;

public class ThrowingGradingService : IGradingService
{
    public Task<AttemptFeedbackDto> GradeAsync(string promptTitle, string promptDescription, string code, string language, CancellationToken ct = default)
        => throw new GradingException("Simulated grading outage for testing.");
}
