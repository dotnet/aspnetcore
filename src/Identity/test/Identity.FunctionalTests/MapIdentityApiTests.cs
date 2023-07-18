// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Identity.DefaultUI.WebSite;
using Identity.DefaultUI.WebSite.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public class MapIdentityApiTests : LoggedTest
{
    private string Username { get; } = $"{Guid.NewGuid()}@example.com";
    private string Password { get; } = $"[PLACEHOLDER]-1a";

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanRegisterUser(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        AssertOkAndEmpty(await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username }));
    }

    [Fact]
    public async Task RegisterFailsGivenNoEmail()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        AssertBadRequestAndEmpty(await client.PostAsJsonAsync("/identity/register", new { Username, Password }));
    }

    [Fact]
    public async Task LoginFailsGivenUnregisteredUser()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await AssertUnauthorizedAndProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "Failed");
    }

    [Fact]
    public async Task LoginFailsGivenWrongPassword()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });
        await AssertUnauthorizedAndProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password = "wrong" }),
            "Failed");
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanLoginWithBearerToken(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });
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
            services.AddSingleton<TimeProvider>(clock);
            services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
            services.AddIdentityCore<ApplicationUser>().AddApiEndpoints().AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddAuthentication(IdentityConstants.BearerScheme).AddIdentityBearerToken<ApplicationUser>(options =>
            {
                options.BearerTokenExpiration = expireTimeSpan;
            });
        });

        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();
        var expiresIn = loginContent.GetProperty("expires_in").GetDouble();

        Assert.Equal(expireTimeSpan.TotalSeconds, expiresIn);

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        // Works without time passing.
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));

        clock.Advance(TimeSpan.FromSeconds(expireTimeSpan.TotalSeconds - 1));

        // Still works one second before expiration.
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));

        clock.Advance(TimeSpan.FromSeconds(1));

        // Fails the second the BearerTokenExpiration elapses.
        AssertUnauthorizedAndEmpty(await client.GetAsync("/auth/hello"));
    }

    [Fact]
    public async Task CanLoginWithCookies()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });
        var loginResponse = await client.PostAsJsonAsync("/identity/login?cookieMode=true", new { Username, Password });

        loginResponse.EnsureSuccessStatusCode();
        Assert.Equal(0, loginResponse.Content.Headers.ContentLength);

        Assert.True(loginResponse.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookieHeaders));
        var setCookieHeader = Assert.Single(setCookieHeaders);

        // The compiler does not see Assert.True's DoesNotReturnIfAttribute :(
        if (setCookieHeader.Split(';', 2) is not [var cookieHeader, _])
        {
            throw new XunitException("Invalid Set-Cookie header!");
        }

        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, cookieHeader);
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));
    }

    [Fact]
    public async Task CannotLoginWithCookiesWithOnlyCoreServices()
    {
        await using var app = await CreateAppAsync(services => AddIdentityApiEndpointsBearerOnly(services));
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });

        await Assert.ThrowsAsync<InvalidOperationException>(()
            => client.PostAsJsonAsync("/identity/login?cookieMode=true", new { Username, Password }));
    }

    [Fact]
    public async Task CanReadBearerTokenFromQueryString()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
            services.AddIdentityCore<ApplicationUser>().AddApiEndpoints().AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddAuthentication(IdentityConstants.BearerScheme).AddIdentityBearerToken<ApplicationUser>(options =>
            {
                options.Events.OnMessageReceived = context =>
                {
                    context.Token = (string?)context.Request.Query["access_token"];
                    return Task.CompletedTask;
                };
            });
        });

        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });
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

        AssertUnauthorizedAndEmpty(await client.GetAsync($"/auth/hello"));

        client.DefaultRequestHeaders.Authorization = new("Bearer");
        AssertUnauthorizedAndEmpty(await client.GetAsync($"/auth/hello"));

        client.DefaultRequestHeaders.Authorization = new("Bearer", "");
        AssertUnauthorizedAndEmpty(await client.GetAsync($"/auth/hello"));
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanUseRefreshToken(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginContent.GetProperty("refresh_token").GetString();

        var refreshResponse = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });
        var refreshContent = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));
    }

    [Fact]
    public async Task Returns401UnauthorizedStatusGivenNullOrEmptyRefreshToken()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        string? refreshToken = null;
        AssertUnauthorizedAndEmpty(await client.PostAsJsonAsync("/identity/refresh", new { refreshToken }));

        refreshToken = "";
        AssertUnauthorizedAndEmpty(await client.PostAsJsonAsync("/identity/refresh", new { refreshToken }));
    }

    [Fact]
    public async Task CanCustomizeRefreshTokenExpiration()
    {
        var clock = new MockTimeProvider();
        var expireTimeSpan = TimeSpan.FromHours(42);

        await using var app = await CreateAppAsync(services =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
            services.AddIdentityCore<ApplicationUser>().AddApiEndpoints().AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddAuthentication(IdentityConstants.BearerScheme).AddIdentityBearerToken<ApplicationUser>(options =>
            {
                options.RefreshTokenExpiration = expireTimeSpan;
            });
        });

        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginContent.GetProperty("refresh_token").GetString();
        var accessToken = loginContent.GetProperty("refresh_token").GetString();

        // Works without time passing.
        var refreshResponse = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });
        Assert.True(refreshResponse.IsSuccessStatusCode);

        clock.Advance(TimeSpan.FromSeconds(expireTimeSpan.TotalSeconds - 1));

        // Still works one second before expiration.
        refreshResponse = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });
        Assert.True(refreshResponse.IsSuccessStatusCode);

        // The bearer token stopped working 41 hours ago with the default 1 hour expiration.
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        AssertUnauthorizedAndEmpty(await client.GetAsync("/auth/hello"));

        clock.Advance(TimeSpan.FromSeconds(1));

        // Fails the second the RefreshTokenExpiration elapses.
        AssertUnauthorizedAndEmpty(await client.PostAsJsonAsync("/identity/refresh", new { refreshToken }));

        // But the last refresh_token from the successful /refresh only a second ago has not expired.
        var refreshContent = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        refreshToken = refreshContent.GetProperty("refresh_token").GetString();

        refreshResponse = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });
        refreshContent = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        accessToken = refreshContent.GetProperty("access_token").GetString();

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));
    }

    [Fact]
    public async Task RefreshReturns401UnauthorizedIfSecurityStampChanges()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginContent.GetProperty("refresh_token").GetString();

        var userManager = app.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync(Username);

        Assert.NotNull(user);

        await userManager.UpdateSecurityStampAsync(user);

        AssertUnauthorizedAndEmpty(await client.PostAsJsonAsync("/identity/refresh", new { refreshToken }));
    }

    [Fact]
    public async Task RefreshUpdatesUserFromStore()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginContent.GetProperty("refresh_token").GetString();

        var userManager = app.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync(Username);

        Assert.NotNull(user);

        var newUsername = $"{Guid.NewGuid()}@example.org";
        user.UserName = newUsername;
        await userManager.UpdateAsync(user);

        var refreshResponse = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });
        var refreshContent = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = refreshContent.GetProperty("access_token").GetString();

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        Assert.Equal($"Hello, {newUsername}!", await client.GetStringAsync("/auth/hello"));
    }

    [Fact]
    public async Task LoginCanBeLockedOut()
    {
        await using var app = await CreateAppAsync(services =>
        {
            AddIdentityApiEndpoints(services);
            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = 2;
            });
        });
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });

        await AssertUnauthorizedAndProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password = "wrong" }),
            "Failed");

        await AssertUnauthorizedAndProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password = "wrong" }),
            "LockedOut");

        Assert.Single(TestSink.Writes, w =>
            w.LoggerName == "Microsoft.AspNetCore.Identity.SignInManager" &&
            w.EventId == new EventId(3, "UserLockedOut"));

        await AssertUnauthorizedAndProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "LockedOut");
    }

    [Fact]
    public async Task LockoutCanBeDisabled()
    {
        await using var app = await CreateAppAsync(services =>
        {
            AddIdentityApiEndpoints(services);
            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.AllowedForNewUsers = false;
                options.Lockout.MaxFailedAccessAttempts = 1;
            });
        });
        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });

        await AssertUnauthorizedAndProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password = "wrong" }),
            "Failed");

        Assert.DoesNotContain(TestSink.Writes, w =>
            w.LoggerName == "Microsoft.AspNetCore.Identity.SignInManager" &&
            w.EventId == new EventId(3, "UserLockedOut"));

        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });
        loginResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AccountConfirmationCanBeEnabled()
    {
        var emailSender = new TestEmailSender();

        await using var app = await CreateAppAsync(services =>
        {
            AddIdentityApiEndpoints(services);
            services.AddSingleton<IEmailSender>(emailSender);
            services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            });
        });
        using var client = app.GetTestClient();

        await TestRegistrationWithAccountConfirmation(client, emailSender);

        Assert.Single(TestSink.Writes, w =>
            w.LoggerName == "Microsoft.AspNetCore.Identity.SignInManager" &&
            w.EventId == new EventId(4, "UserCannotSignInWithoutConfirmedAccount"));
    }

    [Fact]
    public async Task EmailConfirmationCanBeEnabled()
    {
        var emailSender = new TestEmailSender();

        await using var app = await CreateAppAsync(services =>
        {
            AddIdentityApiEndpoints(services);
            services.AddSingleton<IEmailSender>(emailSender);
            services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
            });
        });
        using var client = app.GetTestClient();

        await TestRegistrationWithAccountConfirmation(client, emailSender);

        Assert.Single(TestSink.Writes, w =>
            w.LoggerName == "Microsoft.AspNetCore.Identity.SignInManager" &&
            w.EventId == new EventId(0, "UserCannotSignInWithoutConfirmedEmail"));
    }

    [Fact]
    public async Task CanAddEndpointsToMultipleRouteGroupsForSameUserType()
    {
        // Test with confirmation email since that tests link generation capabilities
        var emailSender = new TestEmailSender();

        await using var app = await CreateAppAsync<ApplicationUser, ApplicationDbContext>(services =>
        {
            AddIdentityApiEndpoints(services);
            services.AddSingleton<IEmailSender>(emailSender);
            services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            });
        }, autoStart: false);

        app.MapGroup("/identity2").MapIdentityApi<ApplicationUser>();

        await app.StartAsync();
        using var client = app.GetTestClient();

        await TestRegistrationWithAccountConfirmation(client, emailSender, "/identity", "a@example.com");
        await TestRegistrationWithAccountConfirmation(client, emailSender, "/identity2", "b@example.com");
    }

    [Fact]
    public async Task CanAddEndpointsToMultipleRouteGroupsForMultipleUsersTypes()
    {
        // Test with confirmation email since that tests link generation capabilities
        var emailSender = new TestEmailSender();

        // Even with OnModelCreating tricks to prefix table names, using the same database
        // for multiple user tables is difficult because index conflics, so we just use a different db.
        var dbConnection2 = new SqliteConnection($"DataSource=:memory:");

        await using var app = await CreateAppAsync<ApplicationUser, ApplicationDbContext>(services =>
        {
            AddIdentityApiEndpoints<ApplicationUser, ApplicationDbContext>(services);

            // We just added cookie and/or bearer auth scheme(s) above. We cannot re-add these without an error.
            services
                .AddDbContext<IdentityDbContext>((sp, options) => options.UseSqlite(dbConnection2))
                .AddIdentityCore<IdentityUser>()
                .AddEntityFrameworkStores<IdentityDbContext>()
                .AddApiEndpoints();

            services.AddSingleton<IDisposable>(_ => dbConnection2);

            services.AddSingleton<IEmailSender>(emailSender);
            services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            });
        }, autoStart: false);

        // The following two lines are already taken care of by CreateAppAsync for ApplicationUser and ApplicationDbContext
        await dbConnection2.OpenAsync();
        await app.Services.GetRequiredService<IdentityDbContext>().Database.EnsureCreatedAsync();

        app.MapGroup("/identity2").MapIdentityApi<IdentityUser>();

        await app.StartAsync();
        using var client = app.GetTestClient();

        // We can use the same username twice since we're using two distinct DbContexts.
        await TestRegistrationWithAccountConfirmation(client, emailSender, "/identity", Username);
        await TestRegistrationWithAccountConfirmation(client, emailSender, "/identity2", Username);
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanUseTwoFactor(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);

        var userManager = app.Services.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(userManager.SupportsUserTwoFactor);

        using var client = app.GetTestClient();

        await client.PostAsJsonAsync("/identity/register", new { Username, Password, Email = Username });

        var user = await userManager.FindByNameAsync(Username);
        Assert.NotNull(user);

        // Enable 2fa
        AssertSuccess(await userManager.ResetAuthenticatorKeyAsync(user));
        AssertSuccess(await userManager.SetTwoFactorEnabledAsync(user, enabled: true));

        // Generate 2fa code from key
        var authenticatorKey = await userManager.GetAuthenticatorKeyAsync(user);
        Assert.NotNull(authenticatorKey);
        var keyBytes = Base32.FromBase32(authenticatorKey);
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestep = Convert.ToInt64(unixTimestamp / 30);
        var twoFactorCode = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)timestep, modifierBytes: null).ToString();

        await AssertUnauthorizedAndProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "RequiresTwoFactor");

        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password, twoFactorCode });
        loginResponse.EnsureSuccessStatusCode();
    }

    private async Task<WebApplication> CreateAppAsync<TUser, TContext>(Action<IServiceCollection>? configureServices, bool autoStart = true)
        where TUser : class, new()
        where TContext : DbContext
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(LoggerFactory);
        builder.Services.AddAuthorization();

        var dbConnection = new SqliteConnection($"DataSource=:memory:");
        // Dispose SqliteConnection with host by registering as a singleton factory.
        builder.Services.AddSingleton(_ => dbConnection);

        configureServices ??= services => AddIdentityApiEndpoints<TUser, TContext>(services);
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

        if (autoStart)
        {
            await app.StartAsync();
        }

        return app;
    }

    private static IdentityBuilder AddIdentityApiEndpoints<TUser, TContext>(IServiceCollection services)
        where TUser : class, new()
        where TContext : DbContext
    {
        return services.AddDbContext<TContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()))
            .AddIdentityApiEndpoints<TUser>().AddEntityFrameworkStores<TContext>();
    }

    private static IdentityBuilder AddIdentityApiEndpoints(IServiceCollection services)
        => AddIdentityApiEndpoints<ApplicationUser, ApplicationDbContext>(services);

    private static IdentityBuilder AddIdentityApiEndpointsBearerOnly(IServiceCollection services)
    {
        services
            .AddAuthentication(IdentityConstants.BearerScheme)
            .AddIdentityBearerToken<ApplicationUser>();

        return services
            .AddDbContext<ApplicationDbContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()))
            .AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();
    }

    private Task<WebApplication> CreateAppAsync(Action<IServiceCollection>? configureServices = null)
        => CreateAppAsync<ApplicationUser, ApplicationDbContext>(configureServices);

    private static Dictionary<string, Action<IServiceCollection>> AddIdentityActions { get; } = new()
    {
        [nameof(AddIdentityApiEndpoints)] = services => AddIdentityApiEndpoints(services),
        [nameof(AddIdentityApiEndpointsBearerOnly)] = services => AddIdentityApiEndpointsBearerOnly(services),
    };

    public static object[][] AddIdentityModes => AddIdentityActions.Keys.Select(key => new object[] { key }).ToArray();

    private static void AssertOkAndEmpty(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    private static void AssertBadRequestAndEmpty(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    private static void AssertUnauthorizedAndEmpty(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    private static async Task AssertUnauthorizedAndProblemAsync(HttpResponseMessage response, string title)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Unauthorized", problem.Title);
        Assert.Equal(title, problem.Detail);
    }

    private static void AssertSuccess(IdentityResult result)
    {
        Assert.True(result.Succeeded);
    }

    private static string GetEmailConfirmationLink(Email email)
    {
        // Update if we add more links to the email.
        var confirmationMatch = Regex.Match(email.HtmlMessage, "href='(.*?)'");
        Assert.True(confirmationMatch.Success);
        Assert.Equal(2, confirmationMatch.Groups.Count);

        return WebUtility.HtmlDecode(confirmationMatch.Groups[1].Value);
    }

    private async Task TestRegistrationWithAccountConfirmation(HttpClient client, TestEmailSender emailSender, string? groupPrefix = null, string? username = null)
    {
        groupPrefix ??= "/identity";
        username ??= Username;

        await client.PostAsJsonAsync($"{groupPrefix}/register", new { username, Password, Email = username });

        var email = emailSender.Emails.Last();

        await AssertUnauthorizedAndProblemAsync(await client.PostAsJsonAsync($"{groupPrefix}/login", new { username, Password }),
            "NotAllowed");

        var confirmEmailResponse = await client.GetAsync(GetEmailConfirmationLink(email));
        confirmEmailResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync($"{groupPrefix}/login", new { username, Password });
        loginResponse.EnsureSuccessStatusCode();
    }

    private sealed class TestTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>
        where TUser : class
    {
        public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            return MakeToken(purpose, await manager.GetUserIdAsync(user));
        }

        public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
        {
            return token == MakeToken(purpose, await manager.GetUserIdAsync(user));
        }

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            return Task.FromResult(true);
        }

        private static string MakeToken(string purpose, string userId)
        {
            return string.Join(":", userId, purpose, "ImmaToken");
        }
    }

    private sealed class TestEmailSender : IEmailSender
    {
        public List<Email> Emails { get; set; } = new();

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Emails.Add(new(email, subject, htmlMessage));
            return Task.CompletedTask;
        }
    }

    private sealed record Email(string Address, string Subject, string HtmlMessage);
}
