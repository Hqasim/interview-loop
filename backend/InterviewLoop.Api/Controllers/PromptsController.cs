using InterviewLoop.Api.Data;
using InterviewLoop.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewLoop.Api.Controllers;

/// <summary>
/// Read-only access to the seeded prompt catalog. No POST/PUT/DELETE - prompts are managed via
/// <see cref="Data.SeedData"/>, not through the API, so there's nothing to authorize or validate
/// here beyond "does this id exist."
/// </summary>
[ApiController]
[Route("api/prompts")]
public class PromptsController(InterviewLoopDbContext db) : ControllerBase
{
    /// <summary>GET /api/prompts - the list shown on the home page.</summary>
    [HttpGet]
    public async Task<ActionResult<List<PromptSummaryDto>>> GetAll()
    {
        var prompts = await db.Prompts
            .OrderBy(p => p.Id)
            .Select(p => new PromptSummaryDto(p.Id, p.Title, p.Difficulty))
            .ToListAsync();

        return Ok(prompts);
    }

    /// <summary>GET /api/prompts/{id} - full detail for the solving workspace. 404 if the id
    /// doesn't exist (e.g. a stale bookmark or a typo'd URL).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PromptDetailDto>> GetById(int id)
    {
        var prompt = await db.Prompts.FindAsync(id);
        if (prompt is null) return NotFound();

        return Ok(new PromptDetailDto(prompt.Id, prompt.Title, prompt.Difficulty, prompt.Description, prompt.StarterCode));
    }
}
