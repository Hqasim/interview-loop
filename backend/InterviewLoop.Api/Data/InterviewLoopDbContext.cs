using InterviewLoop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewLoop.Api.Data;

/// <summary>
/// EF Core context for the two-table schema: <see cref="Prompt"/> (one) to <see cref="Attempt"/>
/// (many). Registered against Npgsql in production (see Program.cs) and swapped for the
/// InMemory provider in tests (see ApiTestFactory.cs) - the schema and query logic are identical
/// either way, only the storage backend changes.
/// </summary>
public class InterviewLoopDbContext(DbContextOptions<InterviewLoopDbContext> options) : DbContext(options)
{
    public DbSet<Prompt> Prompts => Set<Prompt>();
    public DbSet<Attempt> Attempts => Set<Attempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Deleting a prompt takes its attempts with it - there's no scenario where an orphaned
        // attempt (pointing at a prompt that no longer exists) should be allowed to exist.
        modelBuilder.Entity<Prompt>()
            .HasMany(p => p.Attempts)
            .WithOne(a => a.Prompt)
            .HasForeignKey(a => a.PromptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
