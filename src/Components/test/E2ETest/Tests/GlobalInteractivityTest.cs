// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class GlobalInteractivityTest(
    BrowserFixture browserFixture,
    BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<GlobalInteractivityApp>> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<GlobalInteractivityApp>>>(browserFixture, serverFixture, output)
{
    [Fact]
    public void CanFindStaticallyRenderedPageAfterClickingBrowserBackButtonOnDynamicallyRenderedPage()
    {
        // Start on a static page
        Navigate("/subdir/globally-interactive/static-via-url");
        Browser.Equal("Global interactivity page: Static via URL", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Navigate to an interactive page and observe it really is interactive
        Browser.Click(By.LinkText("Globally-interactive by default"));
        Browser.Equal("Global interactivity page: Default", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("interactive webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Show that, after "back", we revert to the previous page
        Browser.Navigate().Back();
        Browser.Equal("Global interactivity page: Static via URL", () => Browser.Exists(By.TagName("h1")).Text);
    }

    [Fact]
    public void CanNavigateFromStaticToInteractiveAndBack()
    {
        // Start on a static page
        Navigate("/subdir/globally-interactive/static-via-attribute");
        Browser.Equal("Global interactivity page: Static via attribute", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Navigate to an interactive page and observe it really is interactive
        Browser.Click(By.LinkText("Globally-interactive by default"));
        Browser.Equal("Global interactivity page: Default", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("interactive webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Show that, after "back", we revert to static rendering on the previous page
        Browser.Navigate().Back();
        Browser.Equal("Global interactivity page: Static via attribute", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);
    }

    [Fact]
    public void CanNavigateFromInteractiveToStaticAndBack()
    {
        // Start on an interactive page
        Navigate("/subdir/globally-interactive");
        Browser.Equal("Global interactivity page: Default", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("interactive webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Navigate to a static page
        Browser.Click(By.LinkText("Static via attribute"));
        Browser.Equal("Global interactivity page: Static via attribute", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Show that, after "back", we revert to interactive rendering on the previous page
        Browser.Navigate().Back();
        Browser.Equal("Global interactivity page: Default", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("interactive webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);
    }

    [Fact]
    public void CanNavigateBetweenStaticPagesViaEnhancedNav()
    {
        // Start on a static page
        Navigate("/subdir/globally-interactive/static-via-attribute");
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);
        var h1 = Browser.Exists(By.TagName("h1"));
        Assert.Equal("Global interactivity page: Static via attribute", h1.Text);

        // Navigate to another static page
        // We check it's the same h1 element, because this is enhanced nav
        Browser.Click(By.LinkText("Static via URL"));
        Browser.Equal("Global interactivity page: Static via URL", () => h1.Text);
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Back also works
        Browser.Navigate().Back();
        Browser.Equal("Global interactivity page: Static via attribute", () => h1.Text);
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);
    }
}
