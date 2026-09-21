using System.Text.Json;
using System.Text.Json.Serialization;
using InterviewLoop.Api.Dtos;

namespace InterviewLoop.Api.Services;

public class GeminiOptions
{
    public const string SectionName = "Gemini";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-2.5-flash";
}

public class GeminiGradingService(HttpClient httpClient, Microsoft.Extensions.Options.IOptions<GeminiOptions> options, ILogger<GeminiGradingService> logger) : IGradingService
{
    private readonly GeminiOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        using var response = await httpClient.PostAsJsonAsync(url, requestBody, JsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Gemini API call failed with {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new GradingException("The AI grading service is unavailable right now. Please try again shortly.");
        }

        var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, ct)
            ?? throw new GradingException("Received an empty response from the AI grading service.");

        var text = payload.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
            ?? throw new GradingException("The AI grading service returned no feedback.");

        var feedback = JsonSerializer.Deserialize<AttemptFeedbackDto>(text, JsonOptions)
            ?? throw new GradingException("Could not parse the AI grading response.");

        return feedback;
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

    private record GeminiRequest(
        [property: JsonPropertyName("contents")] GeminiContent[] Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig
    );

    private record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] GeminiPart[] Parts
    );

    private record GeminiPart([property: JsonPropertyName("text")] string Text);

    private record GeminiGenerationConfig(
        [property: JsonPropertyName("responseMimeType")] string ResponseMimeType,
        [property: JsonPropertyName("responseSchema")] object ResponseSchema
    );

    private record GeminiResponse([property: JsonPropertyName("candidates")] GeminiCandidate[]? Candidates);
    private record GeminiCandidate([property: JsonPropertyName("content")] GeminiContent? Content);
}

public class GradingException(string message) : Exception(message);
