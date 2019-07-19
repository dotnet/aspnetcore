// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class InteropReliabilityTests : IClassFixture<AspNetSiteServerFixture>
    {
        private static readonly TimeSpan DefaultLatencyTimeout = TimeSpan.FromMilliseconds(500);
        private readonly AspNetSiteServerFixture _serverFixture;

        public InteropReliabilityTests(AspNetSiteServerFixture serverFixture)
        {
            serverFixture.BuildWebHostMethod = TestServer.Program.BuildWebHost;
            _serverFixture = serverFixture;
        }

        public BlazorClient Client { get; set; } = new BlazorClient() { DefaultLatencyTimeout = DefaultLatencyTimeout };

        [Fact]
        public async Task CannotInvokeNonJSInvokableMethods()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027WriteAllText\\u0027 on assembly \\u0027System.IO.FileSystem\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";
            var (_, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "System.IO.FileSystem",
                "WriteAllText",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            // Assert
            Assert.Single(dotNetCompletions, expectedError);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeNonExistingMethods()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027MadeUpMethod\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";
            var (_, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "MadeUpMethod",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            // Assert
            Assert.Single(dotNetCompletions, expectedError);
            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongNumberOfArguments()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";
            var (_, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new[] { _serverFixture.RootUri }));

            // Assert
            Assert.Single(dotNetCompletions, expectedError);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyAssemblyName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";
            var (_, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            // Assert
            Assert.Single(dotNetCompletions, expectedError);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyMethodName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";
            var (_, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            // Assert
            Assert.Single(dotNetCompletions, expectedError);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongReferenceId()
        {
            // Arrange
            var expectedDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027Reverse\\u0027 on assembly \\u0027\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";
            var (_, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(dotNetCompletions, expectedDotNetObjectRef);

            await Client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                1,
                JsonSerializer.Serialize(Array.Empty<object>()));

            // Assert
            Assert.Single(dotNetCompletions, "[\"1\",true,\"tnatropmI\"]");

            await Client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                3, // non existing ref
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(dotNetCompletions, expectedError);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWrongReferenceIdType()
        {
            // Arrange
            var expectedImportantDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027ReceiveTrivial\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            var (interopCalls, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(dotNetCompletions, expectedImportantDotNetObjectRef);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "ReceiveTrivial",
                null,
                JsonSerializer.Serialize(new object[] { new { __dotNetObject = 1 } }));

            // Assert
            Assert.Single(dotNetCompletions, expectedError);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task ContinuesWorkingAfterInvalidAsyncReturnCallback()
        {
            // Arrange
            var expectedError = "An exception occurred executing JS interop: The JSON value could not be converted to System.Int32. Path: $ | LineNumber: 0 | BytePositionInLine: 3.. See InnerException for more details.";

            var (interopCalls, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.ClickAsync("triggerjsinterop-malformed");

            Assert.Single(interopCalls, (4, "sendMalformedCallbackReturn", (string)null));

            await Client.HubConnection.InvokeAsync(
                "EndInvokeJSFromDotNet",
                4,
                true,
                "[4, true, \"{\"]");

            var text = Assert.Single(
                Client.FindElementById("errormessage-malformed").Children.OfType<TextNode>(),
                e => expectedError == e.TextContent);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task LogsJSInteropCompletionsCallbacksAndContinuesWorkingInAllSituations()
        {
            // Arrange

            var (interopCalls, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name));
            // Act
            await Client.ClickAsync("triggerjsinterop-malformed");

            Assert.Single(interopCalls, (4, "sendMalformedCallbackReturn", (string)null));

            await Client.HubConnection.InvokeAsync(
                "EndInvokeJSFromDotNet",
                4,
                true,
                "[4, true, }");

            // A completely malformed payload like the one above never gets to the application.
            Assert.Single(
                Client.FindElementById("errormessage-malformed").Children.OfType<TextNode>(),
                e => "" == e.TextContent);

            Assert.Contains((LogLevel.Debug, "EndInvokeDispatchException"), logEvents);

            await Client.ClickAsync("triggerjsinterop-success");
            await Client.HubConnection.InvokeAsync(
                "EndInvokeJSFromDotNet",
                5,
                true,
                "[5, true, null]");

            Assert.Single(
                Client.FindElementById("errormessage-success").Children.OfType<TextNode>(),
                e => "" == e.TextContent);

            Assert.Contains((LogLevel.Debug, "EndInvokeJSSucceeded"), logEvents);

            await Client.ClickAsync("triggerjsinterop-failure");
            await Client.HubConnection.InvokeAsync(
                "EndInvokeJSFromDotNet",
                6,
                false,
                "[6, false, \"There was an error invoking sendFailureCallbackReturn\"]");

            Assert.Single(
                Client.FindElementById("errormessage-failure").Children.OfType<TextNode>(),
                e => "There was an error invoking sendFailureCallbackReturn" == e.TextContent);

            Assert.Contains((LogLevel.Debug, "EndInvokeJSFailed"), logEvents);

            Assert.DoesNotContain(logEvents, m => m.logLevel > LogLevel.Information);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithInvalidArgumentsPayload()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            var (_, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                "[ \"invalidPayload\"}");

            // Assert
            Assert.Single(dotNetCompletions, expectedError);
            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithMalformedArgumentPayload()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027ReceiveTrivial\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            var (_, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "ReceiveTrivial",
                null,
                "[ { \"data\": {\"}} ]");

            // Assert
            Assert.Single(dotNetCompletions, expectedError);
            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task DispatchingEventsWithInvalidPayloadsDoesNotCrashTheCircuit()
        {
            // Arrange
            var (interopCalls, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name));

            // Act
            await Client.HubConnection.InvokeAsync(
                "DispatchBrowserEvent",
                null,
                null);

            Assert.Contains(
                (LogLevel.Debug, "DispatchEventFailedToParseEventDescriptor"),
                logEvents);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task DispatchingEventsWithInvalidUIEventArgs()
        {
            // Arrange
            var (interopCalls, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name));

            // Act
            var browserDescriptor = new RendererRegistryEventDispatcher.BrowserEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = 6,
                EventArgsType = "mouse",
            };

            await Client.HubConnection.InvokeAsync(
                "DispatchBrowserEvent",
                JsonSerializer.Serialize(browserDescriptor, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                "{Invalid:{\"payload}");

            Assert.Contains(
                (LogLevel.Debug, "DispatchEventFailedToDispatchEvent"),
                logEvents);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task DispatchingEventsWithInvalidEventHandlerId()
        {
            // Arrange
            var (interopCalls, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string eventIdName, Exception exception)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name, wc.Exception));

            // Act
            var mouseEventArgs = new UIMouseEventArgs()
            {
                Type = "click",
                Detail = 1
            };
            var browserDescriptor = new RendererRegistryEventDispatcher.BrowserEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = 1,
                EventArgsType = "mouse",
            };

            await Client.HubConnection.InvokeAsync(
                "DispatchBrowserEvent",
                JsonSerializer.Serialize(browserDescriptor, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                JsonSerializer.Serialize(mouseEventArgs, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

            Assert.Contains(
                logEvents,
                e => e.eventIdName == "DispatchEventFailedToDispatchEvent" && e.logLevel == LogLevel.Debug &&
                     e.exception is ArgumentException ae && ae.Message.Contains("There is no event handler with ID 1"));

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task DispatchingEventThroughJSInterop()
        {
            // Arrange
            var (interopCalls, dotNetCompletions, batches) = ConfigureClient();
            await GoToTestComponent(batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string eventIdName)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name));

            // Act
            var mouseEventArgs = new UIMouseEventArgs()
            {
                Type = "click",
                Detail = 1
            };
            var browserDescriptor = new RendererRegistryEventDispatcher.BrowserEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = 1,
                EventArgsType = "mouse",
            };

            var serializerOptions = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var uiArgs = JsonSerializer.Serialize(mouseEventArgs, serializerOptions);

            await Assert.ThrowsAsync<TaskCanceledException>(() => Client.InvokeDotNetMethod(
                0,
                "Microsoft.AspNetCore.Components.Web",
                "DispatchEvent",
                null,
                JsonSerializer.Serialize(new object[] { browserDescriptor, uiArgs }, serializerOptions)));

            Assert.Contains(
                (LogLevel.Debug, "DispatchEventThroughJSInterop"),
                logEvents);

            await ValidateClientKeepsWorking(Client, batches);
        }

        private Task ValidateClientKeepsWorking(BlazorClient Client, List<(int, int, byte[])> batches) =>
            ValidateClientKeepsWorking(Client, () => batches.Count);

        private async Task ValidateClientKeepsWorking(BlazorClient Client, Func<int> countAccessor)
        {
            var currentBatches = countAccessor();
            await Client.ClickAsync("thecounter");

            Assert.Equal(currentBatches + 1, countAccessor());
        }

        private async Task GoToTestComponent(List<(int, int, byte[])> batches)
        {
            var rootUri = _serverFixture.RootUri;
            Assert.True(await Client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");
            Assert.Single(batches);

            await Client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            Assert.Equal(2, batches.Count);
        }

        private (List<(int, string, string)>, List<string>, List<(int, int, byte[])>) ConfigureClient()
        {
            var interopCalls = new List<(int, string, string)>();
            Client.JSInterop += (int arg1, string arg2, string arg3) => interopCalls.Add((arg1, arg2, arg3));
            var batches = new List<(int, int, byte[])>();
            Client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));
            var endInvokeDotNetCompletions = new List<string>();
            Client.DotNetInteropCompletion += (completion) => endInvokeDotNetCompletions.Add(completion);
            return (interopCalls, endInvokeDotNetCompletions, batches);
        }
    }
}
