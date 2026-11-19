// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        internal TaskCompletionSource Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public HeadModificationFixture()
        {
            ConfigureGateway = app => app.MapGet("/head-swap-replacement.css", async (CancellationToken cancellationToken) =>
            {
                Requested.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
                return Microsoft.AspNetCore.Http.Results.Text("#swap-target { background-color: green !important; }", "text/css");
            });
        }
    }

    [Fact]
    public async Task ReplacingHeadContentKeepsStylesheetUntilReplacementLoads()
    {
        Browser.MountTestComponent<HeadStylesheetSwap>();
        var target = Browser.Exists(By.Id("swap-target"));
        Browser.Equal("rgba(0, 0, 255, 1)", () => target.GetCssValue("background-color"));

        try
        {
            Browser.FindElement(By.Id("swap-head")).Click();
            await _serverFixture.Requested.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal("rgba(0, 0, 255, 1)", target.GetCssValue("background-color"));
        }
        finally
        {
            _serverFixture.Release.TrySetResult();
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
