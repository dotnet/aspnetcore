// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

[CollectionDefinition(nameof(InteractivityTest), DisableParallelization = true)]
public class EndpointsServerReconnectionTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public EndpointsServerReconnectionTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void ReconnectUI_Displays_OnFirstReconnect()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        var javascript = (IJavaScriptExecutor)Browser;

        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");
        Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
        Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
        Browser.Exists(By.Id("increment-0")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);
    }

    [Fact]
    public void ReconnectUI_Displays_OnSuccessiveReconnects_AfterEnhancedNavigation()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        var javascript = (IJavaScriptExecutor)Browser;

        // Perform the first reconnect
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");
        Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
        Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
        Browser.Exists(By.Id("increment-0")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);

        // Perform an enhanced navigation by updating the component's parameters
        Browser.Exists(By.Id("update-counter-link-0")).Click();
        Browser.Equal("2", () => Browser.FindElement(By.Id("increment-amount-0")).Text);

        // Perform the second reconnect
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");
        Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
        Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
        Browser.Exists(By.Id("increment-0")).Click();
        Browser.Equal("3", () => Browser.Exists(By.Id("count-0")).Text);
    }

    [Fact]
    public void RootComponentOperation_Add_WaitsUntilReconnection()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

        // Remove the reconnection modal so we can interact with the page while reconnection is in progress
        // We'll store the modal on the 'window' object so we can read its state without it needing
        // to be in the DOM
        var reconnectModal = Browser.Exists(By.Id("components-reconnect-modal"));
        RemoveReconnectModal(javascript, reconnectModal);

        // Add a new component via enhanced update while reconnection is in progress
        Browser.Click(By.Id("add-server-counter-prerendered-link"));

        // Assert that the component was added
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-1")).Text);

        // Assert that we go from a disconnected to reconnected state
        Browser.Equal("block", () => GetRemovedReconnectModalDisplay(javascript));
        Browser.Equal("none", () => GetRemovedReconnectModalDisplay(javascript));

        // Assert that we become interactive when reconnection completes
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
    }

    [Fact]
    public void RootComponentOperation_Update_WaitsUntilReconnection()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

        // Remove the reconnection modal so we can interact with the page while reconnection is in progress
        // We'll store the modal on the 'window' object so we can read its state without it needing
        // to be in the DOM
        var reconnectModal = Browser.Exists(By.Id("components-reconnect-modal"));
        RemoveReconnectModal(javascript, reconnectModal);

        // Update the existing counter's increment amount via enhanced update while reconnection is in progress
        Browser.Click(By.Id("update-counter-link-0"));

        // Assert that we go from a disconnected to reconnected state
        Browser.Equal("block", () => GetRemovedReconnectModalDisplay(javascript));
        Browser.Equal("none", () => GetRemovedReconnectModalDisplay(javascript));

        // Assert that the increment amount was updated after the browser reconnected
        Browser.Equal("2", () => Browser.FindElement(By.Id("increment-amount-0")).Text);
        Browser.Click(By.Id("increment-0"));
        Browser.Equal("2", () => Browser.FindElement(By.Id("count-0")).Text);
    }

    [Fact]
    public void RootComponentOperation_Remove_WaitsUntilReconnection()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

        // Remove the reconnection modal so we can interact with the page while reconnection is in progress
        // We'll store the modal on the 'window' object so we can read its state without it needing
        // to be in the DOM
        var reconnectModal = Browser.Exists(By.Id("components-reconnect-modal"));
        RemoveReconnectModal(javascript, reconnectModal);

        // Remove the counter via enhanced update while reconnection is in progress
        Browser.Click(By.Id("remove-counter-link-1"));
        Browser.DoesNotExist(By.Id("remove-counter-link-1"));

        AssertBrowserLogDoesNotContainMessage($"Counter 1 was disposed");

        // Assert that we go from a disconnected to reconnected state
        Browser.Equal("block", () => GetRemovedReconnectModalDisplay(javascript));
        Browser.Equal("none", () => GetRemovedReconnectModalDisplay(javascript));

        // Assert that the component was disposed after the browser reconnected
        AssertBrowserLogContainsMessage($"Counter 1 was disposed");
    }

    private static void RemoveReconnectModal(IJavaScriptExecutor javascript, IWebElement reconnectModal)
    {
        javascript.ExecuteScript("""
            window.reconnectModal = arguments[0];
            window.reconnectModal.remove();
            """, reconnectModal);
    }

    private static string GetRemovedReconnectModalDisplay(IJavaScriptExecutor javascript)
    {
        return (string)javascript.ExecuteScript("return window.reconnectModal.style.display;");
    }

    private void AssertBrowserLogContainsMessage(string message)
        => Browser.True(() => DoesBrowserLogContainMessage(message));

    private void AssertBrowserLogDoesNotContainMessage(string message)
        => Browser.False(() => DoesBrowserLogContainMessage(message));

    private bool DoesBrowserLogContainMessage(string message)
    {
        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        return entries.Any(entry => entry.Message.Contains(message));
    }
}
