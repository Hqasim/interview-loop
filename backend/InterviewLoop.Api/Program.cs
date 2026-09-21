using Amazon.Lambda.AspNetCoreServer.Hosting;
using InterviewLoop.Api.Data;
using InterviewLoop.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

// No-ops outside Lambda. Lambda Function URLs use the same payload format as
// API Gateway HTTP API v2, so HttpApi is the right event source for both.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<InterviewLoopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    app.Logger.LogInformation("Grading service: Gemini ({Model})", builder.Configuration["Gemini:Model"]);
}
else
{
    app.Logger.LogWarning("Grading service: DEMO MODE (no Gemini:ApiKey configured) - attempts will get placeholder feedback.");
}

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
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
