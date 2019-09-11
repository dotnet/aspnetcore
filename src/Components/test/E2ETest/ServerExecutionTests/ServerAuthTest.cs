// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ServerAuthTest : AuthTest
    {
        public ServerAuthTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<BasicTestApp.Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output, ExecutionMode.Server)
        {
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "Someone")]
        [InlineData("Someone", null)]
        [InlineData("Someone", "Someone")]
        public void UpdatesAuthenticationStateWhenReconnecting(
            string usernameBefore, string usernameAfter)
        {
            // Establish state before disconnection
            SignInAs(usernameBefore, usernameBefore == null ? null : "TestRole");
            var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases);
            AssertState(usernameBefore);

            // Change authentication state and force reconnection
            SignInAs(usernameAfter, usernameAfter == null ? null : "TestRole", useSeparateTab: true);
            PerformReconnection();
            AssertState(usernameAfter);

            void AssertState(string username)
            {
                if (username == null)
                {
                    Browser.Equal("You're not authorized, anonymous", () =>
                        appElement.FindElement(By.CssSelector("#authorize-role .not-authorized")).Text);
                }
                else
                {
                    Browser.Equal($"Welcome, {username}!", () =>
                        appElement.FindElement(By.CssSelector("#authorize-role .authorized")).Text);
                }
            }
        }

        private void SignInAs(string usernName, string roles, bool useSeparateTab = false) =>
            Browser.SignInAs(new Uri(_serverFixture.RootUri, "/subdir"), usernName, roles, useSeparateTab);

        private void PerformReconnection()
        {
            ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor._internal.forceCloseConnection()");

            // Wait until the reconnection dialog has been shown but is now hidden
            new WebDriverWait(Browser, TimeSpan.FromSeconds(10))
                .Until(driver => driver.FindElement(By.Id("components-reconnect-modal"))?.GetCssValue("display") == "none");
        }
    }
}
