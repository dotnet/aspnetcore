// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.E2ETesting;
using TestServer;
using Xunit.Abstractions;
using Components.TestServer.RazorComponents;
using OpenQA.Selenium;

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
