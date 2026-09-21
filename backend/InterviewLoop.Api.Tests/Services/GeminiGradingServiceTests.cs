using System.Net;
using System.Text;
using System.Text.Json;
using InterviewLoop.Api.Services;
using InterviewLoop.Api.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace InterviewLoop.Api.Tests.Services;

public class GeminiGradingServiceTests
{
    private static GeminiGradingService CreateService(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(respond))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/")
        };
        var options = Options.Create(new GeminiOptions { ApiKey = "test-key", Model = "gemini-3.6-flash" });
        return new GeminiGradingService(httpClient, options, NullLogger<GeminiGradingService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, object body) => new(status)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
    };

    private const string FeedbackJson = """
        {"score":72,"verdict":"Partially Correct","correctnessNotes":"a","complexityNotes":"b","clarityNotes":"c","suggestions":"d"}
        """;

    private static object CandidateResponse(params (string text, bool thought)[] parts) => new
    {
        candidates = new[]
        {
            new
            {
                content = new
                {
                    role = "model",
                    parts = parts.Select(p => p.thought
                        ? (object)new { text = p.text, thought = true }
                        : new { text = p.text })
                }
            }
        }
    };

    [Fact]
    public async Task GradeAsync_SimpleTextPart_ParsesFeedback()
    {
        var service = CreateService(_ => JsonResponse(HttpStatusCode.OK, CandidateResponse((FeedbackJson, false))));

        var feedback = await service.GradeAsync("Two Sum", "desc", "code", "javascript");

        Assert.Equal(72, feedback.Score);
        Assert.Equal("Partially Correct", feedback.Verdict);
    }

    [Fact]
    public async Task GradeAsync_ThoughtPartBeforeAnswer_SkipsThoughtAndParsesAnswer()
    {
        // Thinking models can emit a reasoning part (marked "thought": true) ahead of the
        // real answer. This is the exact shape that motivated the more robust extraction.
        var service = CreateService(_ => JsonResponse(HttpStatusCode.OK, CandidateResponse(
            ("reasoning about the candidate's approach...", true),
            (FeedbackJson, false)
        )));

        var feedback = await service.GradeAsync("Two Sum", "desc", "code", "javascript");

        Assert.Equal(72, feedback.Score);
        Assert.Equal("Partially Correct", feedback.Verdict);
    }

    [Fact]
    public async Task GradeAsync_NonSuccessStatus_ThrowsWithUpstreamMessage()
    {
        // Mirrors the real failure mode that caused the 502s in production: Gemini returns
        // 404 with an error envelope when a model name is invalid/deprecated.
        var errorBody = new
        {
            error = new
            {
                code = 404,
                message = "This model models/gemini-2.5-flash is no longer available to new users.",
                status = "NOT_FOUND"
            }
        };
        var service = CreateService(_ => JsonResponse(HttpStatusCode.NotFound, errorBody));

        var ex = await Assert.ThrowsAsync<GradingException>(
            () => service.GradeAsync("Two Sum", "desc", "code", "javascript"));

        Assert.Contains("no longer available", ex.Message);
    }

    [Fact]
    public async Task GradeAsync_NoCandidates_ThrowsGradingException()
    {
        var service = CreateService(_ => JsonResponse(HttpStatusCode.OK, new { candidates = Array.Empty<object>() }));

        await Assert.ThrowsAsync<GradingException>(
            () => service.GradeAsync("Two Sum", "desc", "code", "javascript"));
    }

    [Fact]
    public async Task GradeAsync_TextIsNotValidJson_ThrowsGradingException()
    {
        var service = CreateService(_ => JsonResponse(HttpStatusCode.OK, CandidateResponse(("not json", false))));

        await Assert.ThrowsAsync<GradingException>(
            () => service.GradeAsync("Two Sum", "desc", "code", "javascript"));
    }
}
