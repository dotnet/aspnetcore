// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class VirtualizationCspTest : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public VirtualizationCspTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        // strict-style-csp causes ServerStartup to add `Content-Security-Policy: style-src 'self'`.
        Navigate($"{ServerPathBase}?strict-style-csp=true");
        Browser.MountTestComponent<VirtualizationCsp>();
        Browser.Exists(By.Id("csp-container"));
    }

    [Fact]
    public void Virtualize_DoesNotViolate_StrictStyleCspPolicy()
    {
        var container = Browser.Exists(By.Id("csp-container"));

        // Wait until items have rendered through the Virtualize component.
        Browser.True(() => container.FindElements(By.CssSelector(".csp-item")).Count > 0);

        // Scroll to force spacer/placeholder style updates which set CSS custom properties via CSSOM.
        ((IJavaScriptExecutor)Browser).ExecuteScript(
            "document.getElementById('csp-container').scrollTop = 5000;");

        // Allow the MutationObserver microtask + CSSOM updates to flush.
        Browser.True(() =>
        {
            var topSpacer = container.FindElements(By.CssSelector(":scope > div"))[0];
            var style = topSpacer.GetDomAttribute("data-blazor-style");
            return style != null && !style.Contains("--blazor-virtualize-height: 0px");
        });

        // No style-src CSP violation should appear in the browser console.
        var cspErrorMessage = "violates the following Content Security Policy directive: \"style-src";
        var logs = Browser.Manage().Logs.GetLog(LogType.Browser);
        var styleErrors = logs.Where(log => log.Message.Contains(cspErrorMessage)).ToList();

        Assert.Empty(styleErrors);
    }
}
