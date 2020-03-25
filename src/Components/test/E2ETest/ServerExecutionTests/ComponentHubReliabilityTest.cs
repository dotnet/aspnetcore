// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19666")]
    public class ComponentHubReliabilityTest : IgnitorTest<ServerStartup>
    {
        public ComponentHubReliabilityTest(BasicTestAppServerSiteFixture<ServerStartup> serverFixture, ITestOutputHelper output)
            : base(serverFixture, output)
        {
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19414")]
        public async Task CannotStartMultipleCircuits()
        {
            // Arrange
            var expectedError = "The circuit host '.*?' has already been initialized.";
            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri), "Couldn't connect to the app");
            Assert.Single(Batches);

            var descriptors = await Client.GetPrerenderDescriptors(baseUri);

            // Act
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync(
                "StartCircuit",
                baseUri,
                baseUri + "/home",
                descriptors));

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
            var rootUri = ServerFixture.RootUri;
            var uri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(uri, connectAutomatically: false), "Couldn't connect to the app");
            var descriptors = await Client.GetPrerenderDescriptors(uri);

            // Act
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync("StartCircuit", null, null, descriptors));

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
            var rootUri = ServerFixture.RootUri;
            var uri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(uri, connectAutomatically: false), "Couldn't connect to the app");
            var descriptors = await Client.GetPrerenderDescriptors(uri);

            // Act
            //
            // These are valid URIs by the BaseUri doesn't contain the Uri - so it fails to initialize.
            await Client.ExpectCircuitErrorAndDisconnect(() => Client.HubConnection.SendAsync("StartCircuit", uri, "http://example.com", descriptors));

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
            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
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
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message) == (LogLevel.Debug, "Call to 'BeginInvokeDotNetFromJS' received before the circuit host initialization"));
        }

        [Fact]
        public async Task CannotInvokeJSInteropCallbackCompletionsBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, connectAutomatically: false));
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
            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
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
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message) == (LogLevel.Debug, "Call to 'DispatchBrowserEvent' received before the circuit host initialization"));
        }

        [Fact]
        public async Task CannotInvokeOnRenderCompletedBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
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
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message) == (LogLevel.Debug, "Call to 'OnRenderCompleted' received before the circuit host initialization"));
        }

        [Fact]
        public async Task CannotInvokeOnLocationChangedBeforeInitialization()
        {
            // Arrange
            var expectedError = "Circuit not initialized.";
            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
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
            Assert.DoesNotContain(Logs, l => l.LogLevel > LogLevel.Information);
            Assert.Contains(Logs, l => (l.LogLevel, l.Message) == (LogLevel.Debug, "Call to 'OnLocationChanged' received before the circuit host initialization"));
        }

        [Fact]
        public async Task OnLocationChanged_ReportsDebugForExceptionInValidation()
        {
            // Arrange
            var expectedError = "There was an unhandled exception on the current circuit, so this circuit will be terminated. " +
                "For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'. " +
                "Location change to 'http://example.com' failed.";

            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri), "Couldn't connect to the app");
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

            var entry = Assert.Single(Logs, l => l.EventId.Name == "LocationChangeFailed");
            Assert.Equal(LogLevel.Debug, entry.LogLevel);
            Assert.Matches("Location change to 'http://example.com' in circuit '.*' failed\\.", entry.Message);
        }

        [Fact]
        public async Task OnLocationChanged_ReportsErrorForExceptionInUserCode()
        {
            // Arrange
            var expectedError = "There was an unhandled exception on the current circuit, so this circuit will be terminated. " +
                "For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'. " +
                "Location change failed.";

            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri), "Couldn't connect to the app");
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

            var entry = Assert.Single(Logs, l => l.EventId.Name == "LocationChangeFailed");
            Assert.Equal(LogLevel.Error, entry.LogLevel);
            Assert.Matches($"Location change to '{new Uri(ServerFixture.RootUri, "/test")}' in circuit '.*' failed\\.", entry.Message);
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
            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri), "Couldn't connect to the app");
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
            var rootUri = ServerFixture.RootUri;
            var baseUri = new Uri(rootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri), "Couldn't connect to the app");
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
    }
}
