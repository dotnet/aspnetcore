// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest;
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

    [Theory]
    [InlineData("server", false)]
    [InlineData("webassembly", false)]
    [InlineData("server", true)]
    [InlineData("webassembly", true)]
    public void CanRenderNotFoundInteractive(string renderingMode, bool useCustomNotFoundPage)
    {
        string query = useCustomNotFoundPage ? "?useCustomNotFoundPage=true" : "";
        Navigate($"{ServerPathBase}/render-not-found-{renderingMode}{query}");

        var buttonId = "trigger-not-found";
        Browser.WaitForElementToBeVisible(By.Id(buttonId));
        Browser.Exists(By.Id(buttonId)).Click();

        if (useCustomNotFoundPage)
        {
            AssertNotFoundPageRendered();
        }
        else
        {
            var bodyText = Browser.FindElement(By.TagName("body")).Text;
            Assert.Contains("There's nothing here", bodyText);
        }
    }

    [Fact]
    public async Task CanSetNotFoundStatus()
    {
        var statusCode = await GetStatusCodeAsync($"/subdir/render-not-found-ssr");
        Assert.Equal(HttpStatusCode.NotFound, statusCode);
    }

    private async Task<HttpStatusCode> GetStatusCodeAsync(string relativeUrl)
    {
        using var client = new HttpClient() { BaseAddress = _serverFixture.RootUri };
        var response = await client.GetAsync(relativeUrl, HttpCompletionOption.ResponseHeadersRead);
        return response.StatusCode;
    }

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanRenderNotFoundPage_SSR(bool streamingStarted)
    {
        string streamingPath = streamingStarted ? "-streaming" : "";
        Navigate($"{ServerPathBase}/set-not-found-ssr{streamingPath}?useCustomNotFoundPage=true");
        AssertNotFoundPageRendered();
    }

    [Theory]
    [InlineData("ServerNonPrerendered")]
    [InlineData("WebAssemblyNonPrerendered")]
    public void CanRenderNotFoundPage_Interactive(string renderMode)
    {
        Navigate($"{ServerPathBase}/set-not-found?useCustomNotFoundPage=true&renderMode={renderMode}");
        AssertNotFoundPageRendered();
    }

    private void AssertNotFoundPageRendered()
    {
        Browser.Equal("Welcome On Custom Not Found Page", () => Browser.FindElement(By.Id("test-info")).Text);
        // custom page should have a custom layout
        Browser.Equal("About", () => Browser.FindElement(By.Id("about-link")).Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanRenderNotFoundIfNotFoundPageTypeNotProvided_SSR(bool streamingStarted)
    {
        string streamingPath = streamingStarted ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/set-not-found-ssr{streamingPath}");
        if (streamingStarted)
        {
            AssertReExecutedPageRendered();
        }
        else
        {
            AssertNotFoundFragmentRendered();
        }
    }

    [Theory]
    [InlineData("ServerNonPrerendered")]
    [InlineData("WebAssemblyNonPrerendered")]
    public void DoesNotReExecuteIf404WasHandled_Interactive(string renderMode)
    {
        Navigate($"{ServerPathBase}/reexecution/set-not-found?renderMode={renderMode}");
        AssertNotFoundFragmentRendered();
    }

    private void AssertNotFoundFragmentRendered() =>
        Browser.Equal("There's nothing here", () => Browser.FindElement(By.Id("not-found-fragment")).Text);

    [Fact]
    public void StatusCodePagesWithReExecution()
    {
        Navigate($"{ServerPathBase}/reexecution/trigger-404");
        AssertReExecutedPageRendered();
    }
    private void AssertReExecutedPageRendered() =>
        Browser.Equal("Welcome On Page Re-executed After Not Found Event", () => Browser.Exists(By.Id("test-info")).Text);
}
