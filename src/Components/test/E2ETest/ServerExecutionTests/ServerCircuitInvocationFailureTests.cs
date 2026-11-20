// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerCircuitInvocationFailureTests : ServerTestBase<ServerCircuitInvocationFailureTests.ServerFixture>,
    IClassFixture<ServerCircuitInvocationFailureTests.ResumeServerFixture>
{
    private readonly ResumeServerFixture _resumeServerFixture;

    public ServerCircuitInvocationFailureTests(
        BrowserFixture browserFixture,
        ServerFixture serverFixture,
        ResumeServerFixture resumeServerFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        _resumeServerFixture = resumeServerFixture;
    }

    protected override void InitializeAsyncCore()
    {
        Browser.GetBrowserLogs(LogLevel.All);
    }

    [Theory]
    [InlineData("ConnectCircuit", true)]
    [InlineData("ResumeCircuit", true)]
    [InlineData("ConnectCircuit", false)]
    [InlineData("ResumeCircuit", false)]
    public async Task ReconnectUI_ClearsAfterRetryingFailedCircuitInvocation(string hubMethodName, bool automaticRetry)
    {
        var serverFixture = hubMethodName == "ResumeCircuit" ? _resumeServerFixture : _serverFixture;
        var query = automaticRetry ? string.Empty : "?single-reconnect-attempt=true";
        Browser.Navigate(serverFixture.RootUri, $"{ServerPathBase}/persistent-state/disconnection{query}");
        Browser.Exists(By.Id("render-mode-interactive"));

        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Equal("5", () => Browser.Exists(By.Id("non-persisted-counter")).Text);
        Browser.Exists(By.Id("increment-non-persisted-counter")).Click();
        Browser.Equal("6", () => Browser.Exists(By.Id("non-persisted-counter")).Text);

        var persistenceGate = serverFixture.Host.Services.GetRequiredService<CircuitPersistenceTestGate>();
        persistenceGate.Reset();
        var failure = new CircuitInvocationFailureFilter(hubMethodName, persistenceGate);
        serverFixture.InvocationFilter.CurrentFailure = failure;

        try
        {
            var javascript = (IJavaScriptExecutor)Browser;
            javascript.ExecuteScript("window.reconnectionTestDocument = document; Blazor._internal.forceCloseConnection()");

            Browser.True(() => failure.InvocationFailed.Task.IsCompletedSuccessfully);
            Browser.True(() => failure.ConnectionClosed.Task.IsCompletedSuccessfully);
            Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

            if (!automaticRetry)
            {
                // WebDriver can reach the default dialog only through its shadow root.
                Browser.Exists(() => Browser.Exists(By.Id("components-reconnect-modal"))
                    .GetShadowRoot()
                    .FindElements(By.CssSelector("button"))
                    .Single(button => button.Displayed && button.Text == "Retry"), timeout: default).Click();
            }

            Browser.True(() => failure.RetryStarted.Task.IsCompletedSuccessfully);
            Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
            failure.AllowRetry.SetResult();

            Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
            Browser.True(() => (bool)javascript.ExecuteScript("return window.reconnectionTestDocument === document"));
            Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
            var expectedNonPersistedCount = hubMethodName == "ConnectCircuit" ? "6" : "0";
            Browser.Equal(expectedNonPersistedCount, () => Browser.Exists(By.Id("non-persisted-counter")).Text);
            Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
            Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
            Browser.True(() => failure.RetriedInvocation.Task.IsCompletedSuccessfully);
            Assert.NotEqual(await failure.InvocationFailed.Task, await failure.RetriedInvocation.Task);

            persistenceGate.Reset();
            javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

            Browser.True(() => failure.SubsequentInvocation.Task.IsCompletedSuccessfully);
            Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
            Browser.Equal(expectedNonPersistedCount, () => Browser.Exists(By.Id("non-persisted-counter")).Text);
            Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
            Browser.Equal("3", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
            Browser.True(() => (bool)javascript.ExecuteScript("return window.reconnectionTestDocument === document"));
            Assert.DoesNotContain(Browser.GetBrowserLogs(LogLevel.Severe),
                entry => !entry.Message.Contains($"Intentional {hubMethodName} failure.", StringComparison.Ordinal));
        }
        finally
        {
            failure.AllowRetry.TrySetResult();
        }
    }

    public class ServerFixture : BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>
    {
        internal InvocationFilter InvocationFilter { get; } = new();

        public ServerFixture()
        {
            AdditionalArguments.AddRange(["--EnableCircuitPersistenceTestGate", "true"]);
            UpdateHostServices = services =>
                services.GetRequiredService<IOptions<HubOptions>>().Value.AddFilter(InvocationFilter);
        }
    }

    public sealed class ResumeServerFixture : ServerFixture
    {
        public ResumeServerFixture()
        {
            AdditionalArguments.AddRange(["--DisableReconnectionCache", "true"]);
        }
    }

    internal sealed class InvocationFilter : IHubFilter
    {
        public CircuitInvocationFailureFilter CurrentFailure { get; set; }

        public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
            => CurrentFailure is { } failure ? failure.InvokeMethodAsync(invocationContext, next) : next(invocationContext);

        public Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
            => CurrentFailure is { } failure ? failure.OnDisconnectedAsync(context, exception, next) : next(context, exception);
    }

    internal sealed class CircuitInvocationFailureFilter(string hubMethodName, CircuitPersistenceTestGate persistenceGate) : IHubFilter
    {
        private int _attempts;
        private string _failedConnectionId;

        public TaskCompletionSource<string> InvocationFailed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ConnectionClosed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RetryStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowRetry { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<string> RetriedInvocation { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SubsequentInvocation { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            if (invocationContext.HubMethodName == "ConnectCircuit")
            {
                var circuitReady = hubMethodName == "ResumeCircuit" ? persistenceGate.CircuitClosed : persistenceGate.ConnectionDown;
                await circuitReady.WaitAsync(invocationContext.Context.ConnectionAborted);
            }

            if (invocationContext.HubMethodName != hubMethodName)
            {
                return await next(invocationContext);
            }

            var attempt = Interlocked.Increment(ref _attempts);
            if (attempt == 1)
            {
                _failedConnectionId = invocationContext.Context.ConnectionId;
                InvocationFailed.SetResult(_failedConnectionId);
                throw new HubException($"Intentional {hubMethodName} failure.");
            }

            if (attempt == 2)
            {
                RetryStarted.SetResult();
                await AllowRetry.Task.WaitAsync(invocationContext.Context.ConnectionAborted);
            }

            var result = await next(invocationContext);
            if (attempt == 2)
            {
                RetriedInvocation.SetResult(invocationContext.Context.ConnectionId);
            }
            else
            {
                SubsequentInvocation.TrySetResult();
            }

            return result;
        }

        public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
        {
            await next(context, exception);
            if (context.Context.ConnectionId == _failedConnectionId)
            {
                ConnectionClosed.TrySetResult();
            }
        }
    }
}
