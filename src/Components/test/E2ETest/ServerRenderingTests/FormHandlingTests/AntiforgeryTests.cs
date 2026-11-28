// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Components.TestServer.RazorComponents;
using Components.TestServer.Services;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.FormHandlingTests;

public class AntiforgeryTests : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public AntiforgeryTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    public void CanUseAntiforgeryAfterInitialRender(string target)
    {
        Navigate($"{ServerPathBase}/{target}-antiforgery-form");

        Browser.Exists(By.Id("interactive"));

        Browser.Click(By.Id("render-form"));
        Browser.True(() => !string.IsNullOrEmpty(Browser.Exists(By.Name("__RequestVerificationToken")).GetDomProperty("value")));

        var input = Browser.Exists(By.Id("name"));
        input.SendKeys("Test");
        var submit = Browser.Exists(By.Id("submit"));
        submit.Click();

        Browser.Equal("Test", () => Browser.FindElement(By.Id("result")).Text);
    }

    [Fact]
    public Task CrossSiteStreamingNavigationDoesNotInvalidateExistingForm()
        => VerifyExistingFormAfterStreamingNavigation(crossSite: true);

    [Fact]
    public Task DirectNavigationStillStreamsAndPreservesExistingForm()
        => VerifyExistingFormAfterStreamingNavigation(crossSite: false);

    [Fact]
    public Task CrossSiteNavigationWithCookieStillStreamsAndPreservesExistingForm()
        => VerifyExistingFormAfterStreamingNavigation(crossSite: true, includeCrossSiteCookie: true);

    [Fact]
    public Task RemovedInteractiveLoadingComponentDoesNotInvalidateExistingForm()
        => VerifyExistingFormAfterStreamingNavigation(crossSite: true, interactiveLoading: true);

    [Fact]
    public Task RemovingLoadingComponentPreservesRemainingInteractiveRoot()
        => VerifyExistingFormAfterStreamingNavigation(crossSite: true, interactiveLoading: true, retainInteractiveComponent: true);

    private async Task VerifyExistingFormAfterStreamingNavigation(
        bool crossSite, bool interactiveLoading = false, bool retainInteractiveComponent = false, bool includeCrossSiteCookie = false)
    {
        EnhancedNavigationTestUtil.SuppressEnhancedNavigation(this, shouldSuppress: true);
        Navigate($"{ServerPathBase}/forms/default-form");
        var formWindow = Browser.CurrentWindowHandle;
        var token = Browser.Exists(By.Name("__RequestVerificationToken")).GetDomProperty("value");
        var cookie = Assert.Single(Browser.Manage().Cookies.AllCookies,
            cookie => cookie.Name.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
        if (includeCrossSiteCookie)
        {
            Browser.Manage().Cookies.AddCookie(new Cookie(
                cookie.Name, cookie.Value, cookie.Domain, cookie.Path, cookie.Expiry, cookie.Secure, cookie.IsHttpOnly, "Lax"));
        }
        var operations = _serverFixture.Host.Services.GetRequiredService<AsyncOperationService>();
        var id = Guid.NewGuid().ToString("N");
        var started = operations.Start($"{id}-started");
        var responseStarted = operations.Start($"{id}-response-started");
        var destination = new Uri(_serverFixture.RootUri,
            $"{ServerPathBase}/forms/antiforgery-streaming?id={id}&interactiveLoading={interactiveLoading}&retainInteractiveComponent={retainInteractiveComponent}");
        Browser.SwitchTo().NewWindow(WindowType.Tab);
        var navigationWindow = Browser.CurrentWindowHandle;
        var streamingWindow = navigationWindow;

        try
        {
            if (crossSite)
            {
                streamingWindow = OpenCrossSiteLink(destination);
            }
            else
            {
                Browser.Navigate().GoToUrl(destination);
            }
            await started.WaitAsync(TimeSpan.FromSeconds(30));
            await responseStarted.WaitAsync(TimeSpan.FromSeconds(10));
            Browser.SwitchTo().Window(streamingWindow);
            Browser.Equal("Waiting for release", () => Browser.Exists(By.Id("waiting")).Text);
            operations.Complete(id);
            Browser.Equal("Released", () => Browser.Exists(By.Id("released")).Text);
            Browser.Equal(crossSite ? "cross-site" : "none", () => Browser.Exists(By.Id("fetch-site")).Text);
            Browser.Equal(crossSite && !includeCrossSiteCookie ? "False" : "True", () => Browser.Exists(By.Id("received-cookie")).Text);
            if (interactiveLoading)
            {
                Browser.Equal("True", () => Browser.Exists(By.Id("loading-initialized")).Text);
                Browser.DoesNotExist(By.Id("interactive-loading"));
            }
            var currentCookie = Browser.Manage().Cookies.GetCookieNamed(cookie.Name);
            if (retainInteractiveComponent)
            {
                Assert.Equal(cookie.Value, currentCookie.Value);
                Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-retained")).Text);
                Browser.Exists(By.Id("increment-retained")).Click();
                Browser.Equal("1", () => Browser.Exists(By.Id("count-retained")).Text);
            }

            Browser.SwitchTo().Window(formWindow);
            Assert.Equal(token, Browser.Exists(By.Name("__RequestVerificationToken")).GetDomProperty("value"));
            Browser.Exists(By.Id("send")).Click();
            Browser.Equal("Form submitted!", () => Browser.Exists(By.Id("pass")).Text);
            Assert.Equal(cookie.Value, currentCookie.Value);
        }
        finally
        {
            operations.Complete(id);
            Browser.SwitchTo().Window(streamingWindow);
            Browser.Close();
            if (streamingWindow != navigationWindow)
            {
                Browser.SwitchTo().Window(navigationWindow);
                Browser.Close();
            }
            Browser.SwitchTo().Window(formWindow);
            if (includeCrossSiteCookie)
            {
                Browser.Manage().Cookies.AddCookie(cookie);
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CrossSiteNavigationSupportsFormRenderedAfterAsynchronousWork(bool hasExistingCookie, bool enhance)
    {
        Navigate($"{ServerPathBase}/forms/default-form");
        Browser.Exists(By.Id("send"));
        var originalCookie = Assert.Single(Browser.Manage().Cookies.AllCookies,
            cookie => cookie.Name.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
        if (!hasExistingCookie)
        {
            Browser.Manage().Cookies.DeleteCookieNamed(originalCookie.Name);
        }
        var sourceWindow = Browser.CurrentWindowHandle;
        Browser.SwitchTo().NewWindow(WindowType.Tab);
        var navigationWindow = Browser.CurrentWindowHandle;
        var streamingWindow = navigationWindow;
        var operations = _serverFixture.Host.Services.GetRequiredService<AsyncOperationService>();
        var id = Guid.NewGuid().ToString("N");
        var started = operations.Start($"{id}-started");
        var responseStarted = operations.Start($"{id}-response-started");
        var acquisitionId = $"{id}-acquisition";
        var acquisitionStarted = operations.Start($"{acquisitionId}-started");

        try
        {
            streamingWindow = OpenCrossSiteLink(new Uri(_serverFixture.RootUri,
                $"{ServerPathBase}/forms/antiforgery-streaming?id={id}&renderForm=true&enhance={enhance}&tokenAcquisitionId={acquisitionId}"));
            await started.WaitAsync(TimeSpan.FromSeconds(30));
            await responseStarted.WaitAsync(TimeSpan.FromSeconds(10));
            Browser.SwitchTo().Window(streamingWindow);
            Browser.Equal("Waiting for release", () => Browser.Exists(By.Id("waiting")).Text);
            operations.Complete(id);
            Browser.SwitchTo().Window(streamingWindow);
            Browser.Equal("Released", () => Browser.Exists(By.Id("released")).Text);
            Browser.Equal("cross-site", () => Browser.Exists(By.Id("fetch-site")).Text);
            Browser.Equal("False", () => Browser.Exists(By.Id("received-cookie")).Text);
            await acquisitionStarted.WaitAsync(TimeSpan.FromSeconds(30));
            Browser.Exists(By.TagName("blazor-antiforgery"));
            Browser.DoesNotExist(By.Name("__RequestVerificationToken"));
            Browser.Exists(By.Id("send")).Click();
            Browser.Equal("Released", () => Browser.Exists(By.Id("released")).Text);
            Browser.DoesNotExist(By.Id("pass"));
            operations.Complete(acquisitionId);
            Browser.Equal("Form submitted!", () => Browser.Exists(By.Id("pass")).Text);
            if (hasExistingCookie)
            {
                Assert.Equal(originalCookie.Value, Browser.Manage().Cookies.GetCookieNamed(originalCookie.Name).Value);
                Browser.SwitchTo().Window(sourceWindow);
                Browser.Exists(By.Id("send")).Click();
                Browser.Equal("Form submitted!", () => Browser.Exists(By.Id("pass")).Text);
            }
        }
        finally
        {
            operations.Complete(id);
            operations.Complete(acquisitionId);
            if (streamingWindow != navigationWindow)
            {
                Browser.SwitchTo().Window(streamingWindow);
                Browser.Close();
            }
            Browser.SwitchTo().Window(navigationWindow);
            Browser.Close();
            Browser.SwitchTo().Window(sourceWindow);
        }
    }

    [Fact]
    public async Task LateFormCanAcquireTokenOnOutputCachedPage()
    {
        Navigate($"{ServerPathBase}/forms/default-form");
        foreach (var cookie in Browser.Manage().Cookies.AllCookies
            .Where(cookie => cookie.Name.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)))
        {
            Browser.Manage().Cookies.DeleteCookieNamed(cookie.Name);
        }
        var operations = _serverFixture.Host.Services.GetRequiredService<AsyncOperationService>();
        var id = Guid.NewGuid().ToString("N");
        var started = operations.Start($"{id}-started");

        try
        {
            Navigate($"{ServerPathBase}/forms/antiforgery-streaming?id={id}&renderForm=true&cache=true");
            await started.WaitAsync(TimeSpan.FromSeconds(30));
            Browser.Equal("Waiting for release", () => Browser.Exists(By.Id("waiting")).Text);
            operations.Complete(id);
            Browser.Equal("Released", () => Browser.Exists(By.Id("released")).Text);
            Browser.True(() => !string.IsNullOrEmpty(Browser.Exists(By.Name("__RequestVerificationToken")).GetDomProperty("value")));
            Browser.Exists(By.Id("send")).Click();
            Browser.Equal("Form submitted!", () => Browser.Exists(By.Id("pass")).Text);
        }
        finally
        {
            operations.Complete(id);
        }
    }

    [Fact]
    public async Task EnhancedNavigationCancelsPendingTokenAcquisition()
    {
        EnhancedNavigationTestUtil.CleanEnhancedNavigationSuppression(this);
        Navigate($"{ServerPathBase}/forms/default-form");
        foreach (var cookie in Browser.Manage().Cookies.AllCookies
            .Where(cookie => cookie.Name.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)))
        {
            Browser.Manage().Cookies.DeleteCookieNamed(cookie.Name);
        }
        var operations = _serverFixture.Host.Services.GetRequiredService<AsyncOperationService>();
        var id = Guid.NewGuid().ToString("N");
        var started = operations.Start($"{id}-started");
        var acquisitionId = $"{id}-acquisition";
        var acquisitionStarted = operations.Start($"{acquisitionId}-started");

        try
        {
            Navigate($"{ServerPathBase}/forms/antiforgery-streaming?id={id}&renderForm=true&tokenAcquisitionId={acquisitionId}");
            await started.WaitAsync(TimeSpan.FromSeconds(30));
            Browser.Equal("Waiting for release", () => Browser.Exists(By.Id("waiting")).Text);
            operations.Complete(id);
            await acquisitionStarted.WaitAsync(TimeSpan.FromSeconds(30));
            // The gate signals inline; let the request resume before synchronous WebDriver waits for its completion.
            await Task.Yield();
            Browser.Exists(By.Id("early-form-link")).Click();
            Browser.Exists(By.Name("__RequestVerificationToken"));
            var cookie = Assert.Single(Browser.Manage().Cookies.AllCookies,
                cookie => cookie.Name.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));

            operations.Complete(acquisitionId);
            // WebDriver has no network-completion event; Resource Timing observes completion or cancellation of the real fetch.
            Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
                "return performance.getEntriesByType('resource').some(entry => entry.initiatorType === 'fetch' && entry.name.includes(arguments[0]))",
                $"tokenAcquisitionId={acquisitionId}"));
            Assert.Equal(cookie.Value, Browser.Manage().Cookies.GetCookieNamed(cookie.Name).Value);
            Browser.Exists(By.Id("send")).Click();
            Browser.Equal("Form submitted!", () => Browser.Exists(By.Id("pass")).Text);
        }
        finally
        {
            operations.Complete(id);
            operations.Complete(acquisitionId);
        }
    }

    private string OpenCrossSiteLink(Uri destination)
    {
        var source = new UriBuilder(_serverFixture.RootUri)
        {
            Host = _serverFixture.RootUri.Host == "localhost" ? "127.0.0.1" : "localhost",
            Path = $"{ServerPathBase}/forms/antiforgery-streaming",
            Query = $"destination={Uri.EscapeDataString(destination.AbsoluteUri)}",
        };
        Browser.Navigate().GoToUrl(source.Uri);
        Browser.Exists(By.Id("cross-site-link"));
        var window = Browser.CurrentWindowHandle;
        // WebDriver can block on a pending navigation before response headers arrive, even with PageLoadStrategy.None.
        ((IJavaScriptExecutor)Browser).ExecuteScript("setTimeout(() => document.getElementById('cross-site-link').click(), 0)");

        return window;
    }
}
