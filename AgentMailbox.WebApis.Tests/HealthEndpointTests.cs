using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMailbox.WebApis.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task HealthEndpoint_ShouldRespondSuccessfully()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks();

        await using var app = builder.Build();
        app.MapHealthChecks("/health");
        await app.StartAsync();

        using var client = app.GetTestClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
    }
}
