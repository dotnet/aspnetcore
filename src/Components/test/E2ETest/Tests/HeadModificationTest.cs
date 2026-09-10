// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class HeadModificationTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public HeadModificationTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
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
