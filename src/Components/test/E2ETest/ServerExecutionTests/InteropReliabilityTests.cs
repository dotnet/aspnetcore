// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ignitor;
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
    public class InteropReliabilityTests : IgnitorTest<ServerStartup>
    {
        public InteropReliabilityTests(BasicTestAppServerSiteFixture<ServerStartup> serverFixture, ITestOutputHelper output)
            : base(serverFixture, output)
        {
        }

        protected async override Task InitializeAsync()
        {
            var rootUri = ServerFixture.RootUri;
            Assert.True(await Client.ConnectAsync(new Uri(rootUri, "/subdir")), "Couldn't connect to the app");
            Assert.Single(Batches);

            await Client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            Assert.Equal(2, Batches.Count);
        }

        [Fact]
        public async Task CannotInvokeNonJSInvokableMethods()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027WriteAllText\\u0027 on assembly \\u0027System.IO.FileSystem\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "System.IO.FileSystem",
                "WriteAllText",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            // Assert
            Assert.Single(DotNetCompletions, c => c == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeNonExistingMethods()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027MadeUpMethod\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "MadeUpMethod",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            // Assert
            Assert.Single(DotNetCompletions, c => c == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongNumberOfArguments()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new[] { ServerFixture.RootUri }));

            // Assert
            Assert.Single(DotNetCompletions, c => c == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyAssemblyName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new object[] { ServerFixture.RootUri + "counter", false }));

            // Assert
            Assert.Single(DotNetCompletions, c => c == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyMethodName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "",
                null,
                JsonSerializer.Serialize(new object[] { ServerFixture.RootUri + "counter", false }));

            // Assert
            Assert.Single(DotNetCompletions, c => c == expectedError);

            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongReferenceId()
        {
            // Arrange
            var expectedDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027Reverse\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(DotNetCompletions, c => c == expectedDotNetObjectRef);

            await Client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                1,
                JsonSerializer.Serialize(Array.Empty<object>()));

            // Assert
            Assert.Single(DotNetCompletions, c => c == "[\"1\",true,\"tnatropmI\"]");

            await Client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                3, // non existing ref
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(DotNetCompletions, c => c == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWrongReferenceIdType()
        {
            // Arrange
            var expectedImportantDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027ReceiveTrivial\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(DotNetCompletions, c => c == expectedImportantDotNetObjectRef);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "ReceiveTrivial",
                null,
                JsonSerializer.Serialize(new object[] { new { __dotNetObject = 1 } }));

            // Assert
            Assert.Single(DotNetCompletions, c => c == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19410")]
        public async Task ContinuesWorkingAfterInvalidAsyncReturnCallback()
        {
            // Arrange
            var expectedError = "An exception occurred executing JS interop: The JSON value could not be converted to System.Int32. Path: $ | LineNumber: 0 | BytePositionInLine: 3.. See InnerException for more details.";

            // Act
            await Client.ClickAsync("triggerjsinterop-malformed");

            var call = JSInteropCalls.FirstOrDefault(call => call.Identifier == "sendMalformedCallbackReturn");
            Assert.NotEqual(default, call);

            var id = call.AsyncHandle;
            await Client.ExpectRenderBatch(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                    "EndInvokeJSFromDotNet",
                    id,
                    true,
                    $"[{id}, true, \"{{\"]");
            });

            var text = Assert.Single(
                Client.FindElementById("errormessage-malformed").Children.OfType<TextNode>(),
                e => expectedError == e.TextContent);

            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task JSInteropCompletionSuccess()
        {
            // Arrange

            // Act
            await Client.ClickAsync("triggerjsinterop-success");

            var call = JSInteropCalls.FirstOrDefault(call => call.Identifier == "sendSuccessCallbackReturn");
            Assert.NotEqual(default, call);

            var id = call.AsyncHandle;
            await Client.ExpectRenderBatch(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                    "EndInvokeJSFromDotNet",
                    id,
                    true,
                    $"[{id}, true, null]");
            });

            Assert.Single(
                Client.FindElementById("errormessage-success").Children.OfType<TextNode>(),
                e => "" == e.TextContent);

            var entry = Assert.Single(Logs, l => l.EventId.Name == "EndInvokeJSSucceeded");
            Assert.Equal(LogLevel.Debug, entry.LogLevel);
        }

        [Fact]
        public async Task JSInteropThrowsInUserCode()
        {
            // Arrange

            // Act
            await Client.ClickAsync("triggerjsinterop-failure");

            var call = JSInteropCalls.FirstOrDefault(call => call.Identifier == "sendFailureCallbackReturn");
            Assert.NotEqual(default, call);

            var id = call.AsyncHandle;
            await Client.ExpectRenderBatch(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                    "EndInvokeJSFromDotNet",
                    id,
                    false,
                    $"[{id}, false, \"There was an error invoking sendFailureCallbackReturn\"]");
            });

            Assert.Single(
                Client.FindElementById("errormessage-failure").Children.OfType<TextNode>(),
                e => "There was an error invoking sendFailureCallbackReturn" == e.TextContent);

            var entry = Assert.Single(Logs, l => l.EventId.Name == "EndInvokeJSFailed");
            Assert.Equal(LogLevel.Debug, entry.LogLevel);

            Assert.DoesNotContain(Logs, m => m.LogLevel > LogLevel.Information);

            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task MalformedJSInteropCallbackDisposesCircuit()
        {
            // Arrange

            // Act
            await Client.ClickAsync("triggerjsinterop-malformed");

            var call = JSInteropCalls.FirstOrDefault(call => call.Identifier == "sendMalformedCallbackReturn");
            Assert.NotEqual(default, call);

            var id = call.AsyncHandle;
            await Client.ExpectCircuitError(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                    "EndInvokeJSFromDotNet",
                    id,
                    true,
                    $"[{id}, true, }}");
            });

            // A completely malformed payload like the one above never gets to the application.
            Assert.Single(
                Client.FindElementById("errormessage-malformed").Children.OfType<TextNode>(),
                e => "" == e.TextContent);

            var entry = Assert.Single(Logs, l => l.EventId.Name == "EndInvokeDispatchException");
            Assert.Equal(LogLevel.Debug, entry.LogLevel);

            await Client.ExpectCircuitErrorAndDisconnect(async () =>
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => Client.ClickAsync("event-handler-throw-sync", expectRenderBatch: true));
            });
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithInvalidArgumentsPayload()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                "[ \"invalidPayload\"}");

            // Assert
            Assert.Single(DotNetCompletions, c => c == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithMalformedArgumentPayload()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027ReceiveTrivial\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "ReceiveTrivial",
                null,
                "[ { \"data\": {\"}} ]");

            // Assert
            Assert.Single(DotNetCompletions, c => c == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task DispatchingEventsWithInvalidPayloadsShutsDownCircuitGracefully()
        {
            // Arrange

            // Act
            await Client.ExpectCircuitError(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                "DispatchBrowserEvent",
                null,
                null);
            });

            var entry = Assert.Single(Logs, l => l.EventId.Name == "DispatchEventFailedToParseEventData");
            Assert.Equal(LogLevel.Debug, entry.LogLevel);

            // Taking any other action will fail because the circuit is disposed.
            await Client.ExpectCircuitErrorAndDisconnect(async () =>
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => Client.ClickAsync("event-handler-throw-sync", expectRenderBatch: true));
            });
        }

        [Fact]
        public async Task DispatchingEventsWithInvalidEventDescriptor()
        {
            // Arrange

            // Act
            await Client.ExpectCircuitError(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                "DispatchBrowserEvent",
                "{Invalid:{\"payload}",
                "{}");
            });

            var entry = Assert.Single(Logs, l => l.EventId.Name == "DispatchEventFailedToParseEventData");
            Assert.Equal(LogLevel.Debug, entry.LogLevel);

            // Taking any other action will fail because the circuit is disposed.
            await Client.ExpectCircuitErrorAndDisconnect(async () =>
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => Client.ClickAsync("event-handler-throw-sync", expectRenderBatch: true));
            });
        }

        [Fact]
        public async Task DispatchingEventsWithInvalidEventArgs()
        {
            // Arrange

            // Act
            var browserDescriptor = new WebEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = 6,
                EventArgsType = "mouse",
            };

            await Client.ExpectCircuitError(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                    "DispatchBrowserEvent",
                    JsonSerializer.Serialize(browserDescriptor, TestJsonSerializerOptionsProvider.Options),
                    "{Invalid:{\"payload}");
            });

            Assert.Contains(
                Logs,
                e => e.EventId.Name == "DispatchEventFailedToParseEventData" && e.LogLevel == LogLevel.Debug &&
                     e.Exception.Message == "There was an error parsing the event arguments. EventId: '6'.");

            // Taking any other action will fail because the circuit is disposed.
            await Client.ExpectCircuitErrorAndDisconnect(async () =>
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => Client.ClickAsync("event-handler-throw-sync", expectRenderBatch: true));
            });
        }

        [Fact]
        public async Task DispatchingEventsWithInvalidEventHandlerId()
        {
            // Arrange

            // Act
            var mouseEventArgs = new MouseEventArgs()
            {
                Type = "click",
                Detail = 1
            };
            var browserDescriptor = new WebEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = 1,
                EventArgsType = "mouse",
            };

            await Client.ExpectCircuitError(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                "DispatchBrowserEvent",
                JsonSerializer.Serialize(browserDescriptor, TestJsonSerializerOptionsProvider.Options),
                JsonSerializer.Serialize(mouseEventArgs, TestJsonSerializerOptionsProvider.Options));
            });

            Assert.Contains(
                Logs,
                e => e.EventId.Name == "DispatchEventFailedToDispatchEvent" && e.LogLevel == LogLevel.Debug &&
                     e.Exception is ArgumentException ae && ae.Message.Contains("There is no event handler associated with this event. EventId: '1'."));

            // Taking any other action will fail because the circuit is disposed.
            await Client.ExpectCircuitErrorAndDisconnect(async () =>
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => Client.ClickAsync("event-handler-throw-sync", expectRenderBatch: true));
            });
        }

        [Fact]
        public async Task EventHandlerThrowsSyncExceptionTerminatesTheCircuit()
        {
            // Arrange

            // Act
            await Client.ExpectCircuitError(async () =>
            {
                await Client.ClickAsync("event-handler-throw-sync", expectRenderBatch: false);
            });

            Assert.Contains(
                Logs,
                e => LogLevel.Error == e.LogLevel &&
                    "CircuitUnhandledException" == e.EventId.Name &&
                    "Handler threw an exception" == e.Exception.Message);

            // Now if you try to click again, you will get *forcibly* disconnected for trying to talk to
            // a circuit that's gone.
            await Client.ExpectCircuitErrorAndDisconnect(async () =>
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => Client.ClickAsync("event-handler-throw-sync", expectRenderBatch: true));
            });
        }

        private Task ValidateClientKeepsWorking(BlazorClient Client, IReadOnlyCollection<CapturedRenderBatch> batches) =>
            ValidateClientKeepsWorking(Client, () => batches.Count);

        private async Task ValidateClientKeepsWorking(BlazorClient Client, Func<int> countAccessor)
        {
            var currentBatches = countAccessor();
            await Client.ClickAsync("thecounter");

            Assert.Equal(currentBatches + 1, countAccessor());
        }
    }
}
