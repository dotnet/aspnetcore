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

    [Fact]
    public void Virtualize_WithItems_DoesNotViolate_StrictStyleCspPolicy()
    {
        // strict-style-csp causes ServerStartup to add `Content-Security-Policy: style-src 'self'`.
        Navigate($"{ServerPathBase}?strict-style-csp=true");
        Browser.MountTestComponent<VirtualizationCsp>();

        var container = Browser.Exists(By.Id("csp-container"));
        Browser.True(() => container.FindElements(By.CssSelector(".csp-item")).Count > 0);

        // Scroll to force spacer style updates which set CSS custom properties via CSSOM.
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript("document.getElementById('csp-container').scrollTop = 5000;");

        // Allow the MutationObserver microtask + CSSOM updates to flush.
        Browser.True(() =>
        {
            var topSpacer = container.FindElements(By.CssSelector(":scope > div"))[0];
            var layout = topSpacer.GetDomAttribute("data-blazor-virtualize-layout");
            return layout != null && !layout.Contains("--blazor-virtualize-height: 0px");
        });

        AssertNoStyleCspViolations();
    }

    [Fact]
    public void Virtualize_WithItemsProvider_DoesNotViolate_StrictStyleCspPolicy()
    {
        // strict-style-csp causes ServerStartup to add `Content-Security-Policy: style-src 'self'`.
        Navigate($"{ServerPathBase}?strict-style-csp=true&mode=items-provider");
        Browser.MountTestComponent<VirtualizationCsp>();

        var container = Browser.Exists(By.Id("csp-container-async"));

        // Wait for either placeholders (during in-flight load) or resolved items.
        Browser.True(() => container.FindElements(
            By.CssSelector(".csp-placeholder, .csp-item-async")).Count > 0);

        // Scroll to trigger new placeholder renders and spacer style updates.
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript("document.getElementById('csp-container-async').scrollTop = 5000;");

        // Wait for resolved items after scroll so the placeholder path has executed.
        Browser.True(() => container.FindElements(By.CssSelector(".csp-item-async")).Count > 0);

        AssertNoStyleCspViolations();
    }

    private void AssertNoStyleCspViolations()
    {
        const string cspErrorMessage = "violates the following Content Security Policy directive: \"style-src";
        var logs = Browser.Manage().Logs.GetLog(LogType.Browser);
        var styleErrors = logs.Where(log => log.Message.Contains(cspErrorMessage)).ToList();
        Assert.Empty(styleErrors);
    }
}
