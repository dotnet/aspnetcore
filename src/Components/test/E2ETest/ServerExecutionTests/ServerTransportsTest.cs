// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerTransportsTest : ServerTestBase<BasicTestAppServerSiteFixture<TransportsServerStartup>>
{
    public ServerTransportsTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<TransportsServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void DefaultTransportsWorks_WithWebSockets()
    {
        Navigate("/defaultTransport/Transports");

        Browser.Exists(By.Id("startBlazorServerBtn")).Click();

        var javascript = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__start__script__executed__'] === true;"));

        AssertLogContainsMessages(
            "Starting up Blazor server-side application.",
            "WebSocket connected to ws://",
            "Received render batch with",
            "The HttpConnection connected successfully.",
            "Blazor server-side application started.");

        AssertGlobalErrorState(hasGlobalError: false);
    }

    [Fact]
    public void ErrorIfClientAttemptsLongPolling_WithServerOnWebSockets()
    {
        Navigate("/webSockets/Transports");

        Browser.Exists(By.Id("startWithLongPollingBtn")).Click();

        var javascript = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__start__script__executed__'] === true;"));

        AssertLogContainsMessages(
            "Information: Starting up Blazor server-side application.",
            "Skipping transport 'WebSockets' because it was disabled by the client",
            "Failed to start the connection: Error: Unable to connect to the server with any of the available transports.",
            "Failed to start the circuit.");

        AssertGlobalErrorState(hasGlobalError: true);
    }

    [Fact]
    public void WebSocketsConnectionIsRejected_FallbackToLongPolling()
    {
        Navigate("/defaultTransport/Transports");

        Browser.Exists(By.Id("startAndRejectWebSocketConnectionBtn")).Click();

        var javascript = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__start__script__executed__'] === true;"));

        AssertLogContainsMessages(
            "Information: Starting up Blazor server-side application.",
            "Selecting transport 'LongPolling'",
            "Failed to connect via WebSockets, using the Long Polling fallback transport. This may be due to a VPN or proxy blocking the connection. To troubleshoot this, visit",
            "Blazor server-side application started.");

        AssertGlobalErrorState(hasGlobalError: false);
    }

    [Fact]
    public void ErrorIfWebSocketsConnectionIsRejected_WithServerOnWebSockets()
    {
        Navigate("/webSockets/Transports");

        Browser.Exists(By.Id("startAndRejectWebSocketConnectionBtn")).Click();

        var javascript = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__start__script__executed__'] === true;"));

        AssertLogContainsMessages(
            "Information: Starting up Blazor server-side application.",
            "Selecting transport 'WebSockets'.",
            "Error: Failed to start the transport 'WebSockets': Error: Don't allow Websockets.",
            "Error: Failed to start the connection: Error: Unable to connect to the server with any of the available transports. Error: WebSockets failed: Error: Don't allow Websockets.",
            "Failed to start the circuit.");

        AssertGlobalErrorState(hasGlobalError: true);
    }

    [Fact]
    public void ServerOnlySupportsLongPolling_FallbackToLongPolling()
    {
        Navigate("/longPolling/Transports");

        Browser.Exists(By.Id("startBlazorServerBtn")).Click();

        var javascript = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__start__script__executed__'] === true;"));

        AssertLogContainsMessages(
            "Starting up Blazor server-side application.",
            "Selecting transport 'LongPolling'.",
            "Failed to connect via WebSockets, using the Long Polling fallback transport. This may be due to a VPN or proxy blocking the connection. To troubleshoot this, visit",
            "Blazor server-side application started.");

        AssertGlobalErrorState(hasGlobalError: false);
    }

    [Fact]
    public void ErrorIfClientDisablesLongPolling_WithServerOnLongPolling()
    {
        Navigate("/longPolling/Transports");

        Browser.Exists(By.Id("startWithWebSocketsBtn")).Click();

        var javascript = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__start__script__executed__'] === true;"));

        AssertLogContainsMessages(
            "Starting up Blazor server-side application.",
            "Unable to connect to the server with any of the available transports. LongPolling failed: Error: 'LongPolling' is disabled by the client.",
            "Unable to initiate a SignalR connection to the server. This might be because the server is not configured to support WebSockets. For additional details, visit");

        AssertGlobalErrorState(hasGlobalError: true);
    }

    void AssertLogContainsMessages(params string[] messages)
    {
        var log = Browser.Manage().Logs.GetLog(LogType.Browser);
        foreach (var message in messages)
        {
            Assert.Contains(log, entry =>
            {
                return entry.Message.Contains(message, StringComparison.InvariantCulture);
            });
        }
    }

    void AssertGlobalErrorState(bool hasGlobalError)
    {
        var globalErrorUi = Browser.Exists(By.Id("blazor-error-ui"));
        Browser.Equal(hasGlobalError ? "block" : "none", () => globalErrorUi.GetCssValue("display"));
    }
}
