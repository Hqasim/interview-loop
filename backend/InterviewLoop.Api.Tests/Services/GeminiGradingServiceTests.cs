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
    private static GeminiGradingService CreateService(Func<HttpRequestMessage, HttpResponseMessage> respond, string model = "gemini-3.5-flash-lite")
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(respond))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/")
        };
        var options = Options.Create(new GeminiOptions { ApiKey = "test-key", Model = model });
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
    public async Task GradeAsync_NonSuccessStatus_TechnicalMessageHasDetailUserMessageIsGeneric()
    {
        // Mirrors the real failure mode that caused the 502s in production: Gemini returns
        // 404 with an error envelope when a model name is invalid/deprecated. The raw upstream
        // text belongs in the technical Message (for logs); end users get a generic message -
        // "model not found" is not actionable/meaningful to them.
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
        Assert.DoesNotContain("no longer available", ex.UserMessage);
        Assert.Equal("The AI grading service is temporarily unavailable. Please try again later.", ex.UserMessage);
    }

    [Fact]
    public async Task GradeAsync_RateLimited_UserMessageNamesModelWithoutLeakingRawQuotaText()
    {
        // This is the exact scenario reported against the live API: Gemini's 429 quota response
        // is a multi-paragraph billing/quota dump that must never reach the end user directly.
        var rawQuotaText = "You exceeded your current quota, please check your plan and billing details. " +
            "Quota exceeded for metric: generativelanguage.googleapis.com/generate_content_free_tier_requests, " +
            "limit: 20, model: gemini-3.5-flash-lite\nPlease retry in 10.57s.";
        var errorBody = new { error = new { code = 429, message = rawQuotaText, status = "RESOURCE_EXHAUSTED" } };
        var service = CreateService(_ => JsonResponse(HttpStatusCode.TooManyRequests, errorBody), model: "gemini-3.5-flash-lite");

        var ex = await Assert.ThrowsAsync<GradingException>(
            () => service.GradeAsync("Two Sum", "desc", "code", "javascript"));

        Assert.Contains(rawQuotaText, ex.Message);
        Assert.DoesNotContain("quota", ex.UserMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("billing", ex.UserMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gemini-3.5-flash-lite", ex.UserMessage);
        Assert.Contains("rate limit", ex.UserMessage, StringComparison.OrdinalIgnoreCase);
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

    private static HttpResponseMessage HighDemandResponse() =>
        JsonResponse(HttpStatusCode.ServiceUnavailable, new { error = new { message = "high demand" } });

    [Fact]
    public async Task GradeAsync_TransientServiceUnavailable_RetriesAndSucceeds()
    {
        // Reproduces the real flakiness seen against the live API: gemini-3.6-flash occasionally
        // 503s with "currently experiencing high demand" and recovers within a couple of seconds.
        var attempt = 0;
        var service = CreateService(_ =>
        {
            attempt++;
            return attempt < 3 ? HighDemandResponse() : JsonResponse(HttpStatusCode.OK, CandidateResponse((FeedbackJson, false)));
        });

        var feedback = await service.GradeAsync("Two Sum", "desc", "code", "javascript");

        Assert.Equal(3, attempt);
        Assert.Equal(72, feedback.Score);
    }

    [Fact]
    public async Task GradeAsync_ServiceUnavailablePersists_ThrowsAfterExhaustingRetries()
    {
        var attempt = 0;
        var service = CreateService(_ =>
        {
            attempt++;
            return HighDemandResponse();
        });

        var ex = await Assert.ThrowsAsync<GradingException>(
            () => service.GradeAsync("Two Sum", "desc", "code", "javascript"));

        Assert.Equal(3, attempt); // 1 initial attempt + 2 retries
        Assert.Contains("high demand", ex.Message);
        Assert.Equal("The AI grading service is temporarily overloaded. Please try again in a moment.", ex.UserMessage);
    }
}
