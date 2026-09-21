namespace InterviewLoop.Api.Dtos;

/// <summary>
/// Message is the safe, user-facing text - what the frontend shows in a toast. Details, when
/// present, carries technical/upstream detail for anyone inspecting the raw response; the
/// frontend deliberately never surfaces it in the UI.
/// </summary>
public record ErrorResponseDto(string Message, string? Details = null);
