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

    [Fact]
    public void UsesCurrentLocationAfterWebAssemblyStartup()
    {
        Navigate($"{ServerPathBase}/globally-interactive/startup/Home?delay-webassembly-startup");

        var js = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)js.ExecuteScript("return window.webAssemblyStartup?.waiting === true"));

        Browser.Click(By.Id("next-page"));
        Browser.Equal("Counter", () => Browser.Exists(By.TagName("h1")).Text);
        var expectedUri = Browser.Url;
        js.ExecuteScript("window.webAssemblyStartup.release()");

        Browser.Equal("interactive webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);
        Browser.Equal("Counter", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal(expectedUri, () => Browser.Exists(By.Id("navigation-uri")).Text);

        Browser.Click(By.Id("increment"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count")).Text);

        Browser.Click(By.Id("next-page"));
        Browser.Equal("Final", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal(Browser.Url, () => Browser.Exists(By.Id("navigation-uri")).Text);
    }

    [Fact]
    public void UsesRedirectedLocationAfterWebAssemblyStartup()
    {
        Navigate($"{ServerPathBase}/globally-interactive/startup/Home?delay-webassembly-startup");

        var js = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)js.ExecuteScript("return window.webAssemblyStartup?.waiting === true"));
        // Hold the real fetch until .NET is waiting for the initial component update.
        js.ExecuteScript("""
            const originalFetch = window.fetch;
            const redirectGate = new Promise(resolve => window.webAssemblyStartup.releaseRedirect = resolve);
            window.fetch = async (...args) => {
                if (String(args[0]).endsWith('/globally-interactive/startup-redirect')) {
                    window.fetch = originalFetch;
                    window.webAssemblyStartup.redirectWaiting = true;
                    await redirectGate;
                }
                return originalFetch(...args);
            };
            const getInitialUpdate = Blazor._internal.getInitialComponentsUpdate;
            Blazor._internal.getInitialComponentsUpdate = () => {
                window.webAssemblyStartup.waitingForInitialUpdate = true;
                return getInitialUpdate();
            };
            """);

        Browser.Click(By.Id("redirect-page"));
        Browser.True(() => (bool)js.ExecuteScript("return window.webAssemblyStartup.redirectWaiting === true"));
        Assert.EndsWith("/globally-interactive/startup-redirect", Browser.Url);
        js.ExecuteScript("window.webAssemblyStartup.release()");
        Browser.True(() => (bool)js.ExecuteScript("return window.webAssemblyStartup.waitingForInitialUpdate === true"));
        js.ExecuteScript("window.webAssemblyStartup.releaseRedirect()");

        Browser.Equal("interactive webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);
        Browser.Equal("Counter", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal(Browser.Url, () => Browser.Exists(By.Id("navigation-uri")).Text);
        Assert.EndsWith("/globally-interactive/startup/Counter", Browser.Url);
        Browser.Click(By.Id("increment"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count")).Text);
        Browser.Click(By.Id("next-page"));
        Browser.Equal("Final", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal(Browser.Url, () => Browser.Exists(By.Id("navigation-uri")).Text);
    }

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
            AssertNotFoundContentNotRendered();
        }
    }

    private void AssertNotFoundContentNotRendered() =>
        Browser.Equal("Any content", () => Browser.FindElement(By.Id("test-info")).Text);

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
        AssertReExecutedPageRendered();
    }

    [Theory]
    [InlineData("ServerNonPrerendered")]
    [InlineData("WebAssemblyNonPrerendered")]
    public void DoesNotReExecuteIf404WasHandled_Interactive(string renderMode)
    {
        Navigate($"{ServerPathBase}/reexecution/set-not-found?useCustomNotFoundPage=true&renderMode={renderMode}");
        AssertNotFoundPageRendered();
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

    [Fact]
    public async Task HeadRequestReturnsSuccessWithNoBody()
    {
        // Arrange
        using var client = new HttpClient() { BaseAddress = _serverFixture.RootUri };
        var request = new HttpRequestMessage(HttpMethod.Head, "/subdir/globally-interactive");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Empty(body);
    }
}
