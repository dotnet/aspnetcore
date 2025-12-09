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

public class ServerInteropTestJsInvocationsTimeoutsBehavior : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public ServerInteropTestJsInvocationsTimeoutsBehavior(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<LongRunningInterop>();
    }

    [Fact]
    public async Task LongRunningJavaScriptFunctionsResultInCancellationAndWorkingAppAfterFunctionCompletion()
    {
        // Act & Assert
        var interopButton = Browser.Exists(By.Id("btn-interop"));
        interopButton.Click();

        Browser.Exists(By.Id("done-with-interop"));

        Browser.Exists(By.Id("task-was-cancelled"));

        // wait 10 seconds, js method completes in 5 seconds, after this point it would have triggered a completion for sure.
        await Task.Delay(10000);

        var circuitFunctional = Browser.Exists(By.Id("circuit-functional"));
        circuitFunctional.Click();

        Browser.Exists(By.Id("done-circuit-functional"));
    }
}
