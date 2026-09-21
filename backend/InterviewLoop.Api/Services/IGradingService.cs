using InterviewLoop.Api.Dtos;

namespace InterviewLoop.Api.Services;

public interface IGradingService
{
    Task<AttemptFeedbackDto> GradeAsync(string promptTitle, string promptDescription, string code, string language, CancellationToken ct = default);
}
