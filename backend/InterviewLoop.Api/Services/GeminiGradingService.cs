using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using InterviewLoop.Api.Dtos;

namespace InterviewLoop.Api.Services;

/// <summary>Bound from the "Gemini" config section (appsettings.json / user-secrets / the
/// Lambda's env vars) - see <see cref="SectionName"/>.</summary>
public class GeminiOptions
{
    public const string SectionName = "Gemini";
    public string ApiKey { get; set; } = "";

    /// <summary>Defaults to a current, generally-available Gemini model. Google periodically
    /// retires older model IDs (this project has hit that twice already - see git history) so
    /// this is deliberately a plain string, not a compiled-in enum, and is fully overridable via
    /// config without a code change or redeploy.</summary>
    public string Model { get; set; } = "gemini-3.5-flash-lite";
}

/// <summary>
/// Calls the Gemini API's <c>generateContent</c> endpoint with a JSON response schema that
/// constrains the model's output to exactly the shape of <see cref="AttemptFeedbackDto"/>, so
/// there's no free-text parsing of the AI's answer beyond a straight JSON deserialize.
/// Registered only when a real API key is configured (see Program.cs) - see
/// <see cref="FakeGradingService"/> for the no-key fallback.
/// </summary>
public class GeminiGradingService(HttpClient httpClient, Microsoft.Extensions.Options.IOptions<GeminiOptions> options, ILogger<GeminiGradingService> logger) : IGradingService
{
    private readonly GeminiOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Sends the prompt + candidate's code to Gemini and returns structured feedback.
    /// Throws <see cref="GradingException"/> (never a raw HTTP/JSON exception) on any failure -
    /// see the switch below for how each upstream failure mode maps to a user-safe message.</summary>
    public async Task<AttemptFeedbackDto> GradeAsync(string promptTitle, string promptDescription, string code, string language, CancellationToken ct = default)
    {
        var systemPrompt = $$"""
            You are a strict but fair senior software engineer grading a candidate's solution to a coding interview
            question. Evaluate correctness, time/space complexity, and code clarity. Be specific and reference the
            candidate's actual code when explaining issues. Respond ONLY with JSON matching the required schema.

            Problem: {{promptTitle}}
            Description: {{promptDescription}}

            Candidate's language: {{language}}
            Candidate's code:
            ```
            {{code}}
            ```
            """;

        var requestBody = new GeminiRequest(
            Contents: [new GeminiContent("user", [new GeminiPart(systemPrompt)])],
            GenerationConfig: new GeminiGenerationConfig(
                ResponseMimeType: "application/json",
                ResponseSchema: FeedbackSchema
            )
        );

        var url = $"models/{_options.Model}:generateContent?key={_options.ApiKey}";
        using var response = await SendWithRetryAsync(url, requestBody, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Gemini API call failed with {StatusCode}: {Body}", response.StatusCode, errorBody);

            // The raw upstream text (quota/billing paragraphs, etc.) is only useful for us - it
            // goes in the exception's technical Message (logged, and available to API clients
            // that want it) while end users get a short, specific, actionable message instead.
            var technicalMessage = $"AI grading service error ({(int)response.StatusCode}): {ExtractUpstreamErrorMessage(errorBody)}";
            var userMessage = response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests =>
                    $"Gemini model \"{_options.Model}\" has hit its rate limit. Please try again later.",
                HttpStatusCode.ServiceUnavailable =>
                    "The AI grading service is temporarily overloaded. Please try again in a moment.",
                _ => "The AI grading service is temporarily unavailable. Please try again later."
            };

            throw new GradingException(technicalMessage, userMessage);
        }

