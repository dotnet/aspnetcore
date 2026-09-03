// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public abstract class AutoPauseTestBase<TRootComponent>
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<TRootComponent>>>
{
    protected AutoPauseTestBase(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<TRootComponent>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected void SetVisibility(string state)
    {
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript($@"
            Object.defineProperty(document, 'visibilityState', {{ configurable: true, get: () => '{state}' }});
            Object.defineProperty(document, 'hidden', {{ configurable: true, get: () => {(state == "hidden" ? "true" : "false")} }});
            document.dispatchEvent(new Event('visibilitychange'));
        ");
    }

    protected void WaitForPausedUI()
    {
        Browser.Equal("block", () =>
            Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
    }

    protected void WaitForResumedUI()
    {
        Browser.Equal("none", () =>
            Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
    }
}
