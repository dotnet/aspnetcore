// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Identity.DefaultUI.WebSite;
using Identity.DefaultUI.WebSite.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public class MapIdentityTests : LoggedTest
{
    private string Username { get; } = $"{Guid.NewGuid()}@example.com";
    private string Password { get; } = $"[PLACEHOLDER]-1a";

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanRegisterUser(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/identity/register", new { Username, Password });

        response.EnsureSuccessStatusCode();
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanLoginWithBearerToken(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password });
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        loginResponse.EnsureSuccessStatusCode();
        Assert.False(loginResponse.Headers.Contains(HeaderNames.SetCookie));

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tokenType = loginContent.GetProperty("token_type").GetString();
        var accessToken = loginContent.GetProperty("access_token").GetString();
        var expiresIn = loginContent.GetProperty("expires_in").GetDouble();

        Assert.Equal("Bearer", tokenType);
        Assert.Equal(3600, expiresIn);

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));
    }

    [Fact]
    public async Task CanCustomizeBearerTokenExpiration()
    {
        var clock = new MockTimeProvider();
        var expireTimeSpan = TimeSpan.FromSeconds(42);

        await using var app = await CreateAppAsync(services =>
        {
            services.AddIdentityCore<ApplicationUser>().AddApiEndpoints().AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.BearerTokenExpiration = expireTimeSpan;
                options.TimeProvider = clock;
            });
        });

        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password });
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();
        var expiresIn = loginContent.GetProperty("expires_in").GetDouble();

        Assert.Equal(expireTimeSpan.TotalSeconds, expiresIn);

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        // Works without time passing.
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));

        clock.Advance(TimeSpan.FromSeconds(expireTimeSpan.TotalSeconds - 1));

        // Still works without one second before expiration.
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));

        clock.Advance(TimeSpan.FromSeconds(1));
        var unauthorizedResponse = await client.GetAsync("/auth/hello");

        // Fails the second the BearerTokenExpiration elapses.
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
        Assert.Equal(0, unauthorizedResponse.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task CanLoginWithCookies()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password });
        var loginResponse = await client.PostAsJsonAsync("/identity/login?cookieMode=true", new { Username, Password });

        loginResponse.EnsureSuccessStatusCode();
        Assert.Equal(0, loginResponse.Content.Headers.ContentLength);

        Assert.True(loginResponse.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookieHeaders));
        var setCookieHeader = Assert.Single(setCookieHeaders);

        // The compiler does not see Assert.True's DoesNotReturnIfAttribute :(
        if (setCookieHeader.Split(';', 2) is not [var cookieHeader, _])
        {
            throw new Exception("Invalid Set-Cookie header!");
        }

        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, cookieHeader);
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));
    }

    [Fact]
    public async Task CannotLoginWithCookiesWithOnlyCoreServices()
    {
        await using var app = await CreateAppAsync(AddIdentityEndpointsBearerOnly);
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password });

        await Assert.ThrowsAsync<InvalidOperationException>(()
            => client.PostAsJsonAsync("/identity/login?cookieMode=true", new { Username, Password }));
    }

    [Fact]
    public async Task CanReadBearerTokenFromQueryString()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddIdentityCore<ApplicationUser>().AddApiEndpoints().AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.Events.OnMessageReceived = context =>
                {
                    context.Token = (string?)context.Request.Query["access_token"];
                    return Task.CompletedTask;
                };
            });
        });

        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password });
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();

        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync($"/auth/hello?access_token={accessToken}"));

        // The normal header still works
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync($"/auth/hello"));
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task Returns401UnauthorizedStatusGivenNoBearerTokenOrCookie(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        var unauthorizedResponse = await client.GetAsync($"/auth/hello");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
    }

    private async Task<WebApplication> CreateAppAsync<TUser, TContext>(Action<IServiceCollection>? configureServices)
        where TUser : class, new()
        where TContext : DbContext
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(LoggerFactory);
        builder.Services.AddAuthorization();

        var dbConnection = new SqliteConnection($"DataSource=:memory:");
        builder.Services.AddDbContext<TContext>(options => options.UseSqlite(dbConnection));
        // Dispose SqliteConnection with host by registering as a singleton factory.
        builder.Services.AddSingleton(() => dbConnection);

        configureServices ??= AddIdentityEndpoints;
        configureServices(builder.Services);

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGroup("/identity").MapIdentityApi<TUser>();

        var authGroup = app.MapGroup("/auth").RequireAuthorization();
        authGroup.MapGet("/hello",
            (ClaimsPrincipal user) => $"Hello, {user.Identity?.Name}!");

        await dbConnection.OpenAsync();
        await app.Services.GetRequiredService<TContext>().Database.EnsureCreatedAsync();
        await app.StartAsync();

        return app;
    }

    private static void AddIdentityEndpoints(IServiceCollection services)
        => services.AddIdentityApiEndpoints<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>();

    private static void AddIdentityEndpointsBearerOnly(IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme);
    }

    private Task<WebApplication> CreateAppAsync(Action<IServiceCollection>? configureServices = null)
        => CreateAppAsync<ApplicationUser, ApplicationDbContext>(configureServices);

    private static Dictionary<string, Action<IServiceCollection>> AddIdentityActions { get; } = new()
    {
        [nameof(AddIdentityEndpoints)] = AddIdentityEndpoints,
        [nameof(AddIdentityEndpointsBearerOnly)] = AddIdentityEndpointsBearerOnly,
    };

    public static object[][] AddIdentityModes => AddIdentityActions.Keys.Select(key => new object[] { key }).ToArray();
}
