// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using BasicTestApp;
using BasicTestApp.Reconnection;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
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
        public void DefaultTransportsWorksWithWebSockets()
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
        }

        [Fact]
        public void ErrorIfClientAttemptsLongPollingWithServerOnWebSockets()
        {
            Navigate("/defaultTransport/Transports");

            Browser.Exists(By.Id("startWithLongPollingBtn")).Click();

            var javascript = (IJavaScriptExecutor)Browser;
            Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__start__script__executed__'] === true;"));

            AssertLogContainsMessages(
                "Information: Starting up Blazor server-side application.",
                "Failed to start the connection: Error: Unable to connect to the server with any of the available transports.",
                "Failed to start the circuit.");

            var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
            Assert.NotNull(errorUiElem);
            Assert.Contains("An unhandled exception has occurred. See browser dev tools for details.", errorUiElem.GetAttribute("innerHTML"));
            Browser.Equal("block", () => errorUiElem.GetCssValue("display"));
        }

        [Fact]
        public void ErrorIfWebSocketsConnectionIsRejected()
        {
            Navigate("/defaultTransport/Transports");

            Browser.Exists(By.Id("startAndRejectWebSocketConnectionBtn")).Click();

            var javascript = (IJavaScriptExecutor)Browser;
            Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__start__script__executed__'] === true;"));

            AssertLogContainsMessages(
                "Information: Starting up Blazor server-side application.",
                "Selecting transport 'WebSockets'.",
                "Error: Failed to start the transport 'WebSockets': Error: Don't allow Websockets.",
                "Error: Failed to start the connection: Error: Unable to connect to the server with any of the available transports. Error: WebSockets failed: Error: Don't allow Websockets.",
                "Failed to start the circuit.");

            // Ensure error ui is visible
            var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
            Assert.NotNull(errorUiElem);
            Assert.Contains("Unable to connect, please ensure WebSockets are available. A VPN or proxy may be blocking the connection.", errorUiElem.GetAttribute("innerHTML"));
            Browser.Equal("block", () => errorUiElem.GetCssValue("display"));
        }

        [Fact]
        public void ErrorIfClientAttemptsWebSocketsWithServerOnLongPolling()
        {
            Navigate("/longPolling/Transports");

            Browser.Exists(By.Id("startBlazorServerBtn")).Click();

            var javascript = (IJavaScriptExecutor)Browser;
            Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__start__script__executed__'] === true;"));

            AssertLogContainsMessages(
                "Starting up Blazor server-side application.",
                "Unable to connect to the server with any of the available transports. LongPolling failed: Error: 'LongPolling' is disabled by the client.",
                "Unable to initiate a SignalR connection to the server. This might be because the server is not configured to support WebSockets. To troubleshoot this, visit");

            var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
            Assert.NotNull(errorUiElem);
            Assert.Contains("An unhandled exception has occurred. See browser dev tools for details.", errorUiElem.GetAttribute("innerHTML"));
            Browser.Equal("block", () => errorUiElem.GetCssValue("display"));
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
    }
}
