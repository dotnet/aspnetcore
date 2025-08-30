// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class HeadModificationPrerenderingTest : ServerTestBase<BasicTestAppServerSiteFixture<DeferredComponentContentStartup>>
{
    public HeadModificationPrerenderingTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<DeferredComponentContentStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void CanModifyHeadDuringAndAfterPrerendering()
    {
        Navigate("/deferred-component-content");

        // Check that page medatada was rendered correctly
        Browser.Equal("Title 1", () => Browser.Title);
        Browser.Exists(By.Id("meta-description"));

        BeginInteractivity();

        // Check that page medatada has not changed
        Browser.Equal("Title 1", () => Browser.Title);
        Browser.Exists(By.Id("meta-description"));

        // Check that unrelated <title> elements were left alone
        Browser.Equal("This element is used to test that PageTitle prerendering doesn't interfere with non-head title elements.",
            () => Browser.Exists(By.CssSelector("#svg-for-title-prerendering-test title")).Text);

        var titleText1 = Browser.FindElement(By.Id("title-text-1"));
        titleText1.Clear();
        titleText1.SendKeys("Updated title 1\n");

        var descriptionText1 = Browser.FindElement(By.Id("description-text-1"));
        descriptionText1.Clear();
        descriptionText1.SendKeys("Updated description 1\n");

        // Check that head metadata can be changed after prerendering.
        Browser.Equal("Updated title 1", () => Browser.Title);
        Browser.Equal("Updated description 1", () => Browser.FindElement(By.Id("meta-description")).GetDomAttribute("content"));
    }

    private void BeginInteractivity()
    {
        Browser.Exists(By.Id("load-boot-script")).Click();

        var javascript = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__started__'] === true;"));
    }
}
