using System.Net;
using System.Net.Http.Json;
using InterviewLoop.Api.Dtos;
using InterviewLoop.Api.Tests.Fakes;
using Xunit;

namespace InterviewLoop.Api.Tests;

public class AttemptsControllerTests
{
    private static SubmitAttemptRequest ValidRequest(int promptId = 1) =>
        new(promptId, "function twoSum(nums, target) { return [0, 1]; }", "javascript");

    [Fact]
    public async Task Submit_ValidRequest_ReturnsFeedback()
    {
        // No Gemini:ApiKey configured in the test host's appsettings, so the app wires up
        // FakeGradingService by default - this exercises the same demo-mode path a fresh
        // clone with no API key hits.
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/attempts", ValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var attempt = await response.Content.ReadFromJsonAsync<AttemptDetailDto>();
        Assert.NotNull(attempt);
        Assert.Equal("Two Sum", attempt!.PromptTitle);
        Assert.InRange(attempt.Feedback.Score, 0, 100);
    }

    [Fact]
    public async Task Submit_UnknownPrompt_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/attempts", ValidRequest(promptId: 9999));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Submit_EmptyCode_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/attempts", ValidRequest() with { Code = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_GradingServiceFails_ReturnsBadGateway()
    {
        // This is the regression test for the bug where a bad Gemini model name surfaced as
        // a 502 all the way to the frontend - asserts the failure mode end-to-end through the
        // real controller, not just the grading service in isolation.
        await using var factory = new ApiTestFactory { GradingServiceOverride = new ThrowingGradingService() };
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/attempts", ValidRequest());

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task History_IncludesPreviouslySubmittedAttempt()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var submitResponse = await client.PostAsJsonAsync("/api/attempts", ValidRequest());
        var submitted = await submitResponse.Content.ReadFromJsonAsync<AttemptDetailDto>();

        var history = await client.GetFromJsonAsync<List<AttemptSummaryDto>>("/api/attempts");

        Assert.NotNull(history);
        Assert.Contains(history!, a => a.Id == submitted!.Id);
    }
}
