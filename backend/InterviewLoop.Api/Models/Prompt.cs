namespace InterviewLoop.Api.Models;

/// <summary>
/// A single coding-interview question (e.g. "Two Sum"). The seeded catalog lives in
/// <see cref="Data.SeedData"/>; prompts are read-only from the API's perspective - there's no
/// create/update/delete endpoint, by design, since this is a curated practice set.
/// </summary>
public class Prompt
{
    public int Id { get; set; }
    public required string Title { get; set; }

    /// <summary>One of "Easy", "Medium", "Hard". Not an enum on purpose - it's just a display
    /// label with no behavior attached to it, so a plain string keeps the model simple.</summary>
    public required string Difficulty { get; set; }

    public required string Description { get; set; }

    /// <summary>Optional boilerplate shown in the editor when a prompt is first opened, and
    /// restored by the Reset button. Null for prompts with no scaffolding.</summary>
    public string? StarterCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Every submission made against this prompt. See <see cref="Attempt"/>.</summary>
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
