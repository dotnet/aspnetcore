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
}
