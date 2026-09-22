// Application entry point and composition root.
//
// This project runs identically in two environments from the same code path:
//   - Locally, as a normal Kestrel web server (`dotnet run`).
//   - In production, as an AWS Lambda function behind a Function URL (see DEPLOYMENT.md).
// The `isLambda` checks below are the only places that branch on which environment is active;
// everything else (routing, DI, middleware) is identical in both.
using Amazon.Lambda.AspNetCoreServer.Hosting;
using InterviewLoop.Api.Data;
using InterviewLoop.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// AWS sets this environment variable automatically inside a Lambda execution environment; it's
// never present locally, which is what lets the same binary detect where it's running.
var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

// No-ops outside Lambda. Lambda Function URLs use the same payload format as
// API Gateway HTTP API v2, so HttpApi is the right event source for both.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL via Npgsql - the same connection string setting points at a local Docker container
// in development and a Neon serverless instance in production (see appsettings.json / DEPLOYMENT.md).
builder.Services.AddDbContext<InterviewLoopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

// Grading backend is chosen at startup, not per-request: a real Gemini API key wires up the real
// service; an empty one (e.g. a fresh clone with no key configured yet) falls back to a fake
// that returns clearly-labeled placeholder feedback, so the whole app is clickable out of the box.
var geminiApiKey = builder.Configuration.GetSection(GeminiOptions.SectionName)["ApiKey"];
if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    builder.Services.AddHttpClient<IGradingService, GeminiGradingService>(client =>
    {
        client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}
else
{
    builder.Services.AddSingleton<IGradingService, FakeGradingService>();
}

// Only the configured frontend origin(s) may call this API from a browser - see
// Cors:AllowedOrigins in appsettings.json (localhost:3000 locally, the Amplify URL in production).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

// Server-side throttle for the grading endpoint - see RateLimiting.cs for why this exists
// instead of (or alongside) a client-side cooldown.
builder.Services.AddGradingRateLimiter();

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    app.Logger.LogInformation("Grading service: Gemini ({Model})", builder.Configuration["Gemini:Model"]);
}
else
{
    app.Logger.LogWarning("Grading service: DEMO MODE (no Gemini:ApiKey configured) - attempts will get placeholder feedback.");
}

// Apply any pending EF Core migrations and seed the practice prompts on startup, so a fresh
// database (local Docker container or a brand-new Neon project) is immediately usable with no
// manual migration step.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InterviewLoopDbContext>();
    // Migrate() isn't supported on non-relational providers (e.g. the InMemory
    // provider tests swap in) - only real Postgres needs it.
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    SeedData.EnsureSeeded(db);
}

// Swagger/OpenAPI UI only in Development - never exposed on the public production API.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRateLimiter();
// Skip: Lambda Function URLs already terminate TLS in front of the function,
// so redirecting inside the function would just loop.
if (!isLambda)
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposes the top-level statements' generated Program class so WebApplicationFactory<Program>
// can bootstrap this app in tests.
public partial class Program;
