using System.Net;
using System.Net.Http.Json;
using InterviewLoop.Api.Dtos;
using Xunit;

namespace InterviewLoop.Api.Tests;

/// <summary>
/// End-to-end tests against the real ASP.NET Core pipeline (routing, model binding, EF Core
/// query translation) via ApiTestFactory - not unit tests of the controller class in isolation.
/// </summary>
public class PromptsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsSeededPrompts()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var prompts = await client.GetFromJsonAsync<List<PromptSummaryDto>>("/api/prompts");

        Assert.NotNull(prompts);
        Assert.Equal(10, prompts!.Count);
        Assert.Contains(prompts, p => p.Title == "Two Sum");
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/prompts/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsFullDetail()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var prompt = await client.GetFromJsonAsync<PromptDetailDto>("/api/prompts/1");

        Assert.NotNull(prompt);
        Assert.Equal("Two Sum", prompt!.Title);
        Assert.False(string.IsNullOrWhiteSpace(prompt.Description));
    }
}
