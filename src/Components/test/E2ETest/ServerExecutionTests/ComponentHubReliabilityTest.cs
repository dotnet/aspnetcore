// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Testing;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using TestServer;
using Xunit;
using Ignitor;
using System.Collections.Generic;
using Xunit.Abstractions;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19666")]
    public class ComponentHubReliabilityTest : FunctionalTestBase
    {
        private static readonly TimeSpan DefaultTimeout = Debugger.IsAttached ? TimeSpan.MaxValue : TimeSpan.FromSeconds(30);

        protected BlazorClient Client { get; private set; }

        protected ITestOutputHelper Output { get; }

        protected TimeSpan Timeout { get; set; } = DefaultTimeout;

        protected IReadOnlyCollection<CapturedRenderBatch> Batches => Client?.Operations?.Batches;

        protected IReadOnlyCollection<string> DotNetCompletions => Client?.Operations?.DotNetCompletions;

        protected IReadOnlyCollection<string> Errors => Client?.Operations?.Errors;

        protected IReadOnlyCollection<CapturedJSInteropCall> JSInteropCalls => Client?.Operations?.JSInteropCalls;

        public ComponentHubReliabilityTest(ITestOutputHelper output)
        {
            Output = output;
            Client = new BlazorClient()
            {
                CaptureOperations = true,
                DefaultOperationTimeout = Timeout,
            };
            Client.LoggerProvider = new XunitLoggerProvider(Output);
        }

        [Fact]
        public async Task CannotStartMultipleCircuits()
        {
            // Arrange
            var expectedError = "The circuit host '.*?' has already been initialized.";

            await using (var server = await StartServer<ServerStartup>())
            {
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);

                var baseUri = new Uri($"{server.Url}/subdir");
                Assert.True(await Client.ConnectAsync(baseUri), "Couldn't connect to the app");
                Assert.Single(Batches);

                var descriptors = await Client.GetPrerenderDescriptors(baseUri);

                // Act
                await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                    "StartCircuit",
                    baseUri,
                    baseUri + "/home",
                    descriptors,
                    null));

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Matches(expectedError, actualError);
                Assert.DoesNotContain(logs, l => l.Write.LogLevel > LogLevel.Information);
            }
        }

        [Fact]
        public async Task CannotStartCircuitWithNullData()
        {
            // Arrange
            var expectedError = "The uris provided are invalid.";
            await using (var server = await StartServer<ServerStartup>())
            {
                var rootUri = server.Url;
                var uri = new Uri($"{rootUri}/subdir");
                Assert.True(await Client.ConnectAsync(uri, connectAutomatically: false), "Couldn't connect to the app");
                var descriptors = await Client.GetPrerenderDescriptors(uri);

                // Act
                await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync("StartCircuit", null, null, descriptors, null));

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Matches(expectedError, actualError);
            }
        }

        // This is a hand-chosen example of something that will cause an exception in creating the circuit host.
        // We want to test this case so that we know what happens when creating the circuit host blows up.
        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19666")]
        public async Task StartCircuitCausesInitializationError()
        {
            // Arrange
            var expectedError = "The circuit failed to initialize.";

            await using (var server = await StartServer<ServerStartup>())
            {
                var rootUri = server.Url;
                var uri = new Uri($"{rootUri}/subdir");
                Assert.True(await Client.ConnectAsync(uri, connectAutomatically: false), "Couldn't connect to the app");
                var descriptors = await Client.GetPrerenderDescriptors(uri);

                // Act
                //
                // These are valid URIs by the BaseUri doesn't contain the Uri - so it fails to initialize.
                await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync("StartCircuit", uri, "http://example.com", descriptors, null), Timeout);

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Matches(expectedError, actualError);
            }
        }

        [Fact]
        public async Task CannotInvokeJSInteropBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            await using (var server = await StartServer<ServerStartup>())
            {
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);
                var rootUri = server.Url;
                var baseUri = new Uri($"{rootUri}/subdir");
                Assert.True(await Client.ConnectAsync(baseUri, connectAutomatically: false));
                Assert.Empty(Batches);

                // Act
                await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                    "BeginInvokeDotNetFromJS",
                    "",
                    "",
                    "",
                    0,
                    ""));

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Equal(expectedError, actualError);
                Assert.DoesNotContain(logs, l => l.Write.LogLevel > LogLevel.Information);
                Assert.Contains(logs, l =>
                    l.Write.LogLevel == LogLevel.Debug && l.Write.Message.Contains("Call to 'BeginInvokeDotNetFromJS' received before the circuit host initialization"));
            }
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19666")]
        public async Task CannotInvokeJSInteropCallbackCompletionsBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            await using (var server = await StartServer<ServerStartup>())
            {
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);
                var rootUri = server.Url;
                var baseUri = new Uri($"{rootUri}/subdir");
                Assert.True(await Client.ConnectAsync(baseUri, connectAutomatically: false));
                Assert.Empty(Batches);

                // Act
                await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                    "EndInvokeJSFromDotNet",
                    3,
                    true,
                    "[]"), Timeout);

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Equal(expectedError, actualError);
                Assert.DoesNotContain(logs, l => l.Write.LogLevel > LogLevel.Information);
                Assert.Contains(logs, l => l.Write.LogLevel == LogLevel.Debug && l.Write.Message.Contains("Call to 'EndInvokeJSFromDotNet' received before the circuit host initialization"));
            }
        }

        [Fact]
        public async Task CannotDispatchBrowserEventsBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            await using (var server = await StartServer<ServerStartup>())
            {
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);
                var rootUri = server.Url;
                var baseUri = new Uri($"{rootUri}/subdir");
                Assert.True(await Client.ConnectAsync(baseUri, connectAutomatically: false));
                Assert.Empty(Batches);

                // Act
                await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                    "DispatchBrowserEvent",
                    "",
                    ""));

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Equal(expectedError, actualError);
                Assert.DoesNotContain(logs, l => l.Write.LogLevel > LogLevel.Information);
                Assert.Contains(logs, l => l.Write.LogLevel == LogLevel.Debug && l.Write.Message.Contains("Call to 'DispatchBrowserEvent' received before the circuit host initialization"));
            }
        }

        [Fact]
        public async Task CannotInvokeOnRenderCompletedBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            await using (var server = await StartServer<ServerStartup>())
            {
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);
                var rootUri = server.Url;
                var baseUri = new Uri($"{rootUri}/subdir");
                Assert.True(await Client.ConnectAsync(baseUri, connectAutomatically: false));
                Assert.Empty(Batches);

                // Act
                await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                    "OnRenderCompleted",
                    5,
                    null));

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Equal(expectedError, actualError);
                Assert.DoesNotContain(logs, l => l.Write.LogLevel > LogLevel.Information);
                Assert.Contains(logs, l => l.Write.LogLevel == LogLevel.Debug && l.Write.Message.Contains("Call to 'OnRenderCompleted' received before the circuit host initialization"));
            }
        }

        [Fact]
        public async Task CannotInvokeOnLocationChangedBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            await using (var server = await StartServer<ServerStartup>())
            {
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);
                var rootUri = server.Url;
                var baseUri = new Uri($"{rootUri}/subdir");
                Assert.True(await Client.ConnectAsync(baseUri, connectAutomatically: false));
                Assert.Empty(Batches);

                // Act
                await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                    "OnLocationChanged",
                    baseUri.AbsoluteUri,
                    false));

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Equal(expectedError, actualError);
                Assert.DoesNotContain(logs, l => l.Write.LogLevel > LogLevel.Information);
                Assert.Contains(logs, l => l.Write.LogLevel == LogLevel.Debug && l.Write.Message.Contains("Call to 'OnLocationChanged' received before the circuit host initialization"));
            }
        }

        [Fact]
        public async Task OnLocationChanged_ReportsDebugForExceptionInValidation()
        {
            // Arrange
            var expectedError = "There was an unhandled exception on the current circuit, so this circuit will be terminated. " +
                "For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'. " +
                "Location change to 'http://example.com' failed.";

            await using (var server = await StartServer<ServerStartup>(verifyLogs: false))
            {
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);
                var rootUri = server.Url;
                var baseUri = new Uri($"{rootUri}/subdir");

                Assert.True(await Client.ConnectAsync(baseUri));
                Assert.Single(Batches);

                // Act
                await Client.ExpectCircuitError(() => Client.HubConnection.SendAsync(
                    "OnLocationChanged",
                    "http://example.com",
                    false));

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Equal(expectedError, actualError);
                Assert.DoesNotContain(logs, l => l.Write.LogLevel > LogLevel.Information);

                var entry = Assert.Single(logs, l => l.Write.EventId.Name == "LocationChangeFailed");
                Assert.Equal(LogLevel.Debug, entry.Write.LogLevel);
                Assert.Matches("Location change to 'http://example.com' in circuit '.*' failed\\.", entry.Write.Message);
            }
        }

        [Fact]
        public async Task OnLocationChanged_ReportsErrorForExceptionInUserCode()
        {
            // Arrange
            var expectedError = "There was an unhandled exception on the current circuit, so this circuit will be terminated. " +
                "For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'. " +
                "Location change failed.";

            await using (var server = await StartServer<ServerStartup>(verifyLogs: false))
            {
                // while (!System.Diagnostics.Debugger.IsAttached)
                // {
                //     System.Console.WriteLine($"Waiting to attach on ${System.Diagnostics.Process.GetCurrentProcess().Id}");
                //     System.Threading.Thread.Sleep(1000);
                // }
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);
                var rootUri = server.Url;
                var baseUri = new Uri($"{rootUri}/subdir");

                Assert.True(await Client.ConnectAsync(baseUri));
                Assert.Single(Batches);

                await Client.SelectAsync("test-selector-select", "BasicTestApp.NavigationFailureComponent");

                // Act
                await Client.ExpectCircuitError(() => Client.HubConnection.SendAsync(
                    "OnLocationChanged",
                    new Uri(baseUri, "/test").AbsoluteUri,
                    false));

                // Assert
                var actualError = Assert.Single(Errors);
                Assert.Equal(expectedError, actualError);

                var entry = Assert.Single(logs, l => l.Write.EventId.Name == "LocationChangeFailed");
                Assert.Equal(LogLevel.Error, entry.Write.LogLevel);
                Assert.Matches($"Location change to '{new Uri($"{server.Url}/test")}' in circuit '.*' failed.", entry.Write.Message);
            }
        }

        [Theory]
        [InlineData("constructor-throw")]
        [InlineData("attach-throw")]
        [InlineData("setparameters-sync-throw")]
        [InlineData("setparameters-async-throw")]
        [InlineData("render-throw")]
        [InlineData("afterrender-sync-throw")]
        [InlineData("afterrender-async-throw")]
        public async Task ComponentLifecycleMethodThrowsExceptionTerminatesTheCircuit(string id)
        {
            if (id == "setparameters-async-throw")
            {
                // In the case of setparameters-async-throw, the exception isn't triggered until after
                // a renderbatch. This would lead to timing-based flakiness, because that batch's ACK
                // may be received either before or after the subsequent event that is meant to trigger
                // circuit termination. If it was received before, then the circuit would be terminated
                // prematurely by the OnRenderCompleted call. To avoid timing-based flakiness, we can
                // just not send OnRenderCompleted calls as they aren't required for this scenario.
                Client.ConfirmRenderBatch = false;
            }

            // Arrange
            var expectedError = "Unhandled exception in circuit .*";
            await using (var server = await StartServer<ServerStartup>(verifyLogs: false))
            {
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);
                var rootUri = server.Url;
                var baseUri = new Uri($"{rootUri}/subdir");

                Assert.True(await Client.ConnectAsync(baseUri));
                Assert.Single(Batches);

                await Client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");

                // Act
                await Client.ExpectCircuitError(async () =>
                {
                    await Client.ClickAsync(id, expectRenderBatch: false);
                });

                // Now if you try to click again, you will get *forcibly* disconnected for trying to talk to
                // a circuit that's gone.
                await Client.ExpectCircuitErrorAndDisconnect(async () =>
                {
                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await Client.ClickAsync(id, expectRenderBatch: false));
                });

                // Checking logs at the end to avoid race condition.
                Assert.Contains(
                    logs,
                    e => LogLevel.Error == e.Write.LogLevel && Regex.IsMatch(e.Write.Message, expectedError));
            }
        }

        [Fact]
        public async Task ComponentDisposeMethodThrowsExceptionTerminatesTheCircuit()
        {
            // Arrange
            var expectedError = "Unhandled exception in circuit .*";
            await using (var server = await StartServer<ServerStartup>(verifyLogs: false))
            {
                ConcurrentQueue<Microsoft.AspNetCore.SignalR.Tests.LogRecord> logs = new();
                server.ServerLogged += (LogRecord record) => logs.Enqueue(record);
                var rootUri = server.Url;
                var baseUri = new Uri($"{rootUri}/subdir");

                Assert.True(await Client.ConnectAsync(baseUri));
                Assert.Single(Batches);

                await Client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");

                // Act - show then hide
                await Client.ClickAsync("dispose-throw");
                await Client.ExpectCircuitError(async () =>
                {
                    await Client.ClickAsync("dispose-throw", expectRenderBatch: false);
                });

                // Now if you try to click again, you will get *forcibly* disconnected for trying to talk to
                // a circuit that's gone.
                await Client.ExpectCircuitErrorAndDisconnect(async () =>
                {
                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await Client.ClickAsync("dispose-throw", expectRenderBatch: false));
                });

                // Checking logs at the end to avoid race condition.
                Assert.Contains(
                    logs,
                    e => LogLevel.Error == e.Write.LogLevel && Regex.IsMatch(e.Write.Message, expectedError));
            }
        }
    }
}
