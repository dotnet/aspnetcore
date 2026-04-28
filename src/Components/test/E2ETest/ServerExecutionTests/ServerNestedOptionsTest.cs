// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerNestedOptionsTest : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public ServerNestedOptionsTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate($"{ServerPathBase}/nestedCircuitOptions");
    }

    [Fact]
    public void NestedCircuitOptionsAreAccepted()
    {
        var appElement = Browser.MountTestComponent<CounterComponent>();
        var countDisplayElement = appElement.FindElement(By.TagName("p"));
        Browser.Equal("Current count: 0", () => countDisplayElement.Text);

        appElement.FindElement(By.TagName("button")).Click();
        Browser.Equal("Current count: 1", () => countDisplayElement.Text);
    }
}
