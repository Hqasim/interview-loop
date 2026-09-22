using InterviewLoop.Api.Data;
using InterviewLoop.Api.Dtos;
using InterviewLoop.Api.Models;
using InterviewLoop.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace InterviewLoop.Api.Controllers;

/// <summary>
/// Submitting and retrieving graded attempts. <see cref="Submit"/> is the only endpoint in the
/// app that costs money/quota (it calls out to Gemini), which is why it's the only one behind
/// the rate limiter (see <see cref="RateLimiting"/>) - the two read endpoints are cheap local
/// DB queries and don't need throttling.
/// </summary>
[ApiController]
[Route("api/attempts")]
public class AttemptsController(InterviewLoopDbContext db, IGradingService gradingService, ILogger<AttemptsController> logger) : ControllerBase
{
    /// <summary>
    /// POST /api/attempts - grades a submission and persists the result.
    /// 400 if the code is empty, 404 if the prompt doesn't exist, 429 if the caller is
    /// submitting faster than the rate limit allows (handled by the
    /// <see cref="EnableRateLimitingAttribute"/> below before this method ever runs), 502 if the
    /// AI grading call itself fails (see <see cref="GradingException"/> for how that message is
    /// chosen).
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimiting.GradingPolicyName)]
    public async Task<ActionResult<AttemptDetailDto>> Submit(SubmitAttemptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new ErrorResponseDto("Code cannot be empty."));

        var prompt = await db.Prompts.FindAsync([request.PromptId], ct);
        if (prompt is null) return NotFound(new ErrorResponseDto($"Prompt {request.PromptId} not found."));

        AttemptFeedbackDto feedback;
        try
        {
            feedback = await gradingService.GradeAsync(prompt.Title, prompt.Description, request.Code, request.Language, ct);
        }
        catch (GradingException ex)
        {
            // ex.Message (full technical detail) goes to the server log and the response's
            // Details field; ex.UserMessage (short, safe) is what the frontend actually shows.
            logger.LogError(ex, "Grading failed for prompt {PromptId}", request.PromptId);
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponseDto(ex.UserMessage, ex.Message));
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

    /// <summary>GET /api/attempts - the history list, most recent first.</summary>
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

    /// <summary>GET /api/attempts/{id} - full detail (code + feedback) for the history detail view.</summary>
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
