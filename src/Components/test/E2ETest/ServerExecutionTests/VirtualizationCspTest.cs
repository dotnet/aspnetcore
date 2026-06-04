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
            var height = topSpacer.GetDomAttribute("data-blazor-virtualize-reserved-height");
            return height != null && height != "0";
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

    [Theory]
    [InlineData("url(https://example.invalid/x.png)")]    // non-numeric, CSS-like
    [InlineData("120px")]                                  // a unit suffix is no longer accepted
    [InlineData("abc")]                                    // non-numeric
    [InlineData("1e9999")]                                 // overflows to Infinity (not finite)
    [InlineData("NaN")]                                    // non-finite
    [InlineData("1 2")]                                    // multi-token
    [InlineData("")]                                       // empty
    public void Virtualize_RejectsUnexpectedLayoutAttributeValues(string invalidValue)
    {
        // Values that don't parse as a finite number must not propagate to CSS custom properties.
        Navigate($"{ServerPathBase}?strict-style-csp=true");
        Browser.MountTestComponent<VirtualizationCsp>();

        var container = Browser.Exists(By.Id("csp-container"));
        Browser.True(() => container.FindElements(By.CssSelector(".csp-item")).Count > 0);

        var js = (IJavaScriptExecutor)Browser;
        const string spacerSelector = "#csp-container > div:first-of-type";

        js.ExecuteScript(
            "var el = document.querySelector(arguments[0]);" +
            "el.setAttribute('data-blazor-virtualize-reserved-height', arguments[1]);" +
            "el.setAttribute('data-blazor-virtualize-loop-breaker-transform', arguments[1]);",
            spacerSelector, invalidValue);

        // Invalid values must result in the custom property being removed (empty).
        Browser.True(() =>
        {
            var heightVar = (string)js.ExecuteScript(
                "return getComputedStyle(document.querySelector(arguments[0]))" +
                ".getPropertyValue('--blazor-virtualize-reserved-height').trim();",
                spacerSelector);
            var transformVar = (string)js.ExecuteScript(
                "return getComputedStyle(document.querySelector(arguments[0]))" +
                ".getPropertyValue('--blazor-virtualize-loop-breaker-transform').trim();",
                spacerSelector);
            return heightVar == "" && transformVar == "";
        });

        AssertNoStyleCspViolations();
    }

    [Fact]
    public void QuickGrid_WithVirtualize_DoesNotViolate_StrictStyleCspPolicy()
    {
        // QuickGrid uses Virtualize internally. Validates the integration is CSP-clean end-to-end.
        Navigate($"{ServerPathBase}?strict-style-csp=true");
        Browser.MountTestComponent<BasicTestApp.QuickGridTest.QuickGridCsp>();

        var container = Browser.Exists(By.Id("csp-quickgrid"));

        // Wait for QuickGrid to render at least one data row.
        Browser.True(() => container.FindElements(By.CssSelector("tbody tr")).Count > 0);

        // Scroll to force spacer style updates which set CSS custom properties via CSSOM.
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript("document.getElementById('csp-quickgrid').scrollTop = 20000;");

        // Wait for the top spacer's reserved-height attribute to grow past 0 after scrolling.
        Browser.True(() =>
        {
            var spacers = container.FindElements(By.CssSelector("[data-blazor-virtualize-reserved-height]"));
            return spacers.Count > 0
                && spacers[0].GetDomAttribute("data-blazor-virtualize-reserved-height") != "0";
        });

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
