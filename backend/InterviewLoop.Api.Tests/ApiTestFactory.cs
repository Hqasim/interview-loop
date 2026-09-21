using InterviewLoop.Api.Data;
using InterviewLoop.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InterviewLoop.Api.Tests;

/// <summary>
/// Boots the real app with an isolated in-memory database per instance, so each test that
/// creates its own factory gets a clean slate (only the seeded prompts, no cross-test bleed).
/// Set <see cref="GradingServiceOverride"/> before the first request to swap in a test double.
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    public IGradingService? GradingServiceOverride { get; set; }

    private readonly string _dbName = $"interview-loop-test-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // WebApplicationFactory defaults to the Development environment, which would load the
        // developer's real `dotnet user-secrets` (including a live Gemini API key, if set) and
        // make tests silently hit the real network instead of the deterministic fake. Pinning a
        // non-Development environment keeps tests hermetic regardless of local machine state.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // AddDbContext accumulates its options-configuration delegates (IDbContextOptionsConfiguration<T>)
            // rather than replacing them, so removing just DbContextOptions<T> leaves the original
            // UseNpgsql call still wired up alongside UseInMemoryDatabase below, and EF Core throws
            // "only a single database provider can be registered". Strip every descriptor that closes
            // over our DbContext type to guarantee a clean slate.
            var efDescriptors = services
                .Where(d => d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericArguments().Contains(typeof(InterviewLoopDbContext)))
                .ToList();
            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<InterviewLoopDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            if (GradingServiceOverride is not null)
            {
                services.RemoveAll<IGradingService>();
                services.AddSingleton(GradingServiceOverride);
            }
        });
    }
}
