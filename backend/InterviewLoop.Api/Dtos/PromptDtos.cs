namespace InterviewLoop.Api.Dtos;

/// <summary>Shape returned by <c>GET /api/prompts</c> - just enough to render the prompt list
/// (title + difficulty badge) without shipping every prompt's full description up front.</summary>
public record PromptSummaryDto(int Id, string Title, string Difficulty);

/// <summary>Shape returned by <c>GET /api/prompts/{id}</c> - the full prompt, including the
/// description and starter code needed to render the solving workspace.</summary>
public record PromptDetailDto(int Id, string Title, string Difficulty, string Description, string? StarterCode);
