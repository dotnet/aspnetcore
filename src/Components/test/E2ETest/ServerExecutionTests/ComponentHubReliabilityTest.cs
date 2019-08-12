// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ComponentHubReliabilityTest : IClassFixture<AspNetSiteServerFixture>, IDisposable
    {
        private static readonly TimeSpan DefaultLatencyTimeout = TimeSpan.FromSeconds(Debugger.IsAttached ? 60 : 10);
        private readonly AspNetSiteServerFixture _serverFixture;

        public ComponentHubReliabilityTest(AspNetSiteServerFixture serverFixture, ITestOutputHelper output)
        {
            _serverFixture = serverFixture;
            Output = output;

            serverFixture.BuildWebHostMethod = TestServer.Program.BuildWebHost;
            CreateDefaultConfiguration();
        }

        public BlazorClient Client { get; set; }
        public ITestOutputHelper Output { get; set; }
        private IList<Batch> Batches { get; set; } = new List<Batch>();
        private IList<string> Errors { get; set; } = new List<string>();
        private ConcurrentQueue<LogMessage> Logs { get; set; } = new ConcurrentQueue<LogMessage>();

        public TestSink TestSink { get; set; }

        private void CreateDefaultConfiguration()
        {
            Client = new BlazorClient() { DefaultLatencyTimeout = DefaultLatencyTimeout };
            Client.RenderBatchReceived += (id, data) => Batches.Add(new Batch(id, data));
            Client.OnCircuitError += (error) => Errors.Add(error);
            Client.LoggerProvider = new XunitLoggerProvider(Output);
            Client.FormatError = (error) =>
            {
                var logs = string.Join(Environment.NewLine, Logs);
                return new Exception(error + Environment.NewLine + logs);
            };

            _ = _serverFixture.RootUri; // this is needed for the side-effects of getting the URI.
            TestSink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            TestSink.MessageLogged += LogMessages;
        }

        private void LogMessages(WriteContext context)
        {
            var log = new LogMessage(context.LogLevel, context.Message, context.Exception);
            Logs.Enqueue(log);
            Output.WriteLine(log.ToString());
        }

        [Fact]
        public async Task CannotStartMultipleCircuits()
        {
            // Arrange
            var expectedError = "The circuit host '.*?' has already been initialized.";
            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
            Assert.Single(Batches);

            // Act
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                "StartCircuit",
                baseUri,
                baseUri + "/home"));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Matches(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
        }

        [Fact]
        public async Task CannotStartCircuitWithNullData()
        {
            // Arrange
            var expectedError = "The uris provided are invalid.";
            var rootUri = _serverFixture.RootUri;
            var uri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(uri, prerendered: false, connectAutomatically: false), "Couldn't connect to the app");

            // Act
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync("StartCircuit", null, null));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Matches(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
        }

        // This is a hand-chosen example of something that will cause an exception in creating the circuit host.
        // We want to test this case so that we know what happens when creating the circuit host blows up.
        [Fact]
        public async Task StartCircuitCausesInitializationError()
        {
            // Arrange
            var expectedError = "The circuit failed to initialize.";
            var rootUri = _serverFixture.RootUri;
            var uri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(uri, prerendered: false, connectAutomatically: false), "Couldn't connect to the app");

            // Act
            //
            // These are valid URIs by the BaseUri doesn't contain the Uri - so it fails to initialize.
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync("StartCircuit", uri, "http://example.com"));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Matches(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
        }

        [Fact]
        public async Task CannotInvokeJSInteropBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false, connectAutomatically: false));
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
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message) == (LogLevel.Debug, "Call to 'BeginInvokeDotNetFromJS' received before the circuit host initialization"));
        }

        [Fact]
        public async Task CannotInvokeJSInteropCallbackCompletionsBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false, connectAutomatically: false));
            Assert.Empty(Batches);

            // Act
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                "EndInvokeJSFromDotNet",
                3,
                true,
                "[]"));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Equal(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message) == (LogLevel.Debug, "Call to 'EndInvokeJSFromDotNet' received before the circuit host initialization"));
        }

        [Fact]
        public async Task CannotDispatchBrowserEventsBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false, connectAutomatically: false));
            Assert.Empty(Batches);

            // Act
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                "DispatchBrowserEvent",
                "",
                ""));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Equal(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message) == (LogLevel.Debug, "Call to 'DispatchBrowserEvent' received before the circuit host initialization"));
        }

        private async Task GoToTestComponent(IList<Batch> batches)
        {
            var rootUri = _serverFixture.RootUri;
            Assert.True(await Client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");
            Assert.Single(batches);

            await Client.SelectAsync("test-selector-select", "BasicTestApp.CounterComponent");
            Assert.Equal(2, batches.Count);
        }

        [Fact]
        public async Task DispatchingAnInvalidEventArgument_DoesNotProduceWarnings()
        {
            // Arrange
            var expectedError = $"There was an unhandled exception on the current circuit, so this circuit will be terminated. For more details turn on " +
                $"detailed exceptions in 'CircuitOptions.DetailedErrors'. Bad input data.";

            var eventDescriptor = Serialize(new WebEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = 3,
                EventArgsType = "mouse",
            });

            await GoToTestComponent(Batches);
            Assert.Equal(2, Batches.Count);

            // Act
            await Client.ExpectCircuitError(() => Client.HubConnection.SendAsync(
                "DispatchBrowserEvent",
                eventDescriptor,
                "{sadfadsf]"));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Equal(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Exception?.Message) == (LogLevel.Debug, "There was an error parsing the event arguments. EventId: '3'."));
        }

        [Fact]
        public async Task DispatchingAnInvalidEvent_DoesNotTriggerWarnings()
        {
            // Arrange
            var expectedError = $"There was an unhandled exception on the current circuit, so this circuit will be terminated. For more details turn on " +
                $"detailed exceptions in 'CircuitOptions.DetailedErrors'. Failed to dispatch event.";

            var eventDescriptor = Serialize(new WebEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = 1990,
                EventArgsType = "mouse",
            });

            var eventArgs = new UIMouseEventArgs
            {
                Type = "click",
                Detail = 1,
                ScreenX = 47,
                ScreenY = 258,
                ClientX = 47,
                ClientY = 155,
            };

            await GoToTestComponent(Batches);
            Assert.Equal(2, Batches.Count);

            // Act
            await Client.ExpectCircuitError(() => Client.HubConnection.SendAsync(
                "DispatchBrowserEvent",
                eventDescriptor,
                Serialize(eventArgs)));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Equal(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message, l.Exception?.Message) ==
                (LogLevel.Debug,
                "There was an error dispatching the event '1990' to the application.",
                "There is no event handler associated with this event. EventId: '1990'. (Parameter 'eventHandlerId')"));
        }

        [Fact]
        public async Task DispatchingAnInvalidRenderAcknowledgement_DoesNotTriggerWarnings()
        {
            // Arrange
            var expectedError = $"There was an unhandled exception on the current circuit, so this circuit will be terminated. For more details turn on " +
                $"detailed exceptions in 'CircuitOptions.DetailedErrors'. Failed to complete render batch '1846'.";

            await GoToTestComponent(Batches);
            Assert.Equal(2, Batches.Count);

            Client.ConfirmRenderBatch = false;
            await Client.ClickAsync("counter");

            // Act
            await Client.ExpectCircuitError(() => Client.HubConnection.SendAsync(
                "OnRenderCompleted",
                1846,
                null));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Equal(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message, l.Exception?.Message) ==
                (LogLevel.Debug,
                $"Failed to complete render batch '1846' in circuit host '{Client.CircuitId}'.",
                "Received an acknowledgement for batch with id '1846' when the last batch produced was '4'."));
        }

        [Fact]
        public async Task CannotInvokeOnRenderCompletedBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false, connectAutomatically: false));
            Assert.Empty(Batches);

            // Act
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                "OnRenderCompleted",
                5,
                null));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Equal(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message) == (LogLevel.Debug, "Call to 'OnRenderCompleted' received before the circuit host initialization"));
        }

        [Fact]
        public async Task CannotInvokeOnLocationChangedBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false, connectAutomatically: false));
            Assert.Empty(Batches);

            // Act
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                "OnLocationChanged",
                baseUri.AbsoluteUri,
                false));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Equal(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message) == (LogLevel.Debug, "Call to 'OnLocationChanged' received before the circuit host initialization"));
        }

        [Fact]
        public async Task OnLocationChanged_ReportsDebugForExceptionInValidation()
        {
            // Arrange
            var expectedError = "There was an unhandled exception on the current circuit, so this circuit will be terminated. " +
                "For more details turn on detailed exceptions in 'CircuitOptions.DetailedErrors'. " +
                "Location change to 'http://example.com' failed.";

            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
            Assert.Single(Batches);

            // Act
            await Client.ExpectCircuitError(() => Client.HubConnection.SendAsync(
                "OnLocationChanged",
                "http://example.com",
                false));

            // Assert
            var actualError = Assert.Single(Errors);
            Assert.Equal(expectedError, actualError);
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l =>
            {
                return (l.LogLevel, l.Message) == (LogLevel.Debug, $"Location change to 'http://example.com' in circuit '{Client.CircuitId}' failed.");
            });
        }

        [Fact]
        public async Task OnLocationChanged_ReportsErrorForExceptionInUserCode()
        {
            // Arrange
            var expectedError = "There was an unhandled exception on the current circuit, so this circuit will be terminated. " +
                "For more details turn on detailed exceptions in 'CircuitOptions.DetailedErrors'. " +
                "Location change failed.";

            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
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
            Assert.Contains(Logs, l =>
            {
                return (l.LogLevel, l.Message) == (LogLevel.Error, $"Location change to '{new Uri(_serverFixture.RootUri,"/test")}' in circuit '{Client.CircuitId}' failed.");
            });
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
            // Arrange
            var expectedError = "Unhandled exception in circuit .*";
            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
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
                Logs,
                e => LogLevel.Error == e.LogLevel && Regex.IsMatch(e.Message, expectedError));
        }

        [Fact]
        public async Task ComponentDisposeMethodThrowsExceptionTerminatesTheCircuit()
        {
            // Arrange
            var expectedError = "Unhandled exception in circuit .*";
            var rootUri = _serverFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
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
                Logs,
                e => LogLevel.Error == e.LogLevel && Regex.IsMatch(e.Message, expectedError));
        }

        public void Dispose()
        {
            TestSink.MessageLogged -= LogMessages;
        }

        private string Serialize<T>(T browserEventDescriptor) =>
            JsonSerializer.Serialize(browserEventDescriptor, TestJsonSerializerOptionsProvider.Options);

        [DebuggerDisplay("{LogLevel.ToString(),nq} - {Message ?? \"null\",nq} - {Exception?.Message,nq}")]
        private class LogMessage
        {
            public LogMessage(LogLevel logLevel, string message, Exception exception)
            {
                LogLevel = logLevel;
                Message = message;
                Exception = exception;
            }

            public LogLevel LogLevel { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }

            public override string ToString()
            {
                return $"{LogLevel}: {Message}{(Exception != null ? Environment.NewLine : "")}{Exception}";
            }
        }

        private class Batch
        {
            public Batch(int id, byte[] data)
            {
                Id = id;
                Data = data;
            }

            public int Id { get; }
            public byte[] Data { get; }
        }
    }
}
