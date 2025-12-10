// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;
using OpenQA.Selenium;
using TestServer;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class TempDataTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public TempDataTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void TempDataCanPersistThroughNavigation()
    {
        Navigate($"{ServerPathBase}/tempdata");
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);

        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.FindElement(By.Id("navigate-to-the-same-page")).Click();

        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
    }

    //TO-DO: Add test for Peek()
    //TO-DO: Add test for Keep()
    //TO-DO: Add test for Keep(string)
}
