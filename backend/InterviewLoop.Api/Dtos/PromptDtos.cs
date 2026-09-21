namespace InterviewLoop.Api.Dtos;

public record PromptSummaryDto(int Id, string Title, string Difficulty);

public record PromptDetailDto(int Id, string Title, string Difficulty, string Description, string? StarterCode);
