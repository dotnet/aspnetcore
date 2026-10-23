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

    private async Task VerifyExistingFormAfterStreamingNavigation(bool crossSite)
    {
        EnhancedNavigationTestUtil.SuppressEnhancedNavigation(this, shouldSuppress: true);
        Navigate($"{ServerPathBase}/forms/default-form");
        var formWindow = Browser.CurrentWindowHandle;
        var token = Browser.Exists(By.Name("__RequestVerificationToken")).GetDomProperty("value");
        var cookie = Assert.Single(Browser.Manage().Cookies.AllCookies,
            cookie => cookie.Name.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
        var operations = _serverFixture.Host.Services.GetRequiredService<AsyncOperationService>();
        var id = Guid.NewGuid().ToString("N");
        var started = operations.Start($"{id}-started");
        var destination = new Uri(_serverFixture.RootUri, $"{ServerPathBase}/forms/antiforgery-streaming?id={id}");
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
            Browser.SwitchTo().Window(streamingWindow);
            if (!crossSite)
            {
                Browser.Equal("Waiting for release", () => Browser.Exists(By.Id("waiting")).Text);
            }
            operations.Complete(id);
            Browser.Equal("Released", () => Browser.Exists(By.Id("released")).Text);
            Browser.Equal(crossSite ? "cross-site" : "none", () => Browser.Exists(By.Id("fetch-site")).Text);
            Browser.Equal(crossSite ? "False" : "True", () => Browser.Exists(By.Id("received-cookie")).Text);
            var currentCookie = Browser.Manage().Cookies.GetCookieNamed(cookie.Name);

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
        }
    }

    [Fact]
    public async Task CrossSiteNavigationSupportsFormRenderedAfterAsynchronousWork()
    {
        Navigate($"{ServerPathBase}/forms/default-form");
        Browser.Exists(By.Id("send"));
        foreach (var cookie in Browser.Manage().Cookies.AllCookies
            .Where(cookie => cookie.Name.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)))
        {
            Browser.Manage().Cookies.DeleteCookieNamed(cookie.Name);
        }
        var sourceWindow = Browser.CurrentWindowHandle;
        var streamingWindow = sourceWindow;
        var operations = _serverFixture.Host.Services.GetRequiredService<AsyncOperationService>();
        var id = Guid.NewGuid().ToString("N");
        var started = operations.Start($"{id}-started");

        try
        {
            streamingWindow = OpenCrossSiteLink(new Uri(_serverFixture.RootUri,
                $"{ServerPathBase}/forms/antiforgery-streaming?id={id}&renderForm=true"));
            await started.WaitAsync(TimeSpan.FromSeconds(30));
            operations.Complete(id);
            Browser.SwitchTo().Window(streamingWindow);
            Browser.Equal("Released", () => Browser.Exists(By.Id("released")).Text);
            Browser.Equal("cross-site", () => Browser.Exists(By.Id("fetch-site")).Text);
            Browser.Equal("False", () => Browser.Exists(By.Id("received-cookie")).Text);
            Assert.NotEmpty(Browser.Exists(By.Name("__RequestVerificationToken")).GetDomProperty("value"));
            Browser.Exists(By.Id("send")).Click();
            Browser.Equal("Form submitted!", () => Browser.Exists(By.Id("pass")).Text);
        }
        finally
        {
            operations.Complete(id);
            if (streamingWindow != sourceWindow)
            {
                Browser.SwitchTo().Window(streamingWindow);
                Browser.Close();
            }
            Browser.SwitchTo().Window(sourceWindow);
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
        var windows = Browser.WindowHandles.ToHashSet();
        Browser.Exists(By.Id("cross-site-link")).Click();
        Browser.True(() => Browser.WindowHandles.Count == windows.Count + 1);

        return Browser.WindowHandles.Single(window => !windows.Contains(window));
    }
}
