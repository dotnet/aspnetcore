// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    [Collection("auth")] // Because auth uses cookies, this can't run in parallel with other auth tests
    public class AuthTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        // These strings correspond to the links in BasicTestApp\AuthTest\Links.razor
        protected const string CascadingAuthenticationStateLink = "Cascading authentication state";
        protected const string AuthorizeViewCases = "AuthorizeView cases";
        protected const string PageAllowingAnonymous = "Page allowing anonymous";
        protected const string PageRequiringAuthorization = "Page requiring any authentication";
        protected const string PageRequiringPolicy = "Page requiring policy";
        protected const string PageRequiringRole = "Page requiring role";

        public AuthTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : this(browserFixture, serverFixture, output, ExecutionMode.Client)
        {
        }

        protected AuthTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output,
            ExecutionMode executionMode)
            : base(browserFixture, serverFixture, output)
        {
            // Normally, the E2E tests use the Blazor dev server if they are testing
            // client-side execution. But for the auth tests, we always have to run
            // in "hosted on ASP.NET Core" mode, because we get the auth state from it.
            switch (executionMode)
            {
                case ExecutionMode.Client:
                    serverFixture.UseAspNetHost(TestServer.Program.BuildWebHost<TestServer.AuthenticationStartup>);
                    break;
                case ExecutionMode.Server:
                    serverFixture.UseAspNetHost(TestServer.Program.BuildWebHost<TestServer.ServerAuthenticationStartup>);
                    break;
                default:
                    break;
            }
        }

        [Fact]
        public void CascadingAuthenticationState_Unauthenticated()
        {
            SignInAs(null, null);

            var appElement = MountAndNavigateToAuthTest(CascadingAuthenticationStateLink);

            Browser.Equal("False", () => appElement.FindElement(By.Id("identity-authenticated")).Text);
            Browser.Equal(string.Empty, () => appElement.FindElement(By.Id("identity-name")).Text);
            Browser.Equal("(none)", () => appElement.FindElement(By.Id("test-claim")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void CascadingAuthenticationState_Authenticated()
        {
            SignInAs("someone cool", null);

            var appElement = MountAndNavigateToAuthTest(CascadingAuthenticationStateLink);

            Browser.Equal("True", () => appElement.FindElement(By.Id("identity-authenticated")).Text);
            Browser.Equal("someone cool", () => appElement.FindElement(By.Id("identity-name")).Text);
            Browser.Equal("Test claim value", () => appElement.FindElement(By.Id("test-claim")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void AuthorizeViewCases_NoAuthorizationRule_NotAuthorized()
        {
            SignInAs(null, null);
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Exists(By.CssSelector("#no-authorization-rule .not-authorized"));
            Browser.Equal("You're not authorized, anonymous", () =>
                appElement.FindElement(By.CssSelector("#no-authorization-rule .not-authorized")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void AuthorizeViewCases_NoAuthorizationRule_Authorized()
        {
            SignInAs("Some User", null);
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("Welcome, Some User!", () =>
                appElement.FindElement(By.CssSelector("#no-authorization-rule .authorized")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void AuthorizeViewCases_RequireRole_Authorized()
        {
            SignInAs("Some User", "IrrelevantRole,TestRole");
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("Welcome, Some User!", () =>
                appElement.FindElement(By.CssSelector("#authorize-role .authorized")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void AuthorizeViewCases_RequireRole_NotAuthorized()
        {
            SignInAs("Some User", "IrrelevantRole");
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("You're not authorized, Some User", () =>
                appElement.FindElement(By.CssSelector("#authorize-role .not-authorized")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void AuthorizeViewCases_RequirePolicy_Authorized()
        {
            SignInAs("Bert", null);
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("Welcome, Bert!", () =>
                appElement.FindElement(By.CssSelector("#authorize-policy .authorized")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void AuthorizeViewCases_RequirePolicy_NotAuthorized()
        {
            SignInAs("Mallory", null);
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            Browser.Equal("You're not authorized, Mallory", () =>
                appElement.FindElement(By.CssSelector("#authorize-policy .not-authorized")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void Router_AllowAnonymous_Anonymous()
        {
            SignInAs(null, null);
            var appElement = MountAndNavigateToAuthTest(PageAllowingAnonymous);
            Browser.Equal("Welcome to PageAllowingAnonymous!", () =>
                appElement.FindElement(By.CssSelector("#auth-success")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void Router_AllowAnonymous_Authenticated()
        {
            SignInAs("Bert", null);
            var appElement = MountAndNavigateToAuthTest(PageAllowingAnonymous);
            Browser.Equal("Welcome to PageAllowingAnonymous!", () =>
                appElement.FindElement(By.CssSelector("#auth-success")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void Router_RequireAuthorization_Authorized()
        {
            SignInAs("Bert", null);
            var appElement = MountAndNavigateToAuthTest(PageRequiringAuthorization);
            Browser.Equal("Welcome to PageRequiringAuthorization!", () =>
                appElement.FindElement(By.CssSelector("#auth-success")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void Router_RequireAuthorization_NotAuthorized()
        {
            SignInAs(null, null);
            var appElement = MountAndNavigateToAuthTest(PageRequiringAuthorization);
            Browser.Equal("Sorry, anonymous, you're not authorized.", () =>
                appElement.FindElement(By.CssSelector("#auth-failure")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void Router_RequirePolicy_Authorized()
        {
            SignInAs("Bert", null);
            var appElement = MountAndNavigateToAuthTest(PageRequiringPolicy);
            Browser.Equal("Welcome to PageRequiringPolicy!", () =>
                appElement.FindElement(By.CssSelector("#auth-success")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void Router_RequirePolicy_NotAuthorized()
        {
            SignInAs("Mallory", null);
            var appElement = MountAndNavigateToAuthTest(PageRequiringPolicy);
            Browser.Equal("Sorry, Mallory, you're not authorized.", () =>
                appElement.FindElement(By.CssSelector("#auth-failure")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void Router_RequireRole_Authorized()
        {
            SignInAs("Bert", "IrrelevantRole,TestRole");
            var appElement = MountAndNavigateToAuthTest(PageRequiringRole);
            Browser.Equal("Welcome to PageRequiringRole!", () =>
                appElement.FindElement(By.CssSelector("#auth-success")).Text);
            AssertExpectedLayoutUsed();
        }

        [Fact]
        public void Router_RequireRole_NotAuthorized()
        {
            SignInAs("Bert", "IrrelevantRole");
            var appElement = MountAndNavigateToAuthTest(PageRequiringRole);
            Browser.Equal("Sorry, Bert, you're not authorized.", () =>
                appElement.FindElement(By.CssSelector("#auth-failure")).Text);
            AssertExpectedLayoutUsed();
        }

        private void AssertExpectedLayoutUsed()
        {
            Browser.Exists(By.Id("auth-links"));
        }

        protected IWebElement MountAndNavigateToAuthTest(string authLinkText)
        {
            Navigate(ServerPathBase);
            var appElement = Browser.MountTestComponent<BasicTestApp.AuthTest.AuthRouter>();
            Browser.Exists(By.Id("auth-links"));
            appElement.FindElement(By.LinkText(authLinkText)).Click();
            return appElement;
        }

        private void SignInAs(string userName, string roles, bool useSeparateTab = false) =>
            Browser.SignInAs(new Uri(_serverFixture.RootUri, "/subdir"), userName, roles, useSeparateTab);
    }
}
