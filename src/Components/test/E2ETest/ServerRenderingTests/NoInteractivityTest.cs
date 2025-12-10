// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

[CollectionDefinition(nameof(InteractivityTest), DisableParallelization = true)]
public class NoInteractivityTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    public NoInteractivityTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void NavigationManagerCanRefreshSSRPageWhenInteractivityNotPresent()
    {
        Navigate($"{ServerPathBase}/forms/form-that-calls-navigation-manager-refresh");

        var guid = Browser.Exists(By.Id("guid")).Text;

        Browser.Exists(By.Id("submit-button")).Click();

        // Checking that the page was refreshed.
        // The redirect request method is GET.
        // Providing a Guid to check that it is not the initial GET request for the page
        Browser.NotEqual(guid, () => Browser.Exists(By.Id("guid")).Text);
        Browser.Equal("GET", () => Browser.Exists(By.Id("method")).Text);
    }

    [Fact]
    public void CanUseServerAuthenticationStateByDefault()
    {
        Navigate($"{ServerPathBase}/auth/static-authentication-state");

        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive")).Text);
        Browser.Equal("Static", () => Browser.FindElement(By.Id("platform")).Text);

        Browser.Equal("False", () => Browser.FindElement(By.Id("identity-authenticated")).Text);
        Browser.Equal("", () => Browser.FindElement(By.Id("identity-name")).Text);
        Browser.Equal("(none)", () => Browser.FindElement(By.Id("test-claim")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-in-test-role-1")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-in-test-role-2")).Text);

        Browser.Click(By.LinkText("Log in"));

        Browser.Equal("True", () => Browser.FindElement(By.Id("identity-authenticated")).Text);
        Browser.Equal("YourUsername", () => Browser.FindElement(By.Id("identity-name")).Text);
        Browser.Equal("Test claim value", () => Browser.FindElement(By.Id("test-claim")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-in-test-role-1")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-in-test-role-2")).Text);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void NavigatesWithoutInteractivityByRequestRedirection(bool controlFlowByException, bool isStreaming)
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException", isEnabled: !controlFlowByException);
        string streaming = isStreaming ? $"streaming-" : "";
        Navigate($"{ServerPathBase}/routing/ssr-{streaming}navigate-to");
        Browser.Equal("Click submit to navigate to home", () => Browser.Exists(By.Id("test-info")).Text);
        Browser.Click(By.Id("redirectButton"));
        Browser.Equal("Routing test cases", () => Browser.Exists(By.Id("test-info")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProgrammaticNavigationToNotExistingPath_ReExecutesTo404(bool streaming)
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException", isEnabled: true);
        string streamingPath = streaming ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/redirection-not-found-ssr{streamingPath}?navigate-programmatically=true");
        AssertReExecutionPageRendered();
    }

    [Fact]
    public void ProgrammaticNavigationToNotExistingPath_AfterAsyncOperation_ReExecutesTo404()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException", isEnabled: true);
        Navigate($"{ServerPathBase}/reexecution/redirection-not-found-ssr?doAsync=true&navigate-programmatically=true");
        AssertReExecutionPageRendered();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LinkNavigationToNotExistingPath_ReExecutesTo404(bool streaming)
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException", isEnabled: true);
        string streamingPath = streaming ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/redirection-not-found-ssr{streamingPath}");
        Browser.Click(By.Id("link-to-not-existing-page"));
        AssertReExecutionPageRendered();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BrowserNavigationToNotExistingPath_ReExecutesTo404(bool streaming)
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException", isEnabled: true);
        // non-existing path has to have re-execution middleware set up
        // so it has to have "reexecution" prefix. Otherwise middleware mapping
        // will not be activated, see configuration in Startup
        string streamingPath = streaming ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/not-existing-page-ssr{streamingPath}");
        AssertReExecutionPageRendered();
    }

    [Fact]
    public void BrowserNavigationToNotExistingPath_WithOnNavigateAsync_ReExecutesTo404()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException", isEnabled: true);
        Navigate($"{ServerPathBase}/reexecution/not-existing-page?useOnNavigateAsync=true");
        AssertReExecutionPageRendered();
    }

    [Fact]
    public void BrowserNavigationToNotExistingPath_WithOnNavigateAsync_ReExecutesTo404_CanStream()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException", isEnabled: true);
        Navigate($"{ServerPathBase}/streaming-reexecution/not-existing-page?useOnNavigateAsync=true");
        AssertReExecutionPageRendered();
        Browser.Equal("Streaming completed.", () => Browser.Exists(By.Id("reexecute-streaming-status")).Text);
    }

    private void AssertReExecutionPageRendered() =>
        Browser.Equal("Welcome On Page Re-executed After Not Found Event", () => Browser.Exists(By.Id("test-info")).Text);

    private void AssertBrowserDefaultNotFoundViewRendered()
    {
        var mainMessage = Browser.FindElement(By.Id("main-message"));

        Browser.True(
            () => mainMessage.FindElement(By.CssSelector("p")).Text
            .Contains("No webpage was found for the web address:", StringComparison.OrdinalIgnoreCase)
        );
    }

    private void AssertLandingPageRendered() =>
        Browser.Equal("Any content", () => Browser.Exists(By.Id("test-info")).Text);

    private void AssertNotFoundPageRendered()
    {
        Browser.Equal("Welcome On Custom Not Found Page", () => Browser.FindElement(By.Id("test-info")).Text);
        // custom page should have a custom layout
        Browser.Equal("About", () => Browser.FindElement(By.Id("about-link")).Text);
    }

    private void AssertNotFoundContentNotRendered(bool responseHasStarted)
    {
        if (!responseHasStarted)
        {
            AssertBrowserDefaultNotFoundViewRendered();
        }
        // otherwise, the render view does not differ from the original page
    }

    private void AssertUrlNotChanged(string expectedUrl) =>
        Browser.True(() => Browser.Url.Contains(expectedUrl), $"Expected URL to contain '{expectedUrl}', but found '{Browser.Url}'");

    private void AssertUrlChanged(string urlPart) =>
        Browser.False(() => Browser.Url.Contains(urlPart), $"Expected URL not to contain '{urlPart}', but found '{Browser.Url}'");

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void NotFoundSetOnInitialization_ResponseNotStarted_SSR(bool hasReExecutionMiddleware, bool hasCustomNotFoundPageSet)
    {
        string reexecution = hasReExecutionMiddleware ? "/reexecution" : "";
        string testUrl = $"{ServerPathBase}{reexecution}/set-not-found-ssr?useCustomNotFoundPage={hasCustomNotFoundPageSet}";
        Navigate(testUrl);

        bool notFoundContentForRenderingProvided = hasCustomNotFoundPageSet || hasReExecutionMiddleware;
        if (notFoundContentForRenderingProvided)
        {
            AssertNotFoundRendered(hasReExecutionMiddleware, hasCustomNotFoundPageSet);
        }
        else
        {
            AssertNotFoundContentNotRendered(responseHasStarted: false);
        }
        AssertUrlNotChanged(testUrl);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    // This tests the application subscribing to OnNotFound event and setting NotFoundEventArgs.Path, opposed to the framework doing it for the app.
    public void NotFoundSetOnInitialization_ApplicationSubscribesToNotFoundEventToSetNotFoundPath_SSR(bool streaming, bool customRouter)
    {
        string streamingPath = streaming ? "-streaming" : "";
        string testUrl = $"{ServerPathBase}/set-not-found-ssr{streamingPath}?useCustomRouter={customRouter}&appSetsEventArgsPath=true";
        Navigate(testUrl);

        bool onlyReExecutionCouldRenderNotFoundPage = !streaming && customRouter;
        if (onlyReExecutionCouldRenderNotFoundPage)
        {
            AssertLandingPageRendered();
        }
        else
        {
            AssertNotFoundPageRendered();
        }
        AssertUrlNotChanged(testUrl);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void NotFoundSetOnInitialization_AfterAsyncOperation_ResponseNotStarted_SSR(bool hasReExecutionMiddleware, bool hasCustomNotFoundPageSet)
    {
        string reexecution = hasReExecutionMiddleware ? "/reexecution" : "";
        string testUrl = $"{ServerPathBase}{reexecution}/set-not-found-ssr?doAsync=true&useCustomNotFoundPage={hasCustomNotFoundPageSet}";
        Navigate(testUrl);

        bool notFoundContentForRenderingProvided = hasCustomNotFoundPageSet || hasReExecutionMiddleware;
        if (notFoundContentForRenderingProvided)
        {
            AssertNotFoundRendered(hasReExecutionMiddleware, hasCustomNotFoundPageSet);
        }
        else
        {
            AssertNotFoundContentNotRendered(responseHasStarted: false);
        }
        AssertUrlNotChanged(testUrl);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    // our custom router does not support NotFoundPage to simulate the most probable custom router behavior
    public void NotFoundSetOnInitialization_ResponseNotStarted_CustomRouter_SSR(bool hasReExecutionMiddleware)
    {
        string reexecution = hasReExecutionMiddleware ? "/reexecution" : "";
        string testUrl = $"{ServerPathBase}{reexecution}/set-not-found-ssr?useCustomRouter=true";
        Navigate(testUrl);

        if (hasReExecutionMiddleware)
        {
            AssertReExecutionPageRendered();
        }
        else
        {
            // Apps that don't support re-execution and don't have blazor's router,
            // cannot render custom NotFound contents.
            // The browser will display default 404 page.
            AssertNotFoundContentNotRendered(responseHasStarted: false);
        }
        AssertUrlNotChanged(testUrl);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void NotFoundSetOnInitialization_ResponseStarted_SSR(bool hasReExecutionMiddleware, bool hasCustomNotFoundPageSet)
    {
        string reexecution = hasReExecutionMiddleware ? "/reexecution" : "";
        string testUrl = $"{ServerPathBase}{reexecution}/set-not-found-ssr-streaming?useCustomNotFoundPage={hasCustomNotFoundPageSet}";
        Navigate(testUrl);
        bool notFoundContentForRenderingProvided = hasCustomNotFoundPageSet || hasReExecutionMiddleware;
        if (notFoundContentForRenderingProvided)
        {
            AssertNotFoundRendered(hasReExecutionMiddleware, hasCustomNotFoundPageSet);
        }
        else
        {
            AssertNotFoundContentNotRendered(responseHasStarted: true);
        }
        AssertUrlNotChanged(testUrl);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void NotFoundSetOnInitialization_ResponseStarted_EnhancedNavigationDisabled_SSR(bool hasReExecutionMiddleware, bool hasCustomNotFoundPageSet)
    {
        EnhancedNavigationTestUtil.SuppressEnhancedNavigation(this, true, skipNavigation: true);
        string reexecution = hasReExecutionMiddleware ? "/reexecution" : "";
        string testUrl = $"{ServerPathBase}{reexecution}/set-not-found-ssr-streaming?useCustomNotFoundPage={hasCustomNotFoundPageSet}";
        Navigate(testUrl);
        AssertNotFoundRendered(hasReExecutionMiddleware, hasCustomNotFoundPageSet);
        AssertUrlChanged(testUrl);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    // our custom router does not support NotFoundPage to simulate the most probable custom router behavior
    public void NotFoundSetOnInitialization_ResponseStarted_CustomRouter_SSR(bool hasReExecutionMiddleware)
    {
        string reexecution = hasReExecutionMiddleware ? "/reexecution" : "";
        string testUrl = $"{ServerPathBase}{reexecution}/set-not-found-ssr-streaming?useCustomRouter=true";
        Navigate(testUrl);

        if (hasReExecutionMiddleware)
        {
            AssertReExecutionPageRendered();
        }
        else
        {
            // Apps that don't support re-execution and don't have blazor's router,
            // cannot render custom NotFound contents.
            // The browser will display default 404 page.
            AssertNotFoundContentNotRendered(responseHasStarted: true);
        }
        AssertUrlNotChanged(testUrl);
    }

    private void AssertNotFoundRendered(bool hasReExecutionMiddleware, bool hasCustomNotFoundPageSet)
    {
        if (hasCustomNotFoundPageSet)
        {
            AssertNotFoundPageRendered();
        }
        else if (hasReExecutionMiddleware)
        {
            AssertReExecutionPageRendered();
        }
        else
        {
            throw new InvalidOperationException("NotFound page will not be rendered without re-execution middleware or custom NotFoundPage set. Use AssertNotFoundNotRendered in this case.");
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void NotFoundSetOnFormSubmit_ResponseNotStarted_SSR(bool hasReExecutionMiddleware, bool hasCustomNotFoundPageSet)
    {
        string reexecution = hasReExecutionMiddleware ? "/reexecution" : "";
        string testUrl = $"{ServerPathBase}{reexecution}/post-not-found-ssr?useCustomNotFoundPage={hasCustomNotFoundPageSet}";
        Navigate(testUrl);
        Browser.FindElement(By.Id("not-found-form")).FindElement(By.TagName("button")).Click();

        bool notFoundContentForRenderingProvided = hasCustomNotFoundPageSet || hasReExecutionMiddleware;
        if (notFoundContentForRenderingProvided)
        {
            AssertNotFoundRendered(hasReExecutionMiddleware, hasCustomNotFoundPageSet);
        }
        else
        {
            AssertNotFoundContentNotRendered(responseHasStarted: false);
        }
        AssertUrlNotChanged(testUrl);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void NotFoundSetOnFormSubmit_AfterAsyncOperation_ResponseNotStarted_SSR(bool hasReExecutionMiddleware, bool hasCustomNotFoundPageSet)
    {
        string reexecution = hasReExecutionMiddleware ? "/reexecution" : "";
        string testUrl = $"{ServerPathBase}{reexecution}/post-not-found-ssr?doAsync=true&useCustomNotFoundPage={hasCustomNotFoundPageSet}";
        Navigate(testUrl);
        Browser.FindElement(By.Id("not-found-form")).FindElement(By.TagName("button")).Click();

        bool notFoundContentForRenderingProvided = hasCustomNotFoundPageSet || hasReExecutionMiddleware;
        if (notFoundContentForRenderingProvided)
        {
            AssertNotFoundRendered(hasReExecutionMiddleware, hasCustomNotFoundPageSet);
        }
        else
        {
            AssertNotFoundContentNotRendered(responseHasStarted: false);
        }
        AssertUrlNotChanged(testUrl);
    }

    [Fact]
    public void NotFoundSetOnFormSubmit_ResponseNotStarted_CustomRouter_SSR()
    {
        string testUrl = $"{ServerPathBase}/reexecution/post-not-found-ssr?useCustomRouter=true";
        Navigate(testUrl);
        Browser.FindElement(By.Id("not-found-form")).FindElement(By.TagName("button")).Click();

        AssertReExecutionPageRendered();
        AssertUrlNotChanged(testUrl);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void NotFoundSetOnFormSubmit_ResponseStarted_SSR(bool hasReExecutionMiddleware, bool hasCustomNotFoundPageSet)
    {
        string reexecution = hasReExecutionMiddleware ? "/reexecution" : "";
        string testUrl = $"{ServerPathBase}{reexecution}/post-not-found-ssr-streaming?useCustomNotFoundPage={hasCustomNotFoundPageSet}";
        Navigate(testUrl);
        Browser.FindElement(By.Id("not-found-form")).FindElement(By.TagName("button")).Click();

        bool notFoundContentForRenderingProvided = hasCustomNotFoundPageSet || hasReExecutionMiddleware;
        if (notFoundContentForRenderingProvided)
        {
            AssertNotFoundRendered(hasReExecutionMiddleware, hasCustomNotFoundPageSet);
        }
        else
        {
            AssertNotFoundContentNotRendered(responseHasStarted: true);
        }
        AssertUrlNotChanged(testUrl);
    }

    [Fact]
    public void NotFoundSetOnFormSubmit_ResponseStarted_CustomRouter_SSR()
    {
        string testUrl = $"{ServerPathBase}/reexecution/post-not-found-ssr-streaming?useCustomRouter=true";
        Navigate(testUrl);
        Browser.FindElement(By.Id("not-found-form")).FindElement(By.TagName("button")).Click();

        AssertReExecutionPageRendered();
        AssertUrlNotChanged(testUrl);
    }

    [Fact]
    public void StatusCodePagesWithReExecution()
    {
        Navigate($"{ServerPathBase}/reexecution/trigger-404");
        Browser.Equal("Re-executed page", () => Browser.Title);
    }
}
