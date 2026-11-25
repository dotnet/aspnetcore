// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using BasicTestApp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class HeadModificationTest : ServerTestBase<HeadModificationTest.HeadModificationFixture>
{
    public HeadModificationTest(
        BrowserFixture browserFixture,
        HeadModificationFixture serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
    }

    public class HeadModificationFixture : ToggleExecutionModeServerFixture<Program>
    {
        private readonly ConcurrentDictionary<string, StylesheetRequestGate> _requests = new();

        public HeadModificationFixture()
        {
            ConfigureEndpoints = endpoints =>
            {
                endpoints.MapGet("/head-swap-replacement/{requestId}.css", async (string requestId, CancellationToken cancellationToken) =>
                {
                    return await WaitForRelease(requestId, cancellationToken);
                });
                endpoints.MapGet("/head-swap-failure/{requestId}.css", async (string requestId, CancellationToken cancellationToken) =>
                {
                    return await WaitForRelease(requestId, cancellationToken, shouldFail: true);
                });
                endpoints.MapGet("/head-swap-idless-replacement/{requestId}.css", async (string requestId, CancellationToken cancellationToken) =>
                {
                    return await WaitForRelease(requestId, cancellationToken);
                });
            };
        }

        internal StylesheetRequestGate GetRequest(string requestId)
            => _requests.GetOrAdd(requestId, static _ => new StylesheetRequestGate());

        private async Task<Microsoft.AspNetCore.Http.IResult> WaitForRelease(
            string requestId,
            CancellationToken cancellationToken,
            bool shouldFail = false)
        {
            var request = GetRequest(requestId);
            request.Requested.TrySetResult();
            try
            {
                await request.Release.Task.WaitAsync(cancellationToken);
                return shouldFail
                    ? Microsoft.AspNetCore.Http.Results.StatusCode(500)
                    : Microsoft.AspNetCore.Http.Results.Text("#swap-target { background-color: green !important; }", "text/css");
            }
            finally
            {
                _requests.TryRemove(requestId, out _);
            }
        }
    }

    internal class StylesheetRequestGate
    {
        internal TaskCompletionSource Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [Fact]
    public async Task ReplacingHeadContentKeepsStylesheetUntilReplacementLoads()
    {
        Browser.MountTestComponent<HeadStylesheetSwap>();
        var target = Browser.Exists(By.Id("swap-target"));
        Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

        StylesheetRequestGate request = null;
        try
        {
            Browser.FindElement(By.Id("swap-head")).Click();
            var requestId = GetRequestId(Browser.Exists(By.CssSelector("link#swap-stylesheet[href*='head-swap-replacement/']")));
            request = _serverFixture.GetRequest(requestId);
            await request.Requested.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal("rgba(0, 0, 255, 1)", target.GetCssValue("background-color"));
            Browser.Equal(1, () => Browser.FindElements(By.Id("swap-stylesheet")).Count);
            Browser.True(() => Browser.Exists(By.Id("swap-stylesheet")).GetDomAttribute("href") is { } href && href.Contains("head-swap-replacement/"));
        }
        finally
        {
            request?.Release.TrySetResult();
        }

        Browser.Equal("rgba(0, 128, 0, 1)", () => target.GetCssValue("background-color"));
        Browser.Equal(1, () => Browser.FindElements(By.Id("swap-stylesheet")).Count);
    }

    [Fact]
    public void RemovingHeadContentWithoutReplacementRemovesStylesheet()
    {
        Browser.MountTestComponent<HeadStylesheetSwap>();
        var target = Browser.Exists(By.Id("swap-target"));
        Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

        Browser.FindElement(By.Id("remove-head-sheet")).Click();

        Browser.Equal("rgba(255, 0, 0, 1)", () => target.GetCssValue("background-color"));
    }

    [Fact]
    public async Task UnmatchedStylesheetsWithoutIdsAreNotTreatedAsReplacements()
    {
        Browser.MountTestComponent<HeadStylesheetSwapWithoutIds>();
        var target = Browser.Exists(By.Id("swap-target"));
        Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

        StylesheetRequestGate request = null;
        try
        {
            Browser.FindElement(By.Id("swap-head-without-ids")).Click();
            var requestId = GetRequestId(Browser.Exists(By.CssSelector("link[rel='stylesheet'][href*='head-swap-idless-replacement/']")));
            request = _serverFixture.GetRequest(requestId);
            await request.Requested.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal("rgba(255, 0, 0, 1)", target.GetCssValue("background-color"));
        }
        finally
        {
            request?.Release.TrySetResult();
        }

        Browser.Equal("rgba(0, 128, 0, 1)", () => target.GetCssValue("background-color"));
    }

    [Fact]
    public async Task DisablingPendingHeadStylesheetRemovesPreviousStylesheet()
    {
        Browser.MountTestComponent<HeadStylesheetSwapPendingReplacement>();
        var target = Browser.Exists(By.Id("swap-target"));
        Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

        StylesheetRequestGate request = null;
        try
        {
            Browser.Exists(By.Id("replace-stylesheet")).Click();
            var replacement = Browser.Exists(By.CssSelector("link#pending-swap-stylesheet[href*='head-swap-replacement/']"));
            request = _serverFixture.GetRequest(GetRequestId(replacement));
            await request.Requested.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

            Browser.Exists(By.Id("disable-replacement")).Click();

            Browser.True(() => Browser.Exists(By.Id("pending-swap-stylesheet")).GetDomAttribute("disabled") is not null);
            Browser.Equal("rgba(255, 0, 0, 1)", () => target.GetCssValue("background-color"));
            Browser.Equal(1, () => Browser.FindElements(By.Id("pending-swap-stylesheet")).Count);
        }
        finally
        {
            request?.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task ChangingPendingHeadStylesheetRelRemovesPreviousStylesheet()
    {
        Browser.MountTestComponent<HeadStylesheetSwapPendingReplacement>();
        var target = Browser.Exists(By.Id("swap-target"));
        Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

        StylesheetRequestGate request = null;
        try
        {
            Browser.Exists(By.Id("replace-stylesheet")).Click();
            var replacement = Browser.Exists(By.CssSelector("link#pending-swap-stylesheet[href*='head-swap-replacement/']"));
            request = _serverFixture.GetRequest(GetRequestId(replacement));
            await request.Requested.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

            Browser.Exists(By.Id("change-replacement-rel")).Click();

            Browser.Equal("alternate", () => Browser.Exists(By.Id("pending-swap-stylesheet")).GetDomAttribute("rel"));
            Browser.Equal("rgba(255, 0, 0, 1)", () => target.GetCssValue("background-color"));

            Browser.Exists(By.Id("remove-replacement")).Click();
            Browser.Equal(0, () => Browser.FindElements(By.Id("pending-swap-stylesheet")).Count);
        }
        finally
        {
            request?.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task FailedHeadStylesheetReplacementRemovesPreviousStylesheet()
    {
        Browser.MountTestComponent<HeadStylesheetSwap>();
        var target = Browser.Exists(By.Id("swap-target"));
        Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

        StylesheetRequestGate request = null;
        try
        {
            Browser.Exists(By.Id("swap-head-failure")).Click();
            var replacement = Browser.Exists(By.CssSelector("link#swap-stylesheet[href*='head-swap-failure/']"));
            request = _serverFixture.GetRequest(GetRequestId(replacement));
            await request.Requested.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));
        }
        finally
        {
            request?.Release.TrySetResult();
        }

        Browser.Equal("rgba(255, 0, 0, 1)", () => target.GetCssValue("background-color"));
        Browser.Equal(1, () => Browser.FindElements(By.Id("swap-stylesheet")).Count);
    }

    [Fact]
    public async Task TimedOutHeadStylesheetReplacementRemovesPreviousStylesheet()
    {
        StylesheetRequestGate request = null;
        var timeoutOverrideInstalled = false;
        try
        {
            ((IJavaScriptExecutor)Browser).ExecuteScript("""
                window.__headStylesheetOriginalSetTimeout = window.setTimeout;
                window.setTimeout = function (callback, delay) {
                  const args = Array.prototype.slice.call(arguments, 2);
                  const adjustedDelay = delay === 30000 ? 5000 : delay;
                  return window.__headStylesheetOriginalSetTimeout.apply(window, [callback, adjustedDelay].concat(args));
                };
                """);
            timeoutOverrideInstalled = true;

            Browser.MountTestComponent<HeadStylesheetSwap>();
            var target = Browser.Exists(By.Id("swap-target"));
            Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

            Browser.Exists(By.Id("swap-head")).Click();
            var replacement = Browser.Exists(By.CssSelector("link#swap-stylesheet[href*='head-swap-replacement/']"));
            request = _serverFixture.GetRequest(GetRequestId(replacement));
            await request.Requested.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal("rgba(0, 0, 255, 1)", target.GetCssValue("background-color"));

            Browser.Equal("rgba(255, 0, 0, 1)", () => target.GetCssValue("background-color"));
            Browser.Equal(1, () => Browser.FindElements(By.Id("swap-stylesheet")).Count);
        }
        finally
        {
            request?.Release.TrySetResult();
            if (timeoutOverrideInstalled)
            {
                ((IJavaScriptExecutor)Browser).ExecuteScript("""
                    window.setTimeout = window.__headStylesheetOriginalSetTimeout;
                    delete window.__headStylesheetOriginalSetTimeout;
                    """);
            }
        }
    }

    [Fact]
    public void ReplacingHeadContentWithDisabledStylesheetRemovesPreviousStylesheet()
    {
        Browser.MountTestComponent<HeadStylesheetSwapDisabledReplacement>();
        var target = Browser.Exists(By.Id("swap-target"));
        Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

        Browser.FindElement(By.Id("swap-head-disabled")).Click();

        Browser.Equal("rgba(255, 0, 0, 1)", () => target.GetCssValue("background-color"));
        Browser.Equal(1, () => Browser.FindElements(By.Id("disabled-swap-stylesheet")).Count);
    }

    private static string GetRequestId(IWebElement link)
    {
        var href = link.GetDomAttribute("href")!;
        return href[(href.LastIndexOf('/') + 1)..^4];
    }

    [Fact]
    public void HeadContentsAreAppended()
    {
        Browser.MountTestComponent<HeadModification>();

        // Wait until the head has been dynamically modified
        Browser.Exists(By.Id("meta-description"));

        // Ensure that the static head contents are untouched.
        Browser.Exists(By.TagName("base"));
    }

    [Fact]
    public void MostRecentlyAttachedPageTitleTakesPriority()
    {
        Browser.MountTestComponent<HeadModification>();

        // Assert initial title
        Browser.Equal("Title 1", () => Browser.Title);

        var titleCheckbox2 = Browser.FindElement(By.Id("title-checkbox-2"));
        titleCheckbox2.Click();

        // Assert that the recently attached PageTitle takes priority
        Browser.Equal("Title 2", () => Browser.Title);

        var titleText1 = Browser.FindElement(By.Id("title-text-1"));
        titleText1.Clear();
        titleText1.SendKeys("Updated title 1\n");

        // Assert changing the content of a PageTitle that is not recently attached takes no effect
        Browser.Equal("Title 2", () => Browser.Title);

        titleCheckbox2.Click();

        // Assert that disposing the most recently attached PageTitle causes the previous one to take effect
        Browser.Equal("Updated title 1", () => Browser.Title);
    }

    [Fact]
    public void MostRecentlyAttachedHeadContentTakesPriority()
    {
        Browser.MountTestComponent<HeadModification>();

        // Assert initial description
        AssertDescriptionEquals("Description 1");

        var descriptionCheckbox2 = Browser.FindElement(By.Id("description-checkbox-2"));
        descriptionCheckbox2.Click();

        // Assert that the recently attached HeadContent takes priority
        AssertDescriptionEquals("Description 2");

        var titleText1 = Browser.FindElement(By.Id("description-text-1"));
        titleText1.Clear();
        titleText1.SendKeys("Updated description 1\n");

        // Assert changing the content of a HeadContent that is not recently attached takes no effect
        AssertDescriptionEquals("Description 2");

        descriptionCheckbox2.Click();

        // Assert that disposing the most recently attached HeadContent causes the previous one to take effect
        AssertDescriptionEquals("Updated description 1");

        void AssertDescriptionEquals(string description)
        {
            Browser.Equal(description, () => Browser.FindElement(By.Id("meta-description")).GetDomAttribute("content"));
        }
    }

    [Fact]
    public void CanFallBackToDefaultTitle()
    {
        Browser.MountTestComponent<HeadModification>();

        // Assert initial title starts as non-default
        Browser.Equal("Title 1", () => Browser.Title);

        var titleCheckbox1 = Browser.FindElement(By.Id("title-checkbox-1"));
        titleCheckbox1.Click();

        // Assert the title is now the default
        Browser.Equal("Basic test app", () => Browser.Title);
    }

    [Fact]
    public void HeadContentGetsAppendedToEndOfHead()
    {
        Browser.MountTestComponent<HeadModification>();

        // Assert that the <meta id="meta-description"> element is the last in the <head>
        Browser.True(() =>
        {
            var metaDescriptionElement = Browser.FindElement(By.Id("meta-description"));
            return (bool)((IJavaScriptExecutor)Browser).ExecuteScript("return document.head.lastChild === arguments[0];", metaDescriptionElement);
        });
    }
}
