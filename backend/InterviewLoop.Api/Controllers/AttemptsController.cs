using InterviewLoop.Api.Data;
using InterviewLoop.Api.Dtos;
using InterviewLoop.Api.Models;
using InterviewLoop.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewLoop.Api.Controllers;

[ApiController]
[Route("api/attempts")]
public class AttemptsController(InterviewLoopDbContext db, IGradingService gradingService, ILogger<AttemptsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AttemptDetailDto>> Submit(SubmitAttemptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code cannot be empty.");

        var prompt = await db.Prompts.FindAsync([request.PromptId], ct);
        if (prompt is null) return NotFound($"Prompt {request.PromptId} not found.");

        AttemptFeedbackDto feedback;
        try
        {
            feedback = await gradingService.GradeAsync(prompt.Title, prompt.Description, request.Code, request.Language, ct);
        }
        catch (GradingException ex)
        {
            logger.LogError(ex, "Grading failed for prompt {PromptId}", request.PromptId);
            return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
        }

        var attempt = new Attempt
        {
            PromptId = prompt.Id,
            Code = request.Code,
            Language = request.Language,
            Score = feedback.Score,
            Verdict = feedback.Verdict,
            CorrectnessNotes = feedback.CorrectnessNotes,
            ComplexityNotes = feedback.ComplexityNotes,
            ClarityNotes = feedback.ClarityNotes,
            Suggestions = feedback.Suggestions
        };

        db.Attempts.Add(attempt);
        await db.SaveChangesAsync(ct);

        return Ok(ToDetailDto(attempt, prompt.Title));
    }

    [HttpGet]
    public async Task<ActionResult<List<AttemptSummaryDto>>> GetHistory()
    {
        var attempts = await db.Attempts
            .Include(a => a.Prompt)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttemptSummaryDto(a.Id, a.PromptId, a.Prompt!.Title, a.Score, a.Verdict, a.CreatedAt))
            .ToListAsync();

        return Ok(attempts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AttemptDetailDto>> GetById(int id)
    {
        var attempt = await db.Attempts.Include(a => a.Prompt).FirstOrDefaultAsync(a => a.Id == id);
        if (attempt is null) return NotFound();

        return Ok(ToDetailDto(attempt, attempt.Prompt!.Title));
    }

    private static AttemptDetailDto ToDetailDto(Attempt a, string promptTitle) => new(
        a.Id, a.PromptId, promptTitle, a.Code, a.Language, a.CreatedAt,
        new AttemptFeedbackDto(a.Score, a.Verdict, a.CorrectnessNotes, a.ComplexityNotes, a.ClarityNotes, a.Suggestions)
    );
}
