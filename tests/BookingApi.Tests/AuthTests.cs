using System.Net;
using System.Net.Http.Json;
using BookingApi.Contracts;

namespace BookingApi.Tests;

public class AuthTests : IAsyncLifetime
{
    private readonly BookingApiFactory _factory = new();

    public Task InitializeAsync() => _factory.SeedAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsBearerToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(BookingApiFactory.AdminUsername, BookingApiFactory.AdminPassword));
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer", login!.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.True(login.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Theory]
    [InlineData("admin", "wrong-password")]
    [InlineData("someone", "test-password-123")]
    public async Task Login_WithWrongCredentials_Returns401(string username, string password)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_TooManyAttempts_Returns429()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest("admin", "wrong-password");

        HttpResponseMessage last = null!;
        for (var i = 0; i < 6; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", request);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/services")]
    [InlineData("PUT", "/api/services/1")]
    [InlineData("DELETE", "/api/services/1")]
    [InlineData("POST", "/api/masters")]
    [InlineData("GET", "/api/bookings")]
    [InlineData("GET", "/api/bookings/1")]
    [InlineData("POST", "/api/bookings/1/cancel")]
    public async Task AdminEndpoints_WithoutToken_Return401(string method, string url)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/bookings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateService()
    {
        var client = await _factory.CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/services",
            new UpsertServiceRequest("Kids haircut", "Up to 12 years", 45, 20m));
        var created = await response.Content.ReadFromJsonAsync<ServiceResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Kids haircut", created!.Name);
    }

    [Theory]
    [InlineData("/api/services")]
    [InlineData("/api/masters")]
    public async Task PublicCatalog_IsAvailableWithoutToken(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
