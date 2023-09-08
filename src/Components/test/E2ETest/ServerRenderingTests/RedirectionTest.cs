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
        Assert.EndsWith("/subdir/nav/scroll-to-hash#some-content", Browser.Url);

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
        Assert.EndsWith("/subdir/nav/scroll-to-hash#some-content", Browser.Url);

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
        // Note that in this specific case we can't preserve the hash part of the URL, as it
        // gets lost when the browser follows a 'fetch' redirection. If we decide it's important
        // to support this later, we'd have to change the server not to do a real redirection
        // here and instead use the same protocol it uses for external redirections.

        Browser.Exists(By.LinkText("Enhanced GET with internal redirection")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash", Browser.Url);

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
        // Note that in this specific case we can't preserve the hash part of the URL, as it
        // gets lost when the browser follows a 'fetch' redirection. If we decide it's important
        // to support this later, we'd have to change the server not to do a real redirection
        // here and instead use the same protocol it uses for external redirections.

        Browser.Exists(By.CssSelector("#form-enhanced-internal button")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash", Browser.Url);

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
        // Because this is enhanced nav, it doesn't support preserving the hash in the
        // redirection for the same reason as above

        Browser.Exists(By.LinkText("Streaming enhanced GET with internal redirection")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash", Browser.Url);

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
        // Because this is enhanced nav, it doesn't support preserving the hash in the
        // redirection for the same reason as above

        Browser.Exists(By.CssSelector("#form-streaming-enhanced-internal button")).Click();
        Browser.Equal("Scroll to hash", () => _originalH1Element.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash", Browser.Url);

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