        var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, ct)
            ?? throw new GradingException("Received an empty response from the AI grading service.");

        // "Thinking" models can emit reasoning as separate parts (marked "thought": true) before
        // the actual answer part - skip those and concatenate whatever's left.
        var text = string.Concat(
            payload.Candidates?.FirstOrDefault()?.Content?.Parts?
                .Where(p => !p.Thought && !string.IsNullOrEmpty(p.Text))
                .Select(p => p.Text) ?? []
        );

        if (string.IsNullOrWhiteSpace(text))
            throw new GradingException("The AI grading service returned no feedback.");

        try
        {
            return JsonSerializer.Deserialize<AttemptFeedbackDto>(text, JsonOptions)
                ?? throw new GradingException("Could not parse the AI grading response.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Could not parse Gemini's response body as feedback JSON: {Text}", text);
            throw new GradingException("Could not parse the AI grading response.");
        }
    }

    // Gemini's "model is currently experiencing high demand" 503 is common and self-resolves
    // within a couple of seconds - worth a couple of quick retries before giving up.
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMilliseconds(600), TimeSpan.FromSeconds(2)];

    /// <summary>POSTs to Gemini, retrying with backoff on 503/429 (transient) responses only -
    /// every other status is returned immediately for the caller to handle.</summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(string url, GeminiRequest requestBody, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var response = await httpClient.PostAsJsonAsync(url, requestBody, JsonOptions, ct);

            var isTransient = response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests;
            if (!isTransient || attempt >= RetryDelays.Length)
            {
                return response;
            }

            logger.LogWarning(
                "Gemini API returned {StatusCode} (attempt {Attempt}/{Max}) - retrying...",
                response.StatusCode, attempt + 1, RetryDelays.Length + 1);
            response.Dispose();
            await Task.Delay(RetryDelays[attempt], ct);
        }
    }

    private static string ExtractUpstreamErrorMessage(string errorBody)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<GeminiErrorEnvelope>(errorBody, JsonOptions);
            if (!string.IsNullOrWhiteSpace(parsed?.Error?.Message))
                return parsed.Error.Message;
        }
        catch (JsonException)
        {
            // fall through to raw body below
        }

        return errorBody.Length > 200 ? errorBody[..200] + "…" : errorBody;
    }

    private static readonly object FeedbackSchema = new
    {
        type = "OBJECT",
        properties = new
        {
            score = new { type = "INTEGER", description = "Overall score from 0 to 100" },
            verdict = new { type = "STRING", @enum = new[] { "Correct", "Partially Correct", "Incorrect" } },
            correctnessNotes = new { type = "STRING" },
            complexityNotes = new { type = "STRING" },
            clarityNotes = new { type = "STRING" },
            suggestions = new { type = "STRING" }
        },
        required = new[] { "score", "verdict", "correctnessNotes", "complexityNotes", "clarityNotes", "suggestions" }
    };

    internal record GeminiRequest(
        [property: JsonPropertyName("contents")] GeminiContent[] Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig
    );

    internal record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] GeminiPart[] Parts
    );

    internal record GeminiPart(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("thought"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Thought = false
    );

    internal record GeminiGenerationConfig(
        [property: JsonPropertyName("responseMimeType")] string ResponseMimeType,
        [property: JsonPropertyName("responseSchema")] object ResponseSchema
    );

    internal record GeminiResponse([property: JsonPropertyName("candidates")] GeminiCandidate[]? Candidates);
    internal record GeminiCandidate([property: JsonPropertyName("content")] GeminiContent? Content);

    internal record GeminiErrorEnvelope([property: JsonPropertyName("error")] GeminiErrorDetail? Error);
    internal record GeminiErrorDetail([property: JsonPropertyName("message")] string? Message);
}

/// <summary>
/// <see cref="Exception.Message"/> carries the full technical detail (logged server-side, and
/// available to API clients via the error response's Details field). <see cref="UserMessage"/>
/// is the short, safe text shown to end users - defaults to the technical message when the two
/// don't need to differ (e.g. "could not parse the response").
/// </summary>
public class GradingException(string message, string? userMessage = null) : Exception(message)
{
    public string UserMessage { get; } = userMessage ?? message;
}
