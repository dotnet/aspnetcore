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
}
