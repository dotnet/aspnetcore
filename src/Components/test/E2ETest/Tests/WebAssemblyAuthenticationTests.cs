// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
using OpenQA.Selenium.Support.UI;
using Wasm.Authentication.Server;
using Wasm.Authentication.Server.Data;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
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
            EnsureDatabaseCreated(_serverFixture.Host.Services);
            WaitUntilLoaded();
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void WasmAuthentication_Loads()
        {
            Assert.Equal("Wasm.Authentication.Client", Browser.Title);
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void AnonymousUser_GetsRedirectedToLogin_AndBackToOriginalProtectedResource()
        {
            var link = By.PartialLinkText("Fetch data");
            var page = "/Identity/Account/Login";

            ClickAndNavigate(link, page);

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            FirstTimeRegister(userName, password);

            ValidateFetchData();
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void CanPreserveApplicationState_DuringLogIn()
        {
            var originalAppState = Browser.Exists(By.Id("app-state")).Text;

            var link = By.PartialLinkText("Fetch data");
            var page = "/Identity/Account/Login";

            ClickAndNavigate(link, page);

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            FirstTimeRegister(userName, password);

            ValidateFetchData();

            var homeLink = By.PartialLinkText("Home");
            var homePage = "/";
            ClickAndNavigate(homeLink, homePage);

            var restoredAppState = Browser.Exists(By.Id("app-state")).Text;
            Assert.Equal(originalAppState, restoredAppState);
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void CanShareUserRolesBetweenClientAndServer()
        {
            ClickAndNavigate(By.PartialLinkText("Log in"), "/Identity/Account/Login");

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
            FirstTimeRegister(userName, password);

            ClickAndNavigate(By.PartialLinkText("Make admin"), "/new-admin");

            ClickAndNavigate(By.PartialLinkText("Settings"), "/admin-settings");

            Browser.Exists(By.Id("admin-action")).Click();

            Browser.Exists(By.Id("admin-success"));
        }


        private void ClickAndNavigate(By link, string page)
        {
            Browser.FindElement(link).Click();
            Browser.Contains(page, () => Browser.Url);
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void AnonymousUser_CanRegister_AndGetLoggedIn()
        {
            ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
            RegisterCore(userName, password);
            CompleteProfileDetails();

            // Need to navigate to fetch page
            Browser.FindElement(By.PartialLinkText("Fetch data")).Click();

            // Can navigate to the 'fetch data' page
            ValidateFetchData();
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void AuthenticatedUser_ProfileIncludesDetails_And_AccessToken()
        {
            ClickAndNavigate(By.PartialLinkText("User"), "/Identity/Account/Login");

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
            FirstTimeRegister(userName, password);

            Browser.Contains("user", () => Browser.Url);
            Browser.Equal($"Welcome {userName}", () => Browser.FindElement(By.TagName("h1")).Text);

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
            payload.Scopes);

            var currentTime = DateTimeOffset.Parse(Browser.Exists(By.Id("current-time")).Text);
            var tokenExpiration = DateTimeOffset.Parse(Browser.Exists(By.Id("access-token-expires")).Text);
            Assert.True(currentTime.AddMinutes(50) < tokenExpiration);
            Assert.True(currentTime.AddMinutes(60) >= tokenExpiration);
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void AuthenticatedUser_CanGoToProfile()
        {
            ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
            RegisterCore(userName, password);
            CompleteProfileDetails();

            ClickAndNavigate(By.PartialLinkText($"Hello, {userName}!"), "/Identity/Account/Manage");

            Browser.Navigate().Back();
            Browser.Equal("/", () => new Uri(Browser.Url).PathAndQuery);
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void RegisterAndBack_DoesNotCause_RedirectLoop()
        {
            Browser.FindElement(By.PartialLinkText("Register")).Click();

            // We will be redirected to the identity UI
            Browser.Contains("/Identity/Account/Register", () => Browser.Url);

            Browser.Navigate().Back();

            Browser.Equal("/", () => new Uri(Browser.Url).PathAndQuery);
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void LoginAndBack_DoesNotCause_RedirectLoop()
        {
            Browser.FindElement(By.PartialLinkText("Log in")).Click();

            // We will be redirected to the identity UI
            Browser.Contains("/Identity/Account/Login", () => Browser.Url);

            Browser.Navigate().Back();

            Browser.Equal("/", () => new Uri(Browser.Url).PathAndQuery);
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void NewlyRegisteredUser_CanLogOut()
        {
            ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
            RegisterCore(userName, password);
            CompleteProfileDetails();

            ValidateLogout();
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void AlreadyRegisteredUser_CanLogOut()
        {
            ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
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

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void LoggedInUser_OnTheIdP_CanLogInSilently()
        {
            ClickAndNavigate(By.PartialLinkText("Register"), "/Identity/Account/Register");

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
            RegisterCore(userName, password);
            CompleteProfileDetails();
            ValidateLoggedIn(userName);

            // Clear the existing storage on the page and refresh
            Browser.Exists(By.Id("test-clear-storage")).Click();
            Browser.Exists(By.Id("test-refresh-page")).Click();

            ValidateLoggedIn(userName);
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
        public void CanNotRedirect_To_External_ReturnUrl()
        {
            Browser.Navigate().GoToUrl(new Uri(new Uri(Browser.Url), "/authentication/login?returnUrl=https%3A%2F%2Fwww.bing.com").AbsoluteUri);
            WaitUntilLoaded(skipHeader: true);
            Browser.Exists(By.CssSelector("[style=\"display: block;\"]"));
            Assert.NotEmpty(Browser.GetBrowserLogs(LogLevel.Severe));
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38098")]
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
            Browser.FindElement(By.PartialLinkText("Login")).Click();
            Browser.Exists(By.Name("Input.Email"));
            Browser.FindElement(By.Name("Input.Email")).SendKeys(userName);
            Browser.FindElement(By.Name("Input.Password")).SendKeys(password);
            Browser.FindElement(By.Id("login-submit")).Click();
        }

        private void ValidateLogout()
        {
            Browser.Exists(By.CssSelector("button.nav-link.btn.btn-link"));

            // Click logout button
            Browser.FindElement(By.CssSelector("button.nav-link.btn.btn-link")).Click();

            Browser.Contains("/authentication/logged-out", () => Browser.Url);
            Browser.True(() => Browser.FindElements(By.TagName("p")).Any(e => e.Text == "You are logged out."));
        }

        private void ValidateFetchData()
        {
            // Can navigate to the 'fetch data' page
            Browser.Contains("fetchdata", () => Browser.Url);
            Browser.Equal("Weather forecast", () => Browser.FindElement(By.TagName("h1")).Text);

            // Asynchronously loads and displays the table of weather forecasts
            Browser.Exists(By.CssSelector("table>tbody>tr"));
            Browser.Equal(5, () => Browser.FindElements(By.CssSelector("p+table>tbody>tr")).Count);
        }

        private void FirstTimeRegister(string userName, string password)
        {
            Browser.FindElement(By.PartialLinkText("Register as a new user")).Click();
            RegisterCore(userName, password);
            CompleteProfileDetails();
        }

        private void CompleteProfileDetails()
        {
            Browser.Exists(By.PartialLinkText("Home"));
            Browser.Contains("/preferences", () => Browser.Url);
            Browser.FindElement(By.Id("color-preference")).SendKeys("Red");
            Browser.FindElement(By.Id("submit-preference")).Click();
        }

        private void RegisterCore(string userName, string password)
        {
            Browser.Exists(By.Name("Input.Email"));
            Browser.FindElement(By.Name("Input.Email")).SendKeys(userName);
            Browser.FindElement(By.Name("Input.Password")).SendKeys(password);
            Browser.FindElement(By.Name("Input.ConfirmPassword")).SendKeys(password);
            Browser.FindElement(By.Id("registerSubmit")).Click();

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
            Browser.FindElement(By.PartialLinkText("Login")).Click();
            Browser.Exists(By.Name("Input.Email"));
            Browser.FindElement(By.Name("Input.Email")).SendKeys(userName);
            Browser.FindElement(By.Name("Input.Password")).SendKeys(password);
            Browser.FindElement(By.Id("login-submit")).Click();
        }

        private void WaitUntilLoaded(bool skipHeader = false)
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("app")).Text != "Loading...");

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
}
