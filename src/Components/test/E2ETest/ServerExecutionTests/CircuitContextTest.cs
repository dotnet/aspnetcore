// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests;

public class CircuitContextTest : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public CircuitContextTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<CircuitContextComponent>();
        Browser.Equal("Circuit Context", () => Browser.Exists(By.TagName("h1")).Text);
    }

    [Fact]
    public void ComponentMethods_HaveCircuitContext()
    {
        Browser.Click(By.Id("trigger-click-event-button"));

        Browser.True(() => HasCircuitContext("SetParametersAsync"));
        Browser.True(() => HasCircuitContext("OnInitializedAsync"));
        Browser.True(() => HasCircuitContext("OnParametersSetAsync"));
        Browser.True(() => HasCircuitContext("OnAfterRenderAsync"));
        Browser.True(() => HasCircuitContext("InvokeDotNet"));
        Browser.True(() => HasCircuitContext("OnClickEvent"));

        bool HasCircuitContext(string eventName)
        {
            var resultText = Browser.FindElement(By.Id($"circuit-context-result-{eventName}")).Text;
            var result = bool.Parse(resultText);
            return result;
        }
    }
}
