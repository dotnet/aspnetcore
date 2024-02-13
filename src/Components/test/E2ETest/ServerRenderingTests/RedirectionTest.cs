// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class RedirectionTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    private IWebElement _originalH1Element;

    public RedirectionTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Navigate($"{ServerPathBase}/redirect");

        _originalH1Element = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Redirections", () => _originalH1Element.Text);
    }

    [Fact]
    public void RedirectStreamingGetToInternal()
    {
        Browser.Exists(By.LinkText("Streaming GET with internal redirection")).Click();
        AssertElementRemoved(_originalH1Element);
        Browser.Equal("Scroll to hash", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.True(() => Browser.GetScrollY() > 500);
        Assert.EndsWith("/subdir/nav/scroll-to-hash?foo=%F0%9F%99%82#some-content", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Redirections", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith("/subdir/redirect", Browser.Url);
    }

    [Fact]
    public void RedirectStreamingGetToExternal()
    {
        Browser.Exists(By.LinkText("Streaming GET with external redirection")).Click();
        Browser.Contains("microsoft.com", () => Browser.Url);
    }

    [Fact]
    public void RedirectStreamingPostToInternal()
    {
        Browser.Exists(By.CssSelector("#form-streaming-internal button")).Click();
        AssertElementRemoved(_originalH1Element);
        Browser.Equal("Scroll to hash", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.True(() => Browser.GetScrollY() > 500);
        Assert.EndsWith("/subdir/nav/scroll-to-hash?foo=%F0%9F%99%82#some-content", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Redirections", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith("/subdir/redirect", Browser.Url);
    }

    [Fact]
    public void RedirectStreamingPostToExternal()
    {
        Browser.Exists(By.CssSelector("#form-streaming-external button")).Click();
        Browser.Contains("microsoft.com", () => Browser.Url);
    }

    [Fact]
    public void RedirectEnhancedGetToInternal()
    {
        // Note that for enhanced nav we can't preserve the hash part of the URL, as it
        // gets discarded when the browser follows a 'fetch' redirection. This is not solvable
        // unless we are willing to make the server return extra information so that the
        // 'fetch' response does disclose the redirection hash to JS. As it stands, people
        // who need to redirect to a URL with a hash need not to do an enhanced nav to it.

        Browser.Exists(By.LinkText("Enhanced GET with internal redirection")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash?foo=%F0%9F%99%82", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Redirections", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/redirect", Browser.Url);
    }

    [Fact]
    public void RedirectEnhancedGetToExternal()
    {
        Browser.Exists(By.LinkText("Enhanced GET with external redirection")).Click();
        Browser.Contains("microsoft.com", () => Browser.Url);
    }

    [Fact]
    public void RedirectEnhancedPostToInternal()
    {
        // See above for why enhanced nav doesn't support preserving the hash
        Browser.Exists(By.CssSelector("#form-enhanced-internal button")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash?foo=%F0%9F%99%82", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Redirections", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/redirect", Browser.Url);
    }

    [Fact]
    public void RedirectEnhancedPostToExternal()
    {
        Browser.Exists(By.CssSelector("#form-enhanced-external button")).Click();
        Browser.Contains("microsoft.com", () => Browser.Url);
    }

    [Fact]
    public void RedirectStreamingEnhancedGetToInternal()
    {
        // See above for why enhanced nav doesn't support preserving the hash
        Browser.Exists(By.LinkText("Streaming enhanced GET with internal redirection")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash?foo=%F0%9F%99%82", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Redirections", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/redirect", Browser.Url);
    }

    [Fact]
    public void RedirectStreamingEnhancedGetToExternal()
    {
        Browser.Exists(By.LinkText("Streaming enhanced GET with external redirection")).Click();
        Browser.Contains("microsoft.com", () => Browser.Url);
    }

    [Fact]
    public void RedirectStreamingEnhancedPostToInternal()
    {
        // See above for why enhanced nav doesn't support preserving the hash
        Browser.Exists(By.CssSelector("#form-streaming-enhanced-internal button")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash?foo=%F0%9F%99%82", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Redirections", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/redirect", Browser.Url);
    }

    [Fact]
    public void RedirectStreamingEnhancedPostToExternal()
    {
        Browser.Exists(By.CssSelector("#form-streaming-enhanced-external button")).Click();
        Browser.Contains("microsoft.com", () => Browser.Url);
    }

    [Fact]
    public void RedirectEnhancedNonBlazorGetToInternal()
    {
        // See above for why enhanced nav doesn't support preserving the hash
        Browser.Exists(By.LinkText("Enhanced GET to non-Blazor endpoint with internal redirection")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Redirections", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/redirect", Browser.Url);
    }

    [Fact]
    public void RedirectEnhancedNonBlazorGetToExternal()
    {
        Browser.Exists(By.LinkText("Enhanced GET to non-Blazor endpoint with external redirection")).Click();
        Browser.Contains("microsoft.com", () => Browser.Url);
    }

    [Fact]
    public void RedirectEnhancedNonBlazorPostToInternal()
    {
        // See above for why enhanced nav doesn't support preserving the hash
        Browser.Exists(By.CssSelector("#form-nonblazor-enhanced-internal button")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Redirections", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/redirect", Browser.Url);
    }

    // There's no RedirectEnhancedNonBlazorPostToExternal case as it's explicitly unsupported
    // We would need server-side middleware that runs on *all* requests to convert the 301/302/etc
    // response to something like a 200 that the 'fetch' is allowed to read (embedding the
    // destination URL).

    [Fact]
    public void RedirectEnhancedGetToInternalWithErrorBoundary()
    {
        // This test verifies that redirection works even if an ErrorBoundary wraps
        // a component throwing a NavigationException.

        Browser.Exists(By.LinkText("Enhanced GET with redirect inside error boundary")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash?foo=%F0%9F%99%82", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Redirections", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/redirect", Browser.Url);
    }

    private void AssertElementRemoved(IWebElement element)
    {
        Browser.True(() =>
        {
            try
            {
                element.GetDomProperty("tagName");
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }

            return false;
        });
    }
}
