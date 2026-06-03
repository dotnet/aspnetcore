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
            return height != null && height != "0px";
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
    [InlineData("url(https://example.invalid/x.png)")]    // value with url() token
    [InlineData("120.5px")]                               // decimal not emitted by C# (int formatter)
    [InlineData("120px; background:url(x)")]              // value containing extra declarations
    [InlineData("var(--cookie)")]                          // custom-property indirection
    [InlineData("120em")]                                  // wrong unit
    [InlineData("")]                                       // empty
    public void Virtualize_RejectsUnexpectedLayoutAttributeValues(string invalidValue)
    {
        // Values that don't match the C#-emitted shape must not propagate to CSS custom properties.
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

        // Acceptable outcomes: the property is unset or contains the previous value
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
            return heightVar != invalidValue && transformVar != invalidValue;
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
