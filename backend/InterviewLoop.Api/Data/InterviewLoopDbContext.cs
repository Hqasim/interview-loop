using InterviewLoop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewLoop.Api.Data;

public class InterviewLoopDbContext(DbContextOptions<InterviewLoopDbContext> options) : DbContext(options)
{
    public DbSet<Prompt> Prompts => Set<Prompt>();
    public DbSet<Attempt> Attempts => Set<Attempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Prompt>()
            .HasMany(p => p.Attempts)
            .WithOne(a => a.Prompt)
            .HasForeignKey(a => a.PromptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
