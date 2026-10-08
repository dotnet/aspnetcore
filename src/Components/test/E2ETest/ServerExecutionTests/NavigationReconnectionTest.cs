// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.JSInterop;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class NavigationReconnectionTest : ServerTestBase<BasicTestAppServerSiteFixture<NavigationReconnectionStartup>>
{
    public NavigationReconnectionTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<NavigationReconnectionStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate($"{ServerPathBase}?navigationReconnection&initial-component-type={typeof(NavigationReconnectionComponent).AssemblyQualifiedName}");
        Browser.Equal("1", () => Browser.Exists(By.Id("navigation-connection-up-count")).Text);
    }

    [Fact]
    public async Task NavigationIssuedWhileDisconnected_TimesOutWithoutTerminatingReconnectedCircuit()
    {
        var circuitId = Browser.Exists(By.Id("navigation-circuit-id")).Text;
        Assert.NotEmpty(circuitId);
        var targetUri = Browser.Exists(By.Id("navigation-target")).Text;
        var originalUrl = Browser.Url;
        using var navigationLog = new NavigationLog(_serverFixture.Host.Services.GetRequiredService<TestSink>(), targetUri);
        var javascript = (IJavaScriptExecutor)Browser;

        Browser.Exists(By.Id("navigation-increment")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("navigation-count")).Text);
        Browser.Exists(By.Id("navigation-after-disconnect")).Click();
        Browser.Equal("True", () => Browser.Exists(By.Id("navigation-pending")).Text);

        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");
        Browser.True(() => Equals(true, javascript.ExecuteScript("return window.navigationReconnectionDisconnected;")));

        // The real connection-down callback releases the component's await. SignalR drops its navigation call.
        await navigationLog.Issued.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.False(navigationLog.Outcome.IsCompleted, "Navigation must still be pending when reconnection starts.");

        javascript.ExecuteScript("""
            window.navigationReconnectionResult = undefined;
            Blazor.reconnect().then(
                result => { window.navigationReconnectionResult = result; },
                error => { window.navigationReconnectionResult = String(error); });
            """);
        Browser.True(() => javascript.ExecuteScript("return window.navigationReconnectionResult;") is not null);
        Assert.True(Assert.IsType<bool>(javascript.ExecuteScript("return window.navigationReconnectionResult;")));

        Browser.Exists(By.Id("navigation-increment")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("navigation-count")).Text);
        Browser.Equal("2", () => Browser.Exists(By.Id("navigation-connection-up-count")).Text);
        Assert.Equal(circuitId, Browser.Exists(By.Id("navigation-circuit-id")).Text);
        Assert.False(navigationLog.Outcome.IsCompleted, "The circuit must reconnect before the navigation timeout.");

        var outcome = await navigationLog.Outcome.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(8, outcome.EventId.Id);
        Assert.Equal("NavigationTimedOut", outcome.EventId.Name);
        Assert.Equal(LogLevel.Warning, outcome.LogLevel);
        Assert.IsType<TaskCanceledException>(outcome.Exception);

        // A server round-trip after the timeout also exercises CircuitHost and the circuit registry.
        Browser.Exists(By.Id("navigation-increment")).Click();
        Browser.Equal("3", () => Browser.Exists(By.Id("navigation-count")).Text);
        Assert.Equal(circuitId, Browser.Exists(By.Id("navigation-circuit-id")).Text);
        Assert.Equal(originalUrl, Browser.Url);
        Browser.False(() => Browser.Exists(By.Id("blazor-error-ui")).Displayed);
    }

    [Fact]
    public async Task UnexpectedNavigationInteropFailure_ShowsErrorAndDisconnectsCircuit()
    {
        var targetUri = Browser.Exists(By.Id("navigation-target")).Text;
        using var navigationLog = new NavigationLog(_serverFixture.Host.Services.GetRequiredService<TestSink>(), targetUri);
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("""
            window.originalNavigationReconnectionNavigateTo = Blazor._internal.navigationManager.navigateTo;
            Blazor._internal.navigationManager.navigateTo = () => {
                throw new Error('Unexpected navigation interop failure.');
            };
            """);

        try
        {
            Browser.Exists(By.Id("navigation-now")).Click();

            var outcome = await navigationLog.Outcome.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(4, outcome.EventId.Id);
            Assert.Equal("NavigationFailed", outcome.EventId.Name);
            Assert.Equal(LogLevel.Error, outcome.LogLevel);
            Assert.Contains("Unexpected navigation interop failure.", Assert.IsType<JSException>(outcome.Exception).Message);
            Browser.True(() => Browser.Exists(By.Id("blazor-error-ui")).Displayed);
            Browser.True(() => Browser.Manage().Logs.GetLog(LogType.Browser)
                .Any(entry => entry.Message.Contains("Connection disconnected.", StringComparison.Ordinal)));
        }
        finally
        {
            javascript.ExecuteScript("""
                Blazor._internal.navigationManager.navigateTo = window.originalNavigationReconnectionNavigateTo;
                delete window.originalNavigationReconnectionNavigateTo;
                """);
        }
    }

    private sealed class NavigationLog : IDisposable
    {
        private readonly TestSink _sink;
        private readonly string _targetUri;
        private readonly TaskCompletionSource _issued = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<WriteContext> _outcome = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public NavigationLog(TestSink sink, string targetUri)
        {
            _sink = sink;
            _targetUri = targetUri;
            _sink.MessageLogged += OnMessageLogged;
        }

        public Task Issued => _issued.Task;

        public Task<WriteContext> Outcome => _outcome.Task;

        private void OnMessageLogged(WriteContext context)
        {
            if (!context.Message.Contains(_targetUri, StringComparison.Ordinal))
            {
                return;
            }

            if (context.EventId.Name is "NavigationIssued")
            {
                _issued.TrySetResult();
            }
            else if (context.EventId.Name is "NavigationTimedOut" or "NavigationFailed" or "NavigationCompleted")
            {
                _outcome.TrySetResult(context);
            }
        }

        public void Dispose()
        {
            _sink.MessageLogged -= OnMessageLogged;
        }
    }
}
