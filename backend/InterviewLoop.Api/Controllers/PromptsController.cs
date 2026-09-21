using InterviewLoop.Api.Data;
using InterviewLoop.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewLoop.Api.Controllers;

[ApiController]
[Route("api/prompts")]
public class PromptsController(InterviewLoopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PromptSummaryDto>>> GetAll()
    {
        var prompts = await db.Prompts
            .OrderBy(p => p.Id)
            .Select(p => new PromptSummaryDto(p.Id, p.Title, p.Difficulty))
            .ToListAsync();

        return Ok(prompts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PromptDetailDto>> GetById(int id)
    {
        var prompt = await db.Prompts.FindAsync(id);
        if (prompt is null) return NotFound();

        return Ok(new PromptDetailDto(prompt.Id, prompt.Title, prompt.Difficulty, prompt.Description, prompt.StarterCode));
    }
}
