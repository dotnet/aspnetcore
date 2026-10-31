// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

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

    [Fact]
    public void UpdatesAuthenticationStateWhenAuthenticationRefreshed()
    {
        SignInAs("user-a", "TestRole", includeNameIdentifier: true);
        var appElement = MountAndNavigateToAuthTest(AuthorizeViewCases, "?captureAuthenticationRefresh");
        Browser.Equal("Welcome, user-a!", () =>
            appElement.FindElement(By.CssSelector("#authorize-role .authorized")).Text);

        var javascript = (IJavaScriptExecutor)Browser;
        var connectionId = Assert.IsType<string>(
            javascript.ExecuteScript("return authenticationRefreshConnection.connectionId;"));

        SignInAs("user-b", "TestRole", useSeparateTab: true, includeNameIdentifier: true);
        Assert.Null(RefreshAuthentication());
        Browser.Equal("Welcome, user-b!", () =>
            appElement.FindElement(By.CssSelector("#authorize-role .authorized")).Text);
        Assert.Equal(
            connectionId,
            Assert.IsType<string>(javascript.ExecuteScript("return authenticationRefreshConnection.connectionId;")));

        SignInAs(null, null, useSeparateTab: true);
        Assert.Null(RefreshAuthentication());
        Browser.Equal("You're not authorized, anonymous", () =>
            appElement.FindElement(By.CssSelector("#authorize-role .not-authorized")).Text);
        Assert.Equal(
            connectionId,
            Assert.IsType<string>(javascript.ExecuteScript("return authenticationRefreshConnection.connectionId;")));

        object RefreshAuthentication() =>
            javascript.ExecuteAsyncScript("""
                const callback = arguments[arguments.length - 1];
                authenticationRefreshConnection.refreshAuthentication().then(
                    () => callback(),
                    error => callback(String(error)));
                """);
    }

    [Fact]
    public void UpdatesAuthenticationStateWhenAuthenticationRefreshesAutomatically()
    {
        SignInAs("user-a", "TestRole", includeNameIdentifier: true);
        var appElement = MountAndNavigateToAuthTest(
            AuthorizeViewCases,
            "?captureAuthenticationRefresh&accelerateAuthenticationRefresh");
        Browser.Equal("Welcome, user-a!", () =>
            appElement.FindElement(By.CssSelector("#authorize-role .authorized")).Text);

        var javascript = (IJavaScriptExecutor)Browser;
        var connectionId = Assert.IsType<string>(
            javascript.ExecuteScript("return authenticationRefreshConnection.connectionId;"));

        SignInAs(null, null, useSeparateTab: true);

        Browser.Equal("You're not authorized, anonymous", () =>
            appElement.FindElement(By.CssSelector("#authorize-role .not-authorized")).Text);
        Assert.Equal(
            connectionId,
            Assert.IsType<string>(javascript.ExecuteScript("return authenticationRefreshConnection.connectionId;")));
    }

    private void SignInAs(
        string userName,
        string roles,
        bool useSeparateTab = false,
        bool includeNameIdentifier = false) =>
        Browser.SignInAs(
            new Uri(_serverFixture.RootUri, "/subdir"),
            userName,
            roles,
            useSeparateTab,
            includeNameIdentifier);

    private void PerformReconnection()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor._internal.forceCloseConnection()");

        // Wait until the reconnection dialog has been shown but is now hidden
        var reconnectModel = Browser.Exists(By.Id("components-reconnect-modal"));
        Browser.Equal("none", () => reconnectModel.GetCssValue("display"));
    }
}
