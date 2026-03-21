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

    [Fact]
    public void ComponentMethods_HaveCircuitContext()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<CircuitContextComponent>();
        TestCircuitContextCore(Browser);
    }

    [Fact]
    public void ComponentMethods_HaveCircuitContext_OnInitialPageLoad()
    {
        // https://github.com/dotnet/aspnetcore/issues/57481
        Navigate($"{ServerPathBase}?initial-component-type={typeof(CircuitContextComponent).AssemblyQualifiedName}");
        TestCircuitContextCore(Browser);
    }

    // Internal for reuse in Blazor Web tests
    internal static void TestCircuitContextCore(IWebDriver browser)
    {
        browser.Equal("Circuit Context", () => browser.Exists(By.Id("circuit-context-title")).Text);

        browser.Click(By.Id("trigger-click-event-button"));

        browser.True(() => HasCircuitContext("SetParametersAsync"));
        browser.True(() => HasCircuitContext("OnInitializedAsync"));
        browser.True(() => HasCircuitContext("OnParametersSetAsync"));
        browser.True(() => HasCircuitContext("OnAfterRenderAsync"));
        browser.True(() => HasCircuitContext("InvokeDotNet"));
        browser.True(() => HasCircuitContext("OnClickEvent"));

        bool HasCircuitContext(string eventName)
        {
            var resultText = browser.FindElement(By.Id($"circuit-context-result-{eventName}")).Text;
            var result = bool.Parse(resultText);
            return result;
        }
    }
}
