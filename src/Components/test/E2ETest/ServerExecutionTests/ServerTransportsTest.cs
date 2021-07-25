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
    public class ServerTransportsTest : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
    {
        public ServerTransportsTest(
            BrowserFixture browserFixture,
            BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        [Fact]
        public void DefaultTransportsWorksWithWebSockets()
        {
            Navigate("/subdir/Transports");

            Browser.Exists(By.Id("startBlazorServerBtn")).Click();

            var javascript = (IJavaScriptExecutor)Browser;
            Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__started__'] === true;"));

            AssertLogContainsMessages(
                "Starting up Blazor server-side application.",
                "WebSocket connected to ws://",
                "Received render batch with",
                "The HttpConnection connected successfully.",
                "Blazor server-side application started.");
        }

        [Fact]
        public void ErrorIfBrowserDoesNotSupportWebSockets()
        {
            Navigate("subdir/Transports");

            Browser.Exists(By.Id("startWithWebSocketsDisabledInBrowserBtn")).Click();

            var javascript = (IJavaScriptExecutor)Browser;
            Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__started__'] === true;"));

            AssertLogContainsMessages(
                "Information: Starting up Blazor server-side application.",
                "Failed to start the connection: Error: Unable to connect to the server with any of the available transports. WebSockets failed: UnsupportedTransportWebSocketsError: 'WebSockets' is not supported in your environment.",
                "Failed to start the circuit.");

            // Ensure error ui is visible
            var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
            Assert.NotNull(errorUiElem);
            Assert.Contains("Unable to connect, please ensure you are using an updated browser that supports WebSockets.", errorUiElem.GetAttribute("innerHTML"));
            Browser.Equal("block", () => errorUiElem.GetCssValue("display"));
        }

        [Fact]
        public void ErrorIfClientAttemptsLongPollingWithServerOnWebSockets()
        {
            Navigate("subdir/Transports");

            Browser.Exists(By.Id("startWithLongPollingBtn")).Click();

            var javascript = (IJavaScriptExecutor)Browser;
            Browser.False(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__error__'] === true;"));

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
            Navigate("subdir/Transports");

            Browser.Exists(By.Id("startAndRejectWebSocketConnectionBtn")).Click();

            var javascript = (IJavaScriptExecutor)Browser;
            Browser.False(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__error__'] === true;"));

            AssertLogContainsMessages(
                "Information: Starting up Blazor server-side application.",
                "Selecting transport 'WebSockets'.",
                "Error: Failed to start the transport 'WebSockets': Error: Don't allow Websockets.",
                "Error: Failed to start the connection: Error: Unable to connect to the server with any of the available transports. FailedToStartTransportWebSocketsError: WebSockets failed: Error: Don't allow Websockets.",
                "Failed to start the circuit.");

            // Ensure error ui is visible
            var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
            Assert.NotNull(errorUiElem);
            Assert.Contains("Unable to connect, please ensure WebSockets are available. A VPN or proxy may be blocking the connection.", errorUiElem.GetAttribute("innerHTML"));
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
