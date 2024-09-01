// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class NavigationLockPrerenderingTest : ServerTestBase<BasicTestAppServerSiteFixture<LockedNavigationStartup>>
{
    public NavigationLockPrerenderingTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<LockedNavigationStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.RoutingTestContext);

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/57153")]
    public void NavigationIsLockedAfterPrerendering()
    {
        Navigate("/locked-navigation");

        // Assert that the component rendered successfully
        Browser.Equal("Prevented navigations: 0", () => Browser.FindElement(By.Id("num-prevented-navigations")).Text);

        BeginInteractivity();

        // Assert that internal navigations are blocked
        Browser.Click(By.Id("internal-navigation-link"));
        Browser.Equal("Prevented navigations: 1", () => Browser.FindElement(By.Id("num-prevented-navigations")).Text);

        // Assert that external navigations are blocked
        Browser.Navigate().GoToUrl("about:blank");
        Browser.SwitchTo().Alert().Dismiss();
        Browser.Equal("Prevented navigations: 1", () => Browser.FindElement(By.Id("num-prevented-navigations")).Text);
    }

    private void BeginInteractivity()
    {
        Browser.Exists(By.Id("load-boot-script")).Click();

        var javascript = (IJavaScriptExecutor)Browser;
        Browser.True(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__blazor__started__'] === true;"));
    }
}
