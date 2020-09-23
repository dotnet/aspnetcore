// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19666")]
    public class ComponentHubInvalidEventTest : IgnitorTest<ServerStartup>
    {
        public ComponentHubInvalidEventTest(BasicTestAppServerSiteFixture<ServerStartup> serverFixture, ITestOutputHelper output)
            : base(serverFixture, output)
        {
        }

        protected async override Task InitializeAsync()
        {
            var rootUri = ServerFixture.RootUri;
            Assert.True(await Client.ConnectAsync(new Uri(rootUri, "/subdir")), "Couldn't connect to the app");
            Assert.Single(Batches);

            await Client.SelectAsync("test-selector-select", "BasicTestApp.CounterComponent");
            Assert.Equal(2, Batches.Count);
        }

        [Fact]
        public async Task DispatchingAnInvalidEventArgument_DoesNotProduceWarnings()
        {
            // Arrange
            var expectedError = $"There was an unhandled exception on the current circuit, so this circuit will be terminated. For more details turn on " +
                $"detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'. Bad input data.";

            var eventDescriptor = Serialize(new WebEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = 3,
                EventArgsType = "mouse",
            });

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
                $"detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'. Failed to dispatch event.";

            var eventDescriptor = Serialize(new WebEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = 1990,
                EventArgsType = "mouse",
            });

            var eventArgs = new MouseEventArgs
            {
                Type = "click",
                Detail = 1,
                ScreenX = 47,
                ScreenY = 258,
                ClientX = 47,
                ClientY = 155,
            };

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
                $"detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'. Failed to complete render batch '1846'.";


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

            var entry = Assert.Single(Logs, l => l.EventId.Name == "OnRenderCompletedFailed");
            Assert.Equal(LogLevel.Debug, entry.LogLevel);
            Assert.Matches("Failed to complete render batch '1846' in circuit host '.*'\\.", entry.Message);
            Assert.Equal("Received an acknowledgement for batch with id '1846' when the last batch produced was '4'.", entry.Exception.Message);
        }

        private string Serialize<T>(T browserEventDescriptor) =>
            JsonSerializer.Serialize(browserEventDescriptor, TestJsonSerializerOptionsProvider.Options);
    }
}
