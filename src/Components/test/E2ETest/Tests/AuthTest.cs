// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class AuthTest : BasicTestAppTestBase
    {
        // These strings correspond to the links in BasicTestApp\AuthTest\Links.razor
        const string CascadingAuthenticationStateLink = "Cascading authentication state";
        const string AuthorizeViewCases = "AuthorizeView cases";

        public AuthTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            // Normally, the E2E tests use the Blazor dev server if they are testing
            // client-side execution. But for the auth tests, we always have to run
            // in "hosted on ASP.NET Core" mode, because we get the auth state from it.
            serverFixture.UseAspNetHost(TestServer.Program.BuildWebHost);
        }

        [Fact]
        public void CascadingAuthenticationState_Unauthenticated()
        {
            SignInAs(null, null);

            var appElement = MountAndNavigateToAuthTest(CascadingAuthenticationStateLink);

            Browser.Equal("False", () => appElement.FindElement(By.Id("identity-authenticated")).Text);
            Browser.Equal(string.Empty, () => appElement.FindElement(By.Id("identity-name")).Text);
            Browser.Equal("(none)", () => appElement.FindElement(By.Id("test-claim")).Text);
        }

        [Fact]
        public void CascadingAuthenticationState_Authenticated()
        {
            SignInAs("someone cool", null);

            var appElement = MountAndNavigateToAuthTest(CascadingAuthenticationStateLink);

            Browser.Equal("True", () => appElement.FindElement(By.Id("identity-authenticated")).Text);
            Browser.Equal("someone cool", () => appElement.FindElement(By.Id("identity-name")).Text);
            Browser.Equal("Test claim value", () => appElement.FindElement(By.Id("test-claim")).Text);
        }

        [Fact]
        public void AuthorizeViewCases_NoAuthorizationRule_Unauthenticated()
        {
            SignInAs(null, null);
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            WaitUntilExists(By.CssSelector("#no-authorization-rule .not-authorized"));
            Browser.Equal("You're not authorized, anonymous", () =>
                appElement.FindElement(By.CssSelector("#no-authorization-rule .not-authorized")).Text);
        }

        [Fact]
        public void AuthorizeViewCases_NoAuthorizationRule_Authenticated()
        {
            SignInAs("Some User", null);
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("Welcome, Some User!", () =>
                appElement.FindElement(By.CssSelector("#no-authorization-rule .authorized")).Text);
        }

        [Fact]
        public void AuthorizeViewCases_RequireRole_Authenticated()
        {
            SignInAs("Some User", "IrrelevantRole,TestRole");
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("Welcome, Some User!", () =>
                appElement.FindElement(By.CssSelector("#authorize-role .authorized")).Text);
        }

        [Fact]
        public void AuthorizeViewCases_RequireRole_Unauthenticated()
        {
            SignInAs("Some User", "IrrelevantRole");
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("You're not authorized, Some User", () =>
                appElement.FindElement(By.CssSelector("#authorize-role .not-authorized")).Text);
        }

        [Fact]
        public void AuthorizeViewCases_RequirePolicy_Authenticated()
        {
            SignInAs("Bert", null);
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("Welcome, Bert!", () =>
                appElement.FindElement(By.CssSelector("#authorize-policy .authorized")).Text);
        }

        [Fact]
        public void AuthorizeViewCases_RequirePolicy_Unauthenticated()
        {
            SignInAs("Mallory", null);
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("You're not authorized, Mallory", () =>
                appElement.FindElement(By.CssSelector("#authorize-policy .not-authorized")).Text);
        }

        IWebElement MountAndNavigateToAuthTest(string authLinkText)
        {
            Navigate(ServerPathBase);
            var appElement = MountTestComponent<BasicTestApp.AuthTest.AuthRouter>();
            WaitUntilExists(By.Id("auth-links"));
            appElement.FindElement(By.LinkText(authLinkText)).Click();
            return appElement;
        }

        void SignInAs(string usernameOrNull, string rolesOrNull)
        {
            const string authenticationPageUrl = "/Authentication";
            var baseRelativeUri = usernameOrNull == null
                ? $"{authenticationPageUrl}?signout=true"
                : $"{authenticationPageUrl}?username={usernameOrNull}&roles={rolesOrNull}";
            Navigate(baseRelativeUri);
            WaitUntilExists(By.CssSelector("h1#authentication"));
        }
    }
}
