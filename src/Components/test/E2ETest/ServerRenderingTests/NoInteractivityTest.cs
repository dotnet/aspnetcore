// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
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
    public void ProgrammaticNavigationToNotExistingPathReExecutesTo404(bool streaming)
    {
        string streamingPath = streaming ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/redirection-not-found-ssr{streamingPath}?navigate-programmatically=true");
        AssertReExecutionPageRendered();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LinkNavigationToNotExistingPathReExecutesTo404(bool streaming)
    {
        string streamingPath = streaming ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/redirection-not-found-ssr{streamingPath}");
        Browser.Click(By.Id("link-to-not-existing-page"));
        AssertReExecutionPageRendered();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BrowserNavigationToNotExistingPathReExecutesTo404(bool streaming)
    {
        // non-existing path has to have re-execution middleware set up
        // so it has to have "reexecution" prefix. Otherwise middleware mapping
        // will not be activated, see configuration in Startup
        string streamingPath = streaming ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/not-existing-page-ssr{streamingPath}");
        AssertReExecutionPageRendered();
    }

    private void AssertReExecutionPageRendered() =>
        Browser.Equal("Welcome On Page Re-executed After Not Found Event", () => Browser.Exists(By.Id("test-info")).Text);

    private void AssertNotFoundPageRendered()
    {
        Browser.Equal("Welcome On Custom Not Found Page", () => Browser.FindElement(By.Id("test-info")).Text);
        // custom page should have a custom layout
        Browser.Equal("About", () => Browser.FindElement(By.Id("about-link")).Text);
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

        if (hasCustomNotFoundPageSet)
        {
            AssertNotFoundPageRendered();
        }
        else
        {
            AssertNotFoundFragmentRendered();
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
        AssertNotFoundRendered_ResponseStarted_Or_POST(hasReExecutionMiddleware, hasCustomNotFoundPageSet, testUrl);
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
        AssertNotFoundRendered_ResponseStarted_Or_POST(hasReExecutionMiddleware, hasCustomNotFoundPageSet, testUrl);
        AssertUrlChanged(testUrl);
    }

    private void AssertNotFoundRendered_ResponseStarted_Or_POST(bool hasReExecutionMiddleware, bool hasCustomNotFoundPageSet, string testUrl)
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
            // this throws an exception logged on the server
            AssertNotFoundContentNotRendered();
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
        string testUrl = $"{ServerPathBase}{reexecution}/post-not-found-ssr-streaming?useCustomNotFoundPage={hasCustomNotFoundPageSet}";
        Navigate(testUrl);
        Browser.FindElement(By.Id("not-found-form")).FindElement(By.TagName("button")).Click();

        AssertNotFoundRendered_ResponseStarted_Or_POST(hasReExecutionMiddleware, hasCustomNotFoundPageSet, testUrl);
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

        AssertNotFoundRendered_ResponseStarted_Or_POST(hasReExecutionMiddleware, hasCustomNotFoundPageSet, testUrl);
        AssertUrlNotChanged(testUrl);
    }

    private void AssertNotFoundFragmentRendered() =>
        Browser.Equal("There's nothing here", () => Browser.FindElement(By.Id("not-found-fragment")).Text);

    private void AssertNotFoundContentNotRendered() =>
        Browser.Equal("Any content", () => Browser.FindElement(By.Id("test-info")).Text);

    [Fact]
    public void StatusCodePagesWithReExecution()
    {
        Navigate($"{ServerPathBase}/reexecution/trigger-404");
        Browser.Equal("Re-executed page", () => Browser.Title);
    }
}
