namespace simple_ws_dotnet.Tests;

public class HealthEndpointTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task GetHealth_ReturnsOkWithoutAuth()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("ok", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/ws", body);
        Assert.Contains("activeConnections", body, StringComparison.OrdinalIgnoreCase);
    }
}
