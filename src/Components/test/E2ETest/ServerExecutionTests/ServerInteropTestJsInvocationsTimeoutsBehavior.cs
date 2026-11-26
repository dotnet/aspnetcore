// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        serverFixture.UpdateHostServices = services =>
        {
            var options = services.GetRequiredService<IOptions<CircuitOptions>>().Value;
            options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(5);
            options.DetailedErrors = true;
        };
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<LongRunningInterop>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InteropTimeoutIsReportedAsUnhandledException(bool useDefaultTimeout)
    {
        Browser.Exists(By.Id(useDefaultTimeout ? "default-timeout" : "explicit-timeout")).Click();

        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript("return window.neverCompletingInteropStarted === true;"));
        Browser.Equal("block", () => Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display"));
        Browser.True(() => Browser.Manage().Logs.GetLog(LogType.Browser).Any(entry =>
            entry.Message.Contains("JSException: The JS interop call 'neverCompletingInterop' timed out after")));
    }

    [Fact]
    public void ExplicitCancellationDoesNotReportUnhandledException()
    {
        Browser.Exists(By.Id("uncaught-cancellation")).Click();
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript("return window.neverCompletingInteropStarted === true;"));
        Browser.Exists(By.Id("observe-cancellation")).Click();
        Browser.Exists(By.Id("task-was-cancelled"));

        Browser.Exists(By.Id("circuit-functional")).Click();

        Browser.Exists(By.Id("done-circuit-functional"));
        Browser.Equal("none", () => Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display"));
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
