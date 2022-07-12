// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using Wasm.Authentication.Server;
using Wasm.Authentication.Server.Data;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyAuthenticationTests : ServerTestBase<AspNetSiteServerFixture>
{
    private static readonly SqliteConnection _connection;

    // We create a conection here and open it as the in memory Db will delete the database
    // as soon as there are no open connections to it.
    static WebAssemblyAuthenticationTests()
    {
        _connection = new SqliteConnection($"DataSource=:memory:");
        _connection.Open();
    }

    public WebAssemblyAuthenticationTests(
        BrowserFixture browserFixture,
        AspNetSiteServerFixture serverFixture,
        ITestOutputHelper output) :
        base(browserFixture, serverFixture, output)
    {
        _serverFixture.ApplicationAssembly = typeof(Program).Assembly;

        _serverFixture.AdditionalArguments.Clear();

        _serverFixture.BuildWebHostMethod = args => Program.CreateHostBuilder(args)
            .ConfigureServices(services => SetupTestDatabase<ApplicationDbContext>(services, _connection))
            .Build();
    }

    public override Task InitializeAsync() => base.InitializeAsync(Guid.NewGuid().ToString());

    protected override void InitializeAsyncCore()
    {
        Navigate("/", noReload: true);
        Browser.Manage().Window.Size = new Size(1024, 800);
        EnsureDatabaseCreated(_serverFixture.Host.Services);
        WaitUntilLoaded();
    }

    [Fact]
    public void WasmAuthentication_Loads()
    {
        Browser.Equal("Wasm.Authentication.Client", () => Browser.Title);
    }

    [Fact]
    public void AnonymousUser_GetsRedirectedToLogin_AndBackToOriginalProtectedResource()
    {
        var link = By.PartialLinkText("Fetch data");
        var page = "/Identity/Account/Login";

        ClickAndNavigate(link, page);

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";

        FirstTimeRegister(userName, password);

        ValidateFetchData();
    }

    [Fact]
    public void CanPreserveApplicationState_DuringLogIn()
    {
        var originalAppState = Browser.Exists(By.Id("app-state")).Text;

        var link = By.PartialLinkText("Fetch data");
        var page = "/Identity/Account/Login";

        ClickAndNavigate(link, page);

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";

        FirstTimeRegister(userName, password);

        ValidateFetchData();

        var homeLink = By.PartialLinkText("Home");
        var homePage = "/";
        ClickAndNavigate(homeLink, homePage);

        var restoredAppState = Browser.Exists(By.Id("app-state")).Text;
        Assert.Equal(originalAppState, restoredAppState);
    }

    [Fact]
    public void CanShareUserRolesBetweenClientAndServer()
    {
        ClickAndNavigate(By.PartialLinkText("Log in"), "/Identity/Account/Login");

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";
        FirstTimeRegister(userName, password);

        ClickAndNavigate(By.PartialLinkText("Make admin"), "/new-admin");

        ClickAndNavigate(By.PartialLinkText("Settings"), "/admin-settings");

        Browser.Exists(By.Id("admin-action")).Click();

        Browser.Exists(By.Id("admin-success"));
    }

    private void ClickAndNavigate(By link, string page)
    {
        Browser.Exists(link).Click();
        Browser.Contains(page, () => Browser.Url);
    }

    [Fact]
    public void AnonymousUser_CanRegister_AndGetLoggedIn()
    {
        ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";
        RegisterCore(userName, password);
        CompleteProfileDetails();

        // Need to navigate to fetch page
        Browser.Exists(By.PartialLinkText("Fetch data")).Click();

        // Can navigate to the 'fetch data' page
        ValidateFetchData();
    }

    [Fact]
    public void AuthenticatedUser_ProfileIncludesDetails_And_AccessToken()
    {
        ClickAndNavigate(By.PartialLinkText("User"), "/Identity/Account/Login");

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";
        FirstTimeRegister(userName, password);

        Browser.Contains("user", () => Browser.Url);
        Browser.Equal($"Welcome {userName}", () => Browser.Exists(By.TagName("h1")).Text);

        var claims = Browser.FindElements(By.CssSelector("p.claim"))
            .Select(e =>
            {
                var pair = e.Text.Split(":");
                return (pair[0].Trim(), pair[1].Trim());
            })
            .Where(c => !new[] { "s_hash", "auth_time", "sid", "sub" }.Contains(c.Item1))
            .OrderBy(o => o.Item1)
            .ToArray();

        Assert.Equal(5, claims.Length);

        Assert.Equal(new[]
        {
                ("amr", "pwd"),
                ("idp", "local"),
                ("name", userName),
                ("NewUser", "true"),
                ("preferred_username", userName)
            },
        claims);

        var token = Browser.Exists(By.Id("access-token")).Text;
        Assert.NotNull(token);
        var payload = JsonSerializer.Deserialize<JwtPayload>(Base64UrlTextEncoder.Decode(token.Split(".")[1]));

        Assert.StartsWith("http://127.0.0.1", payload.Issuer);
        Assert.StartsWith("Wasm.Authentication.ServerAPI", payload.Audience);
        Assert.StartsWith("Wasm.Authentication.Client", payload.ClientId);
        Assert.Equal(new[]
        {
                "openid",
                "profile",
                "Wasm.Authentication.ServerAPI"
            },
        payload.Scopes.OrderBy(id => id));

        // The browser formats the text using the current language, so the following parsing relies on
        // the server being set to an equivalent culture. This should be true in our test scenarios.
        var currentTime = DateTimeOffset.Parse(Browser.Exists(By.Id("current-time")).Text, CultureInfo.CurrentCulture);
        var tokenExpiration = DateTimeOffset.Parse(Browser.Exists(By.Id("access-token-expires")).Text, CultureInfo.CurrentCulture);
        Assert.True(currentTime.AddMinutes(50) < tokenExpiration);
        Assert.True(currentTime.AddMinutes(60) >= tokenExpiration);
    }

    [Fact]
    public void AuthenticatedUser_CanGoToProfile()
    {
        ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";
        RegisterCore(userName, password);
        CompleteProfileDetails();

        ClickAndNavigate(By.PartialLinkText($"Hello, {userName}!"), "/Identity/Account/Manage");

        Browser.Navigate().Back();
        Browser.Equal("/", () => new Uri(Browser.Url).PathAndQuery);
    }

    [Fact]
    public void CanPassAdditionalParameters_DuringSignIn()
    {
        // Register first user
        ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

        var userName1 = $"{Guid.NewGuid()}@example.com";
        var password1 = $"[PLACEHOLDER]-1a";
        RegisterCore(userName1, password1);
        CompleteProfileDetails();

        ValidateLogout();

        Browser.Navigate().GoToUrl("data:");
        Navigate("/");
        WaitUntilLoaded();

        // Register second user
        ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

        var userName2 = $"{Guid.NewGuid()}@example.com";
        var password2 = $"[PLACEHOLDER]-1a";
        RegisterCore(userName2, password2);
        CompleteProfileDetails();

        ValidateLogout();

        Browser.Navigate().GoToUrl("data:");
        Navigate("/");
        WaitUntilLoaded();

        // Log in with the first user
        ClickAndNavigate(By.PartialLinkText("Log in"), "/Identity/Account/Login");
        LoginCore(userName1, password1);
        ValidateLoggedIn(userName1);

        // Log in with the second user
        ClickAndNavigate(By.PartialLinkText("Log in with another user"), "/Identity/Account/Login");
        LoginCore(userName2, password2);
        ValidateLoggedIn(userName2);

        ValidateLogout();
    }

    [Fact]
    public void CanRequestAnAdditionalAccessToken_Interactively()
    {
        ClickAndNavigate(By.PartialLinkText("Token"), "/Identity/Account/Login");

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";
        FirstTimeRegister(userName, password, completeProfileDetails: false);

        Browser.Contains("token", () => Browser.Url);

        var claims = Browser.FindElements(By.CssSelector("p.claim"))
            .Select(e =>
            {
                var pair = e.Text.Split(":");
                return (pair[0].Trim(), pair[1].Trim());
            })
            .Where(c => !new[] { "s_hash", "auth_time", "sid", "sub" }.Contains(c.Item1))
            .OrderBy(o => o.Item1)
            .ToArray();

        var token = Browser.Exists(By.Id("access-token")).Text;
        Assert.NotNull(token);
        var payload = JsonSerializer.Deserialize<JwtPayload>(Base64UrlTextEncoder.Decode(token.Split(".")[1]));

        Assert.StartsWith("http://127.0.0.1", payload.Issuer);
        Assert.StartsWith("SecondAPI", payload.Audience);
        Assert.StartsWith("Wasm.Authentication.Client", payload.ClientId);
        Assert.Equal(new[] { "SecondAPI" }, payload.Scopes.OrderBy(id => id));

        // The browser formats the text using the current language, so the following parsing relies on
        // the server being set to an equivalent culture. This should be true in our test scenarios.
        var currentTime = DateTimeOffset.Parse(Browser.Exists(By.Id("current-time")).Text, CultureInfo.CurrentCulture);
        var tokenExpiration = DateTimeOffset.Parse(Browser.Exists(By.Id("access-token-expires")).Text, CultureInfo.CurrentCulture);
        Assert.True(currentTime.AddMinutes(50) < tokenExpiration);
        Assert.True(currentTime.AddMinutes(60) >= tokenExpiration);
    }

    [Fact]
    public void RegisterAndBack_DoesNotCause_RedirectLoop()
    {
        Browser.Exists(By.PartialLinkText("Register")).Click();

        // We will be redirected to the identity UI
        Browser.Contains("/Identity/Account/Register", () => Browser.Url);

        Browser.Navigate().Back();

        Browser.Equal("/", () => new Uri(Browser.Url).PathAndQuery);
    }

    [Fact]
    public void LoginAndBack_DoesNotCause_RedirectLoop()
    {
        Browser.Exists(By.PartialLinkText("Log in")).Click();

        // We will be redirected to the identity UI
        Browser.Contains("/Identity/Account/Login", () => Browser.Url);

        Browser.Navigate().Back();

        Browser.Equal("/", () => new Uri(Browser.Url).PathAndQuery);
    }

    [Fact]
    public void NewlyRegisteredUser_CanLogOut()
    {
        ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";
        RegisterCore(userName, password);
        CompleteProfileDetails();

        ValidateLogout();
    }

    [Fact]
    public void AlreadyRegisteredUser_CanLogOut()
    {
        ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";
        RegisterCore(userName, password);
        CompleteProfileDetails();

        ValidateLogout();

        Browser.Navigate().GoToUrl("data:");
        Navigate("/");
        WaitUntilLoaded();

        ClickAndNavigate(By.PartialLinkText("Log in"), "/Identity/Account/Login");

        // Now we can login
        LoginCore(userName, password);

        ValidateLoggedIn(userName);

        ValidateLogout();
    }

    [Fact]
    public void LoggedInUser_OnTheIdP_CanLogInSilently()
    {
        ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = $"[PLACEHOLDER]-1a";
        RegisterCore(userName, password);
        CompleteProfileDetails();
        ValidateLoggedIn(userName);

        // Clear the existing storage on the page and refresh
        Browser.Exists(By.Id("test-clear-storage")).Click();
        Browser.Exists(By.Id("test-refresh-page")).Click();

        ValidateLoggedIn(userName);
    }

    [Fact]
    public async Task CanNotTrigger_Logout_WithNavigation()
    {
        Browser.Navigate().GoToUrl(new Uri(new Uri(Browser.Url), "/authentication/logout").AbsoluteUri);
        WaitUntilLoaded(skipHeader: true);
        Browser.Contains("/authentication/logout-failed", () => Browser.Url);
        await Task.Delay(3000);
        Browser.Contains("/authentication/logout-failed", () => Browser.Url);
    }

    private void ValidateLoggedIn(string userName)
    {
        Browser.Exists(By.CssSelector("button.nav-link.btn.btn-link"));
        Browser.Exists(By.PartialLinkText($"Hello, {userName}!"));
    }

    private void LoginCore(string userName, string password)
    {
        Browser.Exists(By.Id("login-submit")).Click();
        Browser.Exists(By.Name("Input.Email"));
        Browser.Exists(By.Name("Input.Email")).SendKeys(userName);
        Browser.Exists(By.Name("Input.Password")).SendKeys(password);
        Browser.Exists(By.Id("login-submit")).Click();
    }

    private void ValidateLogout()
    {
        Browser.Exists(By.CssSelector("button.nav-link.btn.btn-link"));

        // Click logout button
        Browser.Exists(By.CssSelector("button.nav-link.btn.btn-link")).Click();

        Browser.Contains("/authentication/logged-out", () => Browser.Url);
        Browser.True(() => Browser.FindElements(By.TagName("p")).Any(e => e.Text == "You are logged out."));
    }

    private void ValidateFetchData()
    {
        // Can navigate to the 'fetch data' page
        Browser.Contains("fetchdata", () => Browser.Url);
        Browser.Equal("Weather forecast", () => Browser.Exists(By.TagName("h1")).Text);

        // Asynchronously loads and displays the table of weather forecasts
        Browser.Exists(By.CssSelector("table>tbody>tr"));
        Browser.Equal(5, () => Browser.FindElements(By.CssSelector("p+table>tbody>tr")).Count);
    }

    private void FirstTimeRegister(string userName, string password, bool completeProfileDetails = true)
    {
        Browser.Exists(By.PartialLinkText("Register as a new user")).Click();
        RegisterCore(userName, password);
        if (completeProfileDetails)
        {
            CompleteProfileDetails();
        }
    }

    private void CompleteProfileDetails()
    {
        Browser.Exists(By.PartialLinkText("Home"));
        Browser.Contains("/preferences", () => Browser.Url);
        Browser.Exists(By.Id("color-preference")).SendKeys("Red");
        Browser.Exists(By.Id("submit-preference")).Click();
    }

    private void RegisterCore(string userName, string password)
    {
        Browser.Exists(By.Name("Input.Email"));
        Browser.Exists(By.Name("Input.Email")).SendKeys(userName);
        Browser.Exists(By.Name("Input.Password")).SendKeys(password);
        Browser.Exists(By.Name("Input.ConfirmPassword")).SendKeys(password);
        Browser.Click(By.Id("registerSubmit"));

        // We will be redirected to the RegisterConfirmation
        Browser.Contains("/Identity/Account/RegisterConfirmation", () => Browser.Url);
        try
        {
            // For some reason the test sometimes get stuck here. Given that this is not something we are testing, to avoid
            // this we'll retry once to minify the chances it happens on CI runs.
            ClickAndNavigate(By.PartialLinkText("Click here to confirm your account"), "/Identity/Account/ConfirmEmail");
        }
        catch
        {
            ClickAndNavigate(By.PartialLinkText("Click here to confirm your account"), "/Identity/Account/ConfirmEmail");
        }

        // Now we can login
        Browser.Exists(By.PartialLinkText("Login")).Click();
        Browser.Exists(By.Name("Input.Email"));
        Browser.Exists(By.Name("Input.Email")).SendKeys(userName);
        Browser.Exists(By.Name("Input.Password")).SendKeys(password);
        Browser.Exists(By.Id("login-submit")).Click();
    }

    private void WaitUntilLoaded(bool skipHeader = false)
    {
        Browser.Exists(By.TagName("app"));
        Browser.True(() => Browser.Exists(By.TagName("app")).Text != "Loading...");

        if (!skipHeader)
        {
            // All pages in the text contain an h1 element. This helps us wait until the router has intercepted links as that
            // happens before rendering the underlying page.
            Browser.Exists(By.TagName("h1"));
        }
    }

    public static IServiceCollection SetupTestDatabase<TContext>(IServiceCollection services, DbConnection connection) where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        services.AddScoped(p =>
        DbContextOptionsFactory<TContext>(
            p,
            (sp, options) => options
                .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning))
                    .UseSqlite(connection)));

        return services;
    }

    private static DbContextOptions<TContext> DbContextOptionsFactory<TContext>(
        IServiceProvider applicationServiceProvider,
        Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>(
            new DbContextOptions<TContext>(new Dictionary<Type, IDbContextOptionsExtension>()));

        builder.UseApplicationServiceProvider(applicationServiceProvider);

        optionsAction?.Invoke(applicationServiceProvider, builder);

        return builder.Options;
    }

    private void EnsureDatabaseCreated(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var applicationDbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (applicationDbContext?.Database?.GetPendingMigrations()?.Any() == true)
        {
            applicationDbContext?.Database?.Migrate();
        }
    }

    private class JwtPayload
    {
        [JsonPropertyName("iss")]
        public string Issuer { get; set; }

        [JsonPropertyName("aud")]
        public string Audience { get; set; }

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }

        [JsonPropertyName("sub")]
        public string Subject { get; set; }

        [JsonPropertyName("idp")]
        public string IdentityProvider { get; set; }

        [JsonPropertyName("scope")]
        public string[] Scopes { get; set; }
    }
}
