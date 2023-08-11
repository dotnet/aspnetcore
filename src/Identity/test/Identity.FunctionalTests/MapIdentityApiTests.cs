// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
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
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.WebUtilities;
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
    private string Password { get; } = "[PLACEHOLDER]-1a";

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

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "Failed");
    }

    [Fact]
    public async Task LoginFailsGivenWrongPassword()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await RegisterAsync(client);
        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password = "wrong" }),
            "Failed");
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanLoginWithBearerToken(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        await RegisterAsync(client);
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
            services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.BearerTokenExpiration = expireTimeSpan;
            });
        });

        using var client = app.GetTestClient();

        await RegisterAsync(client);
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

        await RegisterAsync(client);
        var loginResponse = await client.PostAsJsonAsync("/identity/login?cookieMode=true", new { Username, Password });

        AssertOkAndEmpty(loginResponse);
        Assert.True(loginResponse.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookieHeaders));
        var setCookieHeader = Assert.Single(setCookieHeaders);

        // The compiler does not see Assert.True's DoesNotReturnIfAttribute :(
        if (setCookieHeader.Split(';', 2) is not [var cookie, _])
        {
            throw new XunitException("Invalid Set-Cookie header!");
        }

        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, cookie);
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));
    }

    [Fact]
    public async Task CannotLoginWithCookiesWithOnlyCoreServices()
    {
        await using var app = await CreateAppAsync(services => AddIdentityApiEndpointsBearerOnly(services));
        using var client = app.GetTestClient();

        await RegisterAsync(client);

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
            services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.Events.OnMessageReceived = context =>
                {
                    context.Token = (string?)context.Request.Query["access_token"];
                    return Task.CompletedTask;
                };
            });
        });

        using var client = app.GetTestClient();

        await RegisterAsync(client);
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();

        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync($"/auth/hello?access_token={accessToken}"));

        // The normal header still works
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task Returns401UnauthorizedStatusGivenNoBearerTokenOrCookie(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        AssertUnauthorizedAndEmpty(await client.GetAsync("/auth/hello"));

        client.DefaultRequestHeaders.Authorization = new("Bearer");
        AssertUnauthorizedAndEmpty(await client.GetAsync("/auth/hello"));

        client.DefaultRequestHeaders.Authorization = new("Bearer", "");
        AssertUnauthorizedAndEmpty(await client.GetAsync("/auth/hello"));
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanUseRefreshToken(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        await RegisterAsync(client);
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginContent.GetProperty("refresh_token").GetString();

        var refreshResponse = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });
        var refreshContent = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = refreshContent.GetProperty("access_token").GetString();

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
            services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.RefreshTokenExpiration = expireTimeSpan;
            });
        });

        using var client = app.GetTestClient();

        await RegisterAsync(client);
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

        await RegisterAsync(client);
        var refreshToken = await LoginAsync(client);

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

        await RegisterAsync(client);
        var refreshToken = await LoginAsync(client);

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

        await RegisterAsync(client);

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password = "wrong" }),
            "Failed");

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password = "wrong" }),
            "LockedOut");

        Assert.Single(TestSink.Writes, w =>
            w.LoggerName == "Microsoft.AspNetCore.Identity.SignInManager" &&
            w.EventId == new EventId(3, "UserLockedOut"));

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
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

        await RegisterAsync(client);

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password = "wrong" }),
            "Failed");

        Assert.DoesNotContain(TestSink.Writes, w =>
            w.LoggerName == "Microsoft.AspNetCore.Identity.SignInManager" &&
            w.EventId == new EventId(3, "UserLockedOut"));

        AssertOk(await client.PostAsJsonAsync("/identity/login", new { Username, Password }));
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

        await RegisterAsync(client);
        await LoginWithEmailConfirmationAsync(client, emailSender);

        Assert.Single(emailSender.Emails);
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

        await RegisterAsync(client);
        await LoginWithEmailConfirmationAsync(client, emailSender);

        Assert.Single(emailSender.Emails);
        Assert.Single(TestSink.Writes, w =>
            w.LoggerName == "Microsoft.AspNetCore.Identity.SignInManager" &&
            w.EventId == new EventId(0, "UserCannotSignInWithoutConfirmedEmail"));
    }

    [Fact]
    public async Task EmailConfirmationCanBeResent()
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

        await RegisterAsync(client);

        var firstEmail = Assert.Single(emailSender.Emails);
        Assert.Equal("Confirm your email", firstEmail.Subject);
        Assert.Equal(Username, firstEmail.Address);

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "NotAllowed");

        AssertOk(await client.PostAsJsonAsync("/identity/resendConfirmationEmail", new { Email = "wrong" }));
        AssertOk(await client.PostAsJsonAsync("/identity/resendConfirmationEmail", new { Email = Username }));

        // Even though both resendConfirmationEmail requests returned a 200, only one for a valid registration was sent
        Assert.Equal(2, emailSender.Emails.Count);
        var resentEmail = emailSender.Emails[1];
        Assert.Equal("Confirm your email", resentEmail.Subject);
        Assert.Equal(Username, resentEmail.Address);

        AssertOk(await client.GetAsync(GetEmailConfirmationLink(resentEmail)));
        AssertOk(await client.PostAsJsonAsync("/identity/login", new { Username, Password }));
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

        // We have to use different user names to register twice since they use the same store.
        await RegisterAsync(client, "/identity", username: "a");
        await LoginWithEmailConfirmationAsync(client, emailSender, "/identity", username: "a");

        await RegisterAsync(client, "/identity2", username: "b");
        await LoginWithEmailConfirmationAsync(client, emailSender, "/identity2", username: "b");
    }

    [Fact]
    public async Task CanAddEndpointsToMultipleRouteGroupsForMultipleUsersTypes()
    {
        // Test with confirmation email since that tests link generation capabilities
        var emailSender = new TestEmailSender();

        // Even with OnModelCreating tricks to prefix table names, using the same database
        // for multiple user tables is difficult because index conflics, so we just use a different db.
        var dbConnection2 = new SqliteConnection("DataSource=:memory:");

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
        await RegisterAsync(client, "/identity");
        await LoginWithEmailConfirmationAsync(client, emailSender, "/identity");

        await RegisterAsync(client, "/identity2");
        await LoginWithEmailConfirmationAsync(client, emailSender, "/identity2");
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanEnableAndLoginWithTwoFactor(string addIdentityMode)
    {
        await using var app = await CreateAppAsync(AddIdentityActions[addIdentityMode]);
        using var client = app.GetTestClient();

        await RegisterAsync(client);
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();
        var refreshToken = loginContent.GetProperty("refresh_token").GetString();

        AssertUnauthorizedAndEmpty(await client.GetAsync("/identity/account/2fa"));

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        // We cannot enable 2fa without verifying we can produce a valid token.
        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/account/2fa", new { Enable = true }),
            "RequiresTwoFactor");
        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/account/2fa", new { Enable = true, TwoFactorCode = "wrong" }),
            "InvalidTwoFactorCode");

        var twoFactorKeyResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/2fa");
        Assert.False(twoFactorKeyResponse.GetProperty("isTwoFactorEnabled").GetBoolean());
        Assert.False(twoFactorKeyResponse.GetProperty("isMachineRemembered").GetBoolean());

        var sharedKey = twoFactorKeyResponse.GetProperty("sharedKey").GetString();

        var keyBytes = Base32.FromBase32(sharedKey);
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestep = Convert.ToInt64(unixTimestamp / 30);
        var twoFactorCode = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)timestep, modifierBytes: null).ToString(CultureInfo.InvariantCulture);

        var enable2faResponse = await client.PostAsJsonAsync("/identity/account/2fa", new { twoFactorCode, Enable = true });
        var enable2faContent = await enable2faResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(enable2faContent.GetProperty("isTwoFactorEnabled").GetBoolean());
        Assert.False(enable2faContent.GetProperty("isMachineRemembered").GetBoolean());

        // We can still access auth'd endpoints with old access token.
        Assert.Equal($"Hello, {Username}!", await client.GetStringAsync("/auth/hello"));

        // But the refresh token is invalidated by the security stamp.
        AssertUnauthorizedAndEmpty(await client.PostAsJsonAsync("/identity/refresh", new { refreshToken }));

        client.DefaultRequestHeaders.Clear();

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "RequiresTwoFactor");

        AssertOk(await client.PostAsJsonAsync("/identity/login", new { Username, Password, twoFactorCode }));
    }

    [Fact]
    public async Task CanLoginWithRecoveryCodeAndDisableTwoFactor()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await RegisterAsync(client);
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        var twoFactorKeyResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/2fa");
        var sharedKey = twoFactorKeyResponse.GetProperty("sharedKey").GetString();

        var keyBytes = Base32.FromBase32(sharedKey);
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestep = Convert.ToInt64(unixTimestamp / 30);
        var twoFactorCode = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)timestep, modifierBytes: null).ToString(CultureInfo.InvariantCulture);

        var enable2faResponse = await client.PostAsJsonAsync("/identity/account/2fa", new { twoFactorCode, Enable = true });
        var enable2faContent = await enable2faResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(enable2faContent.GetProperty("isTwoFactorEnabled").GetBoolean());

        var recoveryCodes = enable2faContent.GetProperty("recoveryCodes").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(10, recoveryCodes.Length);

        client.DefaultRequestHeaders.Clear();

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "RequiresTwoFactor");

        var recoveryLoginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password, TwoFactorRecoveryCode = recoveryCodes[0] });

        var recoveryLoginContent = await recoveryLoginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var recoveryAccessToken = recoveryLoginContent.GetProperty("access_token").GetString();
        Assert.NotEqual(accessToken, recoveryAccessToken);

        client.DefaultRequestHeaders.Authorization = new("Bearer", recoveryAccessToken);

        var disable2faResponse = await client.PostAsJsonAsync("/identity/account/2fa", new { Enable = false });
        var disable2faContent = await disable2faResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(disable2faContent.GetProperty("isTwoFactorEnabled").GetBoolean());

        client.DefaultRequestHeaders.Clear();

        AssertOk(await client.PostAsJsonAsync("/identity/login", new { Username, Password }));
    }

    [Fact]
    public async Task CanResetSharedKey()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await RegisterAsync(client);
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        var twoFactorKeyResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/2fa");
        var sharedKey = twoFactorKeyResponse.GetProperty("sharedKey").GetString();

        var keyBytes = Base32.FromBase32(sharedKey);
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestep = Convert.ToInt64(unixTimestamp / 30);
        var twoFactorCode = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)timestep, modifierBytes: null).ToString(CultureInfo.InvariantCulture);

        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/account/2fa", new { twoFactorCode, Enable = true, ResetSharedKey = true }),
            "CannotResetSharedKeyAndEnable");

        var enable2faResponse = await client.PostAsJsonAsync("/identity/account/2fa", new { twoFactorCode, Enable = true });
        var enable2faContent = await enable2faResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(enable2faContent.GetProperty("isTwoFactorEnabled").GetBoolean());

        var resetKeyResponse = await client.PostAsJsonAsync("/identity/account/2fa", new { ResetSharedKey = true });
        var resetKeyContent = await resetKeyResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(resetKeyContent.GetProperty("isTwoFactorEnabled").GetBoolean());

        var resetSharedKey = resetKeyContent.GetProperty("sharedKey").GetString();

        var resetKeyBytes = Base32.FromBase32(sharedKey);
        var resetTwoFactorCode = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)timestep, modifierBytes: null).ToString(CultureInfo.InvariantCulture);

        // The old 2fa code no longer works
        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/account/2fa", new { twoFactorCode, Enable = true }),
            "InvalidTwoFactorCode");

        var reenable2faResponse = await client.PostAsJsonAsync("/identity/account/2fa", new { TwoFactorCode = resetTwoFactorCode, Enable = true });
        var reenable2faContent = await reenable2faResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(enable2faContent.GetProperty("isTwoFactorEnabled").GetBoolean());
    }

    [Fact]
    public async Task CanResetRecoveryCodes()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await RegisterAsync(client);
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        var twoFactorKeyResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/2fa");
        var sharedKey = twoFactorKeyResponse.GetProperty("sharedKey").GetString();

        var keyBytes = Base32.FromBase32(sharedKey);
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestep = Convert.ToInt64(unixTimestamp / 30);
        var twoFactorCode = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)timestep, modifierBytes: null).ToString(CultureInfo.InvariantCulture);

        var enable2faResponse = await client.PostAsJsonAsync("/identity/account/2fa", new { twoFactorCode, Enable = true });
        var enable2faContent = await enable2faResponse.Content.ReadFromJsonAsync<JsonElement>();
        var recoveryCodes = enable2faContent.GetProperty("recoveryCodes").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(10, enable2faContent.GetProperty("recoveryCodesLeft").GetInt32());
        Assert.Equal(10, recoveryCodes.Length);

        client.DefaultRequestHeaders.Clear();

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "RequiresTwoFactor");

        AssertOk(await client.PostAsJsonAsync("/identity/login", new { Username, Password, TwoFactorRecoveryCode = recoveryCodes[0] }));
        // Cannot reuse codes
        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password, TwoFactorRecoveryCode = recoveryCodes[0] }),
            "Failed");

        var recoveryLoginResponse = await client.PostAsJsonAsync("/identity/login", new { Username, Password, TwoFactorRecoveryCode = recoveryCodes[1] });
        var recoveryLoginContent = await recoveryLoginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var recoveryAccessToken = recoveryLoginContent.GetProperty("access_token").GetString();
        Assert.NotEqual(accessToken, recoveryAccessToken);

        client.DefaultRequestHeaders.Authorization = new("Bearer", recoveryAccessToken);

        var updated2faContent = await client.GetFromJsonAsync<JsonElement>("/identity/account/2fa");
        Assert.Equal(8, updated2faContent.GetProperty("recoveryCodesLeft").GetInt32());
        Assert.Null(updated2faContent.GetProperty("recoveryCodes").GetString());

        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/account/2fa", new { twoFactorCode, Enable = true, ResetSharedKey = true }),
            "CannotResetSharedKeyAndEnable");

        var resetRecoveryResponse = await client.PostAsJsonAsync("/identity/account/2fa", new { ResetRecoveryCodes = true });
        var resetRecoveryContent = await resetRecoveryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var resetRecoveryCodes = resetRecoveryContent.GetProperty("recoveryCodes").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(10, resetRecoveryContent.GetProperty("recoveryCodesLeft").GetInt32());
        Assert.Equal(10, resetRecoveryCodes.Length);
        Assert.Empty(recoveryCodes.Intersect(resetRecoveryCodes));

        client.DefaultRequestHeaders.Clear();

        AssertOk(await client.PostAsJsonAsync("/identity/login", new { Username, Password, TwoFactorRecoveryCode = resetRecoveryCodes[0] }));

        // Even unused codes from before the reset now fail.
        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password, TwoFactorRecoveryCode = recoveryCodes[2] }),
            "Failed");
    }

    [Fact]
    public async Task CanUsePersistentTwoFactorCookies()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await RegisterAsync(client);
        var loginResponse = await client.PostAsJsonAsync("/identity/login?cookieMode=true", new { Username, Password });
        ApplyCookies(client, loginResponse);

        var twoFactorKeyResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/2fa");
        Assert.False(twoFactorKeyResponse.GetProperty("isTwoFactorEnabled").GetBoolean());
        Assert.False(twoFactorKeyResponse.GetProperty("isMachineRemembered").GetBoolean());

        var sharedKey = twoFactorKeyResponse.GetProperty("sharedKey").GetString();

        var keyBytes = Base32.FromBase32(sharedKey);
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestep = Convert.ToInt64(unixTimestamp / 30);
        var twoFactorCode = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)timestep, modifierBytes: null).ToString(CultureInfo.InvariantCulture);

        var enable2faResponse = await client.PostAsJsonAsync("/identity/account/2fa", new { twoFactorCode, Enable = true });
        var enable2faContent = await enable2faResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(enable2faContent.GetProperty("isTwoFactorEnabled").GetBoolean());
        Assert.False(enable2faContent.GetProperty("isMachineRemembered").GetBoolean());

        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "RequiresTwoFactor");

        var twoFactorLoginResponse = await client.PostAsJsonAsync("/identity/login?cookieMode=true&persistCookies=false", new { Username, Password, twoFactorCode });
        ApplyCookies(client, twoFactorLoginResponse);

        var cookie2faResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/2fa");
        Assert.True(cookie2faResponse.GetProperty("isTwoFactorEnabled").GetBoolean());
        Assert.False(cookie2faResponse.GetProperty("isMachineRemembered").GetBoolean());

        var persistentLoginResponse = await client.PostAsJsonAsync("/identity/login?cookieMode=true", new { Username, Password, twoFactorCode });
        ApplyCookies(client, persistentLoginResponse);

        var persistent2faResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/2fa");
        Assert.True(persistent2faResponse.GetProperty("isTwoFactorEnabled").GetBoolean());
        Assert.True(persistent2faResponse.GetProperty("isMachineRemembered").GetBoolean());
    }

    [Fact]
    public async Task CanResetPassword()
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

        var confirmedUsername = "confirmed";
        var confirmedEmail = "confirmed@example.com";

        var unconfirmedUsername = "unconfirmed";
        var unconfirmedEmail = "unconfirmed@example.com";

        await RegisterAsync(client, username: confirmedUsername, email: confirmedEmail);
        await LoginWithEmailConfirmationAsync(client, emailSender, username: confirmedUsername, email: confirmedEmail);

        await RegisterAsync(client, username: unconfirmedUsername, email: unconfirmedEmail);

        // Two emails were sent, but only one was confirmed
        Assert.Equal(2, emailSender.Emails.Count);

        // Returns 200 status for invalid email addresses
        AssertOkAndEmpty(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = confirmedEmail }));
        AssertOkAndEmpty(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = unconfirmedEmail }));
        AssertOkAndEmpty(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = "wrong" }));

        // But only one email was sent for the confirmed address
        Assert.Equal(3, emailSender.Emails.Count);
        var resetEmail = emailSender.Emails[2];

        Assert.Equal("Reset your password", resetEmail.Subject);
        Assert.Equal(confirmedEmail, resetEmail.Address);

        var resetCode = GetPasswordResetCode(resetEmail);
        var newPassword = $"{Password}!";

        // The same validation errors are returned even for invalid emails
        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = confirmedEmail, resetCode }),
            "MissingNewPassword");
        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = unconfirmedEmail, resetCode }),
            "MissingNewPassword");
        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = "wrong", resetCode }),
            "MissingNewPassword");

        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = confirmedEmail, ResetCode = "wrong", newPassword }),
            "InvalidToken");
        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = unconfirmedEmail, ResetCode = "wrong", newPassword }),
            "InvalidToken");
        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = "wrong", ResetCode = "wrong", newPassword }),
            "InvalidToken");

        AssertOkAndEmpty(await client.PostAsJsonAsync("/identity/resetPassword", new { Email = confirmedEmail, resetCode, newPassword }));

        // The old password is no longer valid
        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username = confirmedUsername, Password }),
            "Failed");

        // But the new password is
        AssertOk(await client.PostAsJsonAsync("/identity/login", new { Username = confirmedUsername, Password = newPassword }));
    }

    [Fact]
    public async Task CanGetClaims()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        var username = $"UsernamePrefix-{Username}";
        var email = $"EmailPrefix-{Username}";

        await RegisterAsync(client, username: username, email: email);
        await LoginAsync(client, username: username, email: email);

        var infoResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/info");
        Assert.Equal(username, infoResponse.GetProperty("username").GetString());
        Assert.Equal(email, infoResponse.GetProperty("email").GetString());

        var claims = infoResponse.GetProperty("claims");
        Assert.Equal(username, claims.GetProperty(ClaimTypes.Name).GetString());
        Assert.Equal(email, claims.GetProperty(ClaimTypes.Email).GetString());
        Assert.Equal("pwd", claims.GetProperty("amr").GetString());
        Assert.NotNull(claims.GetProperty(ClaimTypes.NameIdentifier).GetString());
    }

    [Theory]
    [MemberData(nameof(AddIdentityModes))]
    public async Task CanChangeEmail(string addIdentityModes)
    {
        var emailSender = new TestEmailSender();

        await using var app = await CreateAppAsync(services =>
        {
            AddIdentityActions[addIdentityModes](services);
            services.AddSingleton<IEmailSender>(emailSender);
            services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            });
        });
        using var client = app.GetTestClient();

        AssertUnauthorizedAndEmpty(await client.GetAsync("/identity/account/info"));

        await RegisterAsync(client);
        var originalRefreshToken = await LoginWithEmailConfirmationAsync(client, emailSender);

        var infoResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/info");
        Assert.Equal(Username, infoResponse.GetProperty("username").GetString());
        Assert.Equal(Username, infoResponse.GetProperty("email").GetString());
        var infoClaims = infoResponse.GetProperty("claims");
        Assert.Equal("pwd", infoClaims.GetProperty("amr").GetString());
        Assert.Equal(Username, infoClaims.GetProperty(ClaimTypes.Name).GetString());
        Assert.Equal(Username, infoClaims.GetProperty(ClaimTypes.Email).GetString());

        var originalNameIdentifier = infoResponse.GetProperty("claims").GetProperty(ClaimTypes.NameIdentifier).GetString();
        var newUsername = $"NewUsernamePrefix-{Username}";
        var newEmail = $"NewEmailPrefix-{Username}";

        var infoPostResponse = await client.PostAsJsonAsync("/identity/account/info", new { newUsername, newEmail });
        var infoPostContent = await infoPostResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(newUsername, infoPostContent.GetProperty("username").GetString());
        // The email isn't updated until the email is confirmed.
        Assert.Equal(Username, infoPostContent.GetProperty("email").GetString());

        // And none of the claims have yet been updated.
        var infoPostClaims = infoPostContent.GetProperty("claims");
        Assert.Equal(Username, infoPostClaims.GetProperty(ClaimTypes.Name).GetString());
        Assert.Equal(Username, infoPostClaims.GetProperty(ClaimTypes.Email).GetString());
        Assert.Equal(originalNameIdentifier, infoClaims.GetProperty(ClaimTypes.NameIdentifier).GetString());

        // The refresh token is now invalidated by the security stamp.
        AssertUnauthorizedAndEmpty(await client.PostAsJsonAsync("/identity/refresh", new { RefreshToken = originalRefreshToken }));

        // But we can immediately log in with the new username.
        var secondRefreshToken = await LoginAsync(client, username: newUsername);

        // Which gives us a new refresh token that is valid for now.
        AssertOk(await client.PostAsJsonAsync("/identity/refresh", new { RefreshToken = secondRefreshToken }));

        // Two emails have now been sent. The first was sent during registration. And the second for the email change.
        Assert.Equal(2, emailSender.Emails.Count);
        var email = emailSender.Emails[1];

        Assert.Equal("Confirm your email", email.Subject);
        Assert.Equal(newEmail, email.Address);

        AssertOk(await client.GetAsync(GetEmailConfirmationLink(email)));

        var infoAfterEmailChange = await client.GetFromJsonAsync<JsonElement>("/identity/account/info");
        Assert.Equal(newUsername, infoAfterEmailChange.GetProperty("username").GetString());
        // The email is immediately updated after the email is confirmed.
        Assert.Equal(newEmail, infoAfterEmailChange.GetProperty("email").GetString());

        // The username claim is updated from the second login, but the email still won't be available as a claim until we get a new token.
        var claimsAfterEmailChange = infoAfterEmailChange.GetProperty("claims");
        Assert.Equal(newUsername, claimsAfterEmailChange.GetProperty(ClaimTypes.Name).GetString());
        Assert.Equal(Username, claimsAfterEmailChange.GetProperty(ClaimTypes.Email).GetString());
        Assert.Equal(originalNameIdentifier, infoClaims.GetProperty(ClaimTypes.NameIdentifier).GetString());

        // And now the email has changed, the refresh token is once again invalidated by the security stamp.
        AssertUnauthorizedAndEmpty(await client.PostAsJsonAsync("/identity/refresh", new { RefreshToken = secondRefreshToken }));

        // We will finally see all the claims updated after logging in again.
        await LoginAsync(client, username: newUsername);

        var infoAfterFinalLogin = await client.GetFromJsonAsync<JsonElement>("/identity/account/info");
        Assert.Equal(newUsername, infoAfterFinalLogin.GetProperty("username").GetString());
        Assert.Equal(newEmail, infoAfterFinalLogin.GetProperty("email").GetString());

        var claimsAfterFinalLogin = infoAfterFinalLogin.GetProperty("claims");
        Assert.Equal(newUsername, claimsAfterFinalLogin.GetProperty(ClaimTypes.Name).GetString());
        Assert.Equal(newEmail, claimsAfterFinalLogin.GetProperty(ClaimTypes.Email).GetString());
        Assert.Equal(originalNameIdentifier, infoClaims.GetProperty(ClaimTypes.NameIdentifier).GetString());
    }

    [Fact]
    public async Task CanUpdateClaimsDuringInfoPostWithCookies()
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

        AssertUnauthorizedAndEmpty(await client.GetAsync("/identity/account/info"));

        await RegisterAsync(client);
        await LoginWithEmailConfirmationAsync(client, emailSender);

        // Clear bearer token. We just used the common login email for convenient email verification.
        client.DefaultRequestHeaders.Clear();
        var loginResponse = await client.PostAsJsonAsync("/identity/login?cookieMode=true", new { Username, Password });
        ApplyCookies(client, loginResponse);

        var infoResponse = await client.GetFromJsonAsync<JsonElement>("/identity/account/info");
        Assert.Equal(Username, infoResponse.GetProperty("username").GetString());
        Assert.Equal(Username, infoResponse.GetProperty("email").GetString());
        var infoClaims = infoResponse.GetProperty("claims");
        Assert.Equal("pwd", infoClaims.GetProperty("amr").GetString());
        Assert.Equal(Username, infoClaims.GetProperty(ClaimTypes.Name).GetString());
        Assert.Equal(Username, infoClaims.GetProperty(ClaimTypes.Email).GetString());

        var originalNameIdentifier = infoResponse.GetProperty("claims").GetProperty(ClaimTypes.NameIdentifier).GetString();
        var newUsername = $"NewUsernamePrefix-{Username}";
        var newEmail = $"NewEmailPrefix-{Username}";

        var infoPostResponse = await client.PostAsJsonAsync("/identity/account/info", new { newUsername, newEmail });
        ApplyCookies(client, infoPostResponse);

        var infoPostContent = await infoPostResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(newUsername, infoPostContent.GetProperty("username").GetString());
        // The email isn't updated until the email is confirmed.
        Assert.Equal(Username, infoPostContent.GetProperty("email").GetString());

        // The claims have been updated to match.
        var infoPostClaims = infoPostContent.GetProperty("claims");
        Assert.Equal(newUsername, infoPostClaims.GetProperty(ClaimTypes.Name).GetString());
        Assert.Equal(Username, infoPostClaims.GetProperty(ClaimTypes.Email).GetString());
        Assert.Equal(originalNameIdentifier, infoClaims.GetProperty(ClaimTypes.NameIdentifier).GetString());

        // Two emails have now been sent. The first was sent during registration. And the second for the email change.
        Assert.Equal(2, emailSender.Emails.Count);
        var email = emailSender.Emails[1];

        Assert.Equal("Confirm your email", email.Subject);
        Assert.Equal(newEmail, email.Address);

        AssertOk(await client.GetAsync(GetEmailConfirmationLink(email)));

        var infoAfterEmailChange = await client.GetFromJsonAsync<JsonElement>("/identity/account/info");
        Assert.Equal(newUsername, infoAfterEmailChange.GetProperty("username").GetString());
        // The email is immediately updated after the email is confirmed.
        Assert.Equal(newEmail, infoAfterEmailChange.GetProperty("email").GetString());

        // The username claim is updated from the /account/info post, but the email still won't be available as a claim until we get a new cookie.
        var claimsAfterEmailChange = infoAfterEmailChange.GetProperty("claims");
        Assert.Equal(newUsername, claimsAfterEmailChange.GetProperty(ClaimTypes.Name).GetString());
        Assert.Equal(Username, claimsAfterEmailChange.GetProperty(ClaimTypes.Email).GetString());
        Assert.Equal(originalNameIdentifier, infoClaims.GetProperty(ClaimTypes.NameIdentifier).GetString());

        // We will finally see all the claims updated after logging in again.
        var secondLoginResponse = await client.PostAsJsonAsync("/identity/login?cookieMode=true", new { Username = newUsername, Password });
        ApplyCookies(client, secondLoginResponse);

        var infoAfterFinalLogin = await client.GetFromJsonAsync<JsonElement>("/identity/account/info");
        Assert.Equal(newUsername, infoAfterFinalLogin.GetProperty("username").GetString());
        Assert.Equal(newEmail, infoAfterFinalLogin.GetProperty("email").GetString());

        var claimsAfterFinalLogin = infoAfterFinalLogin.GetProperty("claims");
        Assert.Equal(newUsername, claimsAfterFinalLogin.GetProperty(ClaimTypes.Name).GetString());
        Assert.Equal(newEmail, claimsAfterFinalLogin.GetProperty(ClaimTypes.Email).GetString());
        Assert.Equal(originalNameIdentifier, infoClaims.GetProperty(ClaimTypes.NameIdentifier).GetString());
    }

    [Fact]
    public async Task CanChangePasswordWithoutResetEmail()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await RegisterAsync(client);
        await LoginAsync(client);

        var newPassword = $"{Password}!";

        await AssertValidationProblemAsync(await client.PostAsJsonAsync("/identity/account/info", new { newPassword }),
            "OldPasswordRequired");
        AssertOk(await client.PostAsJsonAsync("/identity/account/info", new { OldPassword = Password, newPassword }));

        client.DefaultRequestHeaders.Clear();

        // We can immediately log in with the new password
        await AssertProblemAsync(await client.PostAsJsonAsync("/identity/login", new { Username, Password }),
            "Failed");
        AssertOk(await client.PostAsJsonAsync("/identity/login", new { Username, Password = newPassword }));
    }

    [Fact]
    public async Task CanReportMultipleInfoUpdateErrorsAtOnce()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        await RegisterAsync(client);
        // Register a second user that conflicts with our first NewUsername
        await RegisterAsync(client, username: "taken");

        await LoginAsync(client);

        var newPassword = $"{Password}!";
        var multipleProblemResponse = await client.PostAsJsonAsync("/identity/account/info", new { newPassword, NewUsername = "taken" });

        Assert.Equal(HttpStatusCode.BadRequest, multipleProblemResponse.StatusCode);
        var problemDetails = await multipleProblemResponse.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problemDetails);

        Assert.Equal(2, problemDetails.Errors.Count);
        Assert.Contains("OldPasswordRequired", problemDetails.Errors.Keys);
        Assert.Contains("DuplicateUserName", problemDetails.Errors.Keys);

        // We can in fact update multiple things at once if we do it correctly though.
        AssertOk(await client.PostAsJsonAsync("/identity/account/info", new { OldPassword = Password, newPassword, NewUsername = "not-taken" }));
        AssertOk(await client.PostAsJsonAsync("/identity/login", new { Username = "not-taken", Password = newPassword }));
    }

    private async Task<WebApplication> CreateAppAsync<TUser, TContext>(Action<IServiceCollection>? configureServices, bool autoStart = true)
        where TUser : class, new()
        where TContext : DbContext
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(LoggerFactory);
        builder.Services.AddAuthorization();

        var dbConnection = new SqliteConnection("DataSource=:memory:");
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
            .AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme);

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

    private static string GetEmailConfirmationLink(Email email)
    {
        // Update if we add more links to the email.
        var confirmationMatch = Regex.Match(email.HtmlMessage, "href='(.*?)'");
        Assert.True(confirmationMatch.Success);
        Assert.Equal(2, confirmationMatch.Groups.Count);

        return WebUtility.HtmlDecode(confirmationMatch.Groups[1].Value);
    }

    private static string GetPasswordResetCode(Email email)
    {
        // Update if we add more links to the email.
        var confirmationMatch = Regex.Match(email.HtmlMessage, "code: (.*?)$");
        Assert.True(confirmationMatch.Success);
        Assert.Equal(2, confirmationMatch.Groups.Count);

        return WebUtility.HtmlDecode(confirmationMatch.Groups[1].Value);
    }

    private async Task RegisterAsync(HttpClient client, string? groupPrefix = null, string? username = null, string? email = null)
    {
        groupPrefix ??= "/identity";
        username ??= Username;
        email ??= Username;

        AssertOkAndEmpty(await client.PostAsJsonAsync($"{groupPrefix}/register", new { username, Password, email }));
    }

    private async Task<string> LoginAsync(HttpClient client, string? groupPrefix = null, string? username = null, string? email = null)
    {
        groupPrefix ??= "/identity";
        username ??= Username;
        email ??= Username;

        await client.PostAsJsonAsync($"{groupPrefix}/login", new { username, Password, email });
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new { username, Password });
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginContent.GetProperty("access_token").GetString();
        var refreshToken = loginContent.GetProperty("refresh_token").GetString();
        Assert.NotNull(accessToken);
        Assert.NotNull(refreshToken);
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        return refreshToken;
    }

    private async Task<string> LoginWithEmailConfirmationAsync(HttpClient client, TestEmailSender emailSender, string? groupPrefix = null, string? username = null, string? email = null)
    {
        groupPrefix ??= "/identity";
        username ??= Username;
        email ??= Username;

        var receivedEmail = emailSender.Emails.Last();

        Assert.Equal("Confirm your email", receivedEmail.Subject);
        Assert.Equal(email, receivedEmail.Address);

        await AssertProblemAsync(await client.PostAsJsonAsync($"{groupPrefix}/login", new { username, Password }),
            "NotAllowed");

        AssertOk(await client.GetAsync(GetEmailConfirmationLink(receivedEmail)));

        return await LoginAsync(client, groupPrefix, username, email);
    }

    private static void AssertOk(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

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

    private static async Task AssertProblemAsync(HttpResponseMessage response, string detail, HttpStatusCode status = HttpStatusCode.Unauthorized)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ReasonPhrases.GetReasonPhrase((int)status), problem.Title);
        Assert.Equal(detail, problem.Detail);
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string error)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        var errorEntry = Assert.Single(problem.Errors);
        Assert.Equal(error, errorEntry.Key);
    }

    private static void ApplyCookies(HttpClient client, HttpResponseMessage response)
    {
        AssertOk(response);

        Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookieHeaders));
        foreach (var setCookieHeader in setCookieHeaders)
        {
            if (setCookieHeader.Split(';', 2) is not [var cookie, _])
            {
                throw new XunitException("Invalid Set-Cookie header!");
            }

            // Cookies starting with "CookieName=;" are being deleted
            if (!cookie.EndsWith("=", StringComparison.Ordinal))
            {
                client.DefaultRequestHeaders.Add(HeaderNames.Cookie, cookie);
            }
        }
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
