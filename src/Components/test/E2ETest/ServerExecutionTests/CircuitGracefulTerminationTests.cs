// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests
{
    public class CircuitGracefulTerminationTests : BasicTestAppTestBase, IDisposable
    {
        public CircuitGracefulTerminationTests(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }

        public TaskCompletionSource<object> GracefulDisconnectCompletionSource { get; private set; }
        public TestSink Sink { get; private set; }
        public List<(Extensions.Logging.LogLevel level, string eventIdName)> Messages { get; private set; }

        protected override void InitializeAsyncCore()
        {
            // On WebAssembly, page reloads are expensive so skip if possible
            Navigate(ServerPathBase, _serverFixture.ExecutionMode == ExecutionMode.Client);
            MountTestComponent<CounterComponent>();
            Browser.Equal("Current count: 0", () => Browser.FindElement(By.TagName("p")).Text);
        }

        [Fact]
        public async Task ReloadingThePage_GracefullyDisconnects_TheCurrentCircuit()
        {
            // Arrange
            GracefulDisconnectCompletionSource = new TaskCompletionSource<object>(TaskContinuationOptions.RunContinuationsAsynchronously);
            Sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            Messages = new List<(Extensions.Logging.LogLevel level, string eventIdName)>();
            Sink.MessageLogged += wc => Log(wc);

            // Act
            _ = ((IJavaScriptExecutor)Browser).ExecuteScript("location.reload()");
            await Task.WhenAny(Task.Delay(10000), GracefulDisconnectCompletionSource.Task);

            // Assert
            Assert.Contains((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully"), Messages);
        }

        [Fact]
        public async Task ClosingTheBrowserWindow_GracefullyDisconnects_TheCurrentCircuit()
        {
            // Arrange
            GracefulDisconnectCompletionSource = new TaskCompletionSource<object>(TaskContinuationOptions.RunContinuationsAsynchronously);
            Sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            Messages = new List<(Extensions.Logging.LogLevel level, string eventIdName)>();
            Sink.MessageLogged += wc => Log(wc);

            // Act
            Browser.Close();
            await Task.WhenAny(Task.Delay(10000), GracefulDisconnectCompletionSource.Task);

            // Assert
            Assert.Contains((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully"), Messages);
        }

        [Fact]
        public async Task ClosingTheBrowserWindow_GracefullyDisconnects_WhenNavigatingAwayFromThePage()
        {
            // Arrange
            GracefulDisconnectCompletionSource = new TaskCompletionSource<object>(TaskContinuationOptions.RunContinuationsAsynchronously);
            Sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            Messages = new List<(Extensions.Logging.LogLevel level, string eventIdName)>();
            Sink.MessageLogged += wc => Log(wc);

            // Act
            Browser.Navigate().GoToUrl("about:blank");
            await Task.WhenAny(Task.Delay(10000), GracefulDisconnectCompletionSource.Task);

            // Assert
            Assert.Contains((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully"), Messages);
        }

        private void Log(WriteContext wc)
        {
            if ((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully") == (wc.LogLevel, wc.EventId.Name))
            {
                GracefulDisconnectCompletionSource.TrySetResult(null);
            }
            Messages.Add((wc.LogLevel, wc.EventId.Name));
        }

        public void Dispose()
        {
            Sink.MessageLogged -= Log;
        }
    }
}
