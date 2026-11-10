using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingApi.Contracts;
using BookingApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApi.Tests;

/// <summary>Starts the API in memory with an isolated in-memory database and a frozen clock.</summary>
public class BookingApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Monday, 7 January 2030, 08:00 UTC — before the salon opens.</summary>
    public static readonly DateTimeOffset Now = new(2030, 1, 7, 8, 0, 0, TimeSpan.Zero);

    public const string AdminUsername = "admin";
    public const string AdminPassword = "test-password-123";

    private readonly string _databaseName = $"booking-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-signing-key-that-is-long-enough-1234567890",
                ["Admin:Username"] = AdminUsername,
                ["Admin:Password"] = AdminPassword
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_databaseName));
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbSeeder.SeedAsync(db);
    }

    /// <summary>HTTP client that is already logged in as the salon administrator.</summary>
    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AdminUsername, AdminPassword));
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
