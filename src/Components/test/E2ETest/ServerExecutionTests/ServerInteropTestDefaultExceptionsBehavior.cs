// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerInteropTestDefaultExceptionsBehavior : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public ServerInteropTestDefaultExceptionsBehavior(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<InteropComponent>();
    }

    [Fact]
    public void DotNetExceptionDetailsAreNotLoggedByDefault()
    {
        // Arrange
        var expectedValues = new Dictionary<string, string>
        {
            ["AsyncThrowSyncException"] = GetExpectedMessage("AsyncThrowSyncException"),
            ["AsyncThrowAsyncException"] = GetExpectedMessage("AsyncThrowAsyncException"),
        };

        var actualValues = new Dictionary<string, string>();

        // Act
        var interopButton = Browser.Exists(By.Id("btn-interop"));
        interopButton.Click();

        Browser.Exists(By.Id("done-with-interop"));

        foreach (var expectedValue in expectedValues)
        {
            var currentValue = Browser.Exists(By.Id(expectedValue.Key));
            actualValues.Add(expectedValue.Key, currentValue.Text);
        }

        // Assert
        foreach (var expectedValue in expectedValues)
        {
            Assert.Equal(expectedValue.Value, actualValues[expectedValue.Key]);
        }

        string GetExpectedMessage(string method) =>
            "\"There was an exception invoking '" + method + "' on assembly 'BasicTestApp'. For more details turn on " +
            "detailed exceptions in '" + typeof(CircuitOptions).Name + "." + nameof(CircuitOptions.DetailedErrors) + "'\"";
    }
}
