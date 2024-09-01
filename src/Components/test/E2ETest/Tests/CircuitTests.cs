// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class CircuitTests : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public CircuitTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
    }

    [Theory]
    [InlineData("constructor-throw")]
    [InlineData("attach-throw")]
    [InlineData("setparameters-sync-throw")]
    [InlineData("setparameters-async-throw")]
    [InlineData("render-throw")]
    [InlineData("afterrender-sync-throw")]
    [InlineData("afterrender-async-throw")]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/57588")]
    public void ComponentLifecycleMethodThrowsExceptionTerminatesTheCircuit(string id)
    {
        Browser.MountTestComponent<ReliabilityComponent>();
        Browser.Exists(By.Id("thecounter"));

        var targetButton = Browser.Exists(By.Id(id));
        targetButton.Click();

        // Triggering an error will show the exception UI
        Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"));

        // Clicking the button again will trigger a server disconnect
        targetButton.Click();

        AssertLogContains("Connection disconnected.");
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/57588")]
    public void ComponentDisposeMethodThrowsExceptionTerminatesTheCircuit()
    {
        Browser.MountTestComponent<ReliabilityComponent>();
        Browser.Exists(By.Id("thecounter"));

        // Arrange
        var targetButton = Browser.Exists(By.Id("dispose-throw"));

        // Clicking the button sets a boolean that renders the component
        targetButton.Click();
        // Clicking it again hides the component and invokes the rethrow which triggers the exception
        targetButton.Click();
        Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"));

        // Clicking it again causes the circuit to disconnect
        targetButton.Click();
        AssertLogContains("Connection disconnected.");
    }

    [Fact]
    public void OnLocationChanged_ReportsErrorForExceptionInUserCode()
    {
        Browser.MountTestComponent<NavigationFailureComponent>();
        var targetButton = Browser.Exists(By.Id("navigate-to-page"));

        targetButton.Click();
        Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"));

        var expectedError = "Location change failed";
        AssertLogContains(expectedError);
    }

    void AssertLogContains(params string[] messages)
    {
        var log = Browser.Manage().Logs.GetLog(LogType.Browser);
        foreach (var message in messages)
        {
            Assert.Contains(log, entry => entry.Message.Contains(message));
        }
    }
}
