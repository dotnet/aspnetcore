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
            SignInAs(null);

            var appElement = MountAndNavigateToAuthTest(CascadingAuthenticationStateLink);

            Browser.Equal("False", () => appElement.FindElement(By.Id("identity-authenticated")).Text);
            Browser.Equal(string.Empty, () => appElement.FindElement(By.Id("identity-name")).Text);
            Browser.Equal("(none)", () => appElement.FindElement(By.Id("test-claim")).Text);
        }

        [Fact]
        public void CascadingAuthenticationState_Authenticated()
        {
            SignInAs("someone cool");

            var appElement = MountAndNavigateToAuthTest(CascadingAuthenticationStateLink);

            Browser.Equal("True", () => appElement.FindElement(By.Id("identity-authenticated")).Text);
            Browser.Equal("someone cool", () => appElement.FindElement(By.Id("identity-name")).Text);
            Browser.Equal("Test claim value", () => appElement.FindElement(By.Id("test-claim")).Text);
        }

        [Fact]
        public void AuthorizeViewCases_NoAuthorizationRule_Unauthenticated()
        {
            SignInAs(null);
            MountAndNavigateToAuthTest(AuthorizeViewCases);
            WaitUntilExists(By.CssSelector("#no-authorization-rule .not-authorized"));
        }

        [Fact]
        public void AuthorizeViewCases_NoAuthorizationRule_Authenticated()
        {
            SignInAs("Some User");
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("Welcome, Some User!", () =>
                appElement.FindElement(By.CssSelector("#no-authorization-rule .authorized")).Text);
        }

        IWebElement MountAndNavigateToAuthTest(string authLinkText)
        {
            Navigate(ServerPathBase);
            var appElement = MountTestComponent<BasicTestApp.AuthTest.AuthRouter>();
            WaitUntilExists(By.Id("auth-links"));
            appElement.FindElement(By.LinkText(authLinkText)).Click();
            return appElement;
        }

        void SignInAs(string usernameOrNull)
        {
            const string authenticationPageUrl = "/Authentication";
            var baseRelativeUri = usernameOrNull == null
                ? $"{authenticationPageUrl}?signout=true"
                : $"{authenticationPageUrl}?username={usernameOrNull}";
            Navigate(baseRelativeUri);
            WaitUntilExists(By.CssSelector("h1#authentication"));
        }
    }
}
