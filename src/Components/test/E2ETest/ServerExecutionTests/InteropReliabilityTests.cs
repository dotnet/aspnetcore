// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class InteropReliabilityTests : IClassFixture<AspNetSiteServerFixture>, IDisposable
    {
        private static readonly TimeSpan DefaultLatencyTimeout = TimeSpan.FromSeconds(30);
        private readonly AspNetSiteServerFixture _serverFixture;

        public InteropReliabilityTests(AspNetSiteServerFixture serverFixture, ITestOutputHelper output)
        {
            _serverFixture = serverFixture;
            Output = output;

            serverFixture.BuildWebHostMethod = TestServer.Program.BuildWebHost;
            CreateDefaultConfiguration();
        }

        public BlazorClient Client { get; set; }
        public ITestOutputHelper Output { get; set; }
        private IList<Batch> Batches { get; set; } = new List<Batch>();
        private List<DotNetCompletion> DotNetCompletions = new List<DotNetCompletion>();
        private List<JSInteropCall> JSInteropCalls = new List<JSInteropCall>();
        private IList<string> Errors { get; set; } = new List<string>();
        private ConcurrentQueue<LogMessage> Logs { get; set; } = new ConcurrentQueue<LogMessage>();

        public TestSink TestSink { get; set; }

        private void CreateDefaultConfiguration()
        {
            Client = new BlazorClient() { DefaultLatencyTimeout = DefaultLatencyTimeout };
            Client.RenderBatchReceived += (id, data) => Batches.Add(new Batch(id, data));
            Client.DotNetInteropCompletion += (method) => DotNetCompletions.Add(new DotNetCompletion(method));
            Client.JSInterop += (asyncHandle, identifier, argsJson) => JSInteropCalls.Add(new JSInteropCall(asyncHandle, identifier, argsJson));
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
        public async Task CannotInvokeNonJSInvokableMethods()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027WriteAllText\\u0027 on assembly \\u0027System.IO.FileSystem\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";
            await GoToTestComponent(Batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "System.IO.FileSystem",
                "WriteAllText",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            // Assert
            Assert.Single(DotNetCompletions, c => c.Message == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeNonExistingMethods()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027MadeUpMethod\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            await GoToTestComponent(Batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "MadeUpMethod",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            // Assert
            Assert.Single(DotNetCompletions, c => c.Message == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongNumberOfArguments()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            await GoToTestComponent(Batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new[] { _serverFixture.RootUri }));

            // Assert
            Assert.Single(DotNetCompletions, c => c.Message == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyAssemblyName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            await GoToTestComponent(Batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            // Assert
            Assert.Single(DotNetCompletions, c => c.Message == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyMethodName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            await GoToTestComponent(Batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            // Assert
            Assert.Single(DotNetCompletions, c => c.Message == expectedError);

            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongReferenceId()
        {
            // Arrange
            var expectedDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027Reverse\\u0027 on assembly \\u0027\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            await GoToTestComponent(Batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(DotNetCompletions, c => c.Message == expectedDotNetObjectRef);

            await Client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                1,
                JsonSerializer.Serialize(Array.Empty<object>()));

            // Assert
            Assert.Single(DotNetCompletions, c => c.Message == "[\"1\",true,\"tnatropmI\"]");

            await Client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                3, // non existing ref
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(DotNetCompletions, c => c.Message == expectedError);
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

            await GoToTestComponent(Batches);

            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(DotNetCompletions, c => c.Message == expectedImportantDotNetObjectRef);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "ReceiveTrivial",
                null,
                JsonSerializer.Serialize(new object[] { new { __dotNetObject = 1 } }));

            // Assert
            Assert.Single(DotNetCompletions, c => c.Message == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task ContinuesWorkingAfterInvalidAsyncReturnCallback()
        {
            // Arrange
            var expectedError = "An exception occurred executing JS interop: The JSON value could not be converted to System.Int32. Path: $ | LineNumber: 0 | BytePositionInLine: 3.. See InnerException for more details.";

            await GoToTestComponent(Batches);

            // Act
            await Client.ClickAsync("triggerjsinterop-malformed");

            var call = JSInteropCalls.FirstOrDefault(call => call.Identifier == "sendMalformedCallbackReturn");
            Assert.NotEqual(default, call);

            var id = call.AsyncHandle;
            await Client.HubConnection.InvokeAsync(
                "EndInvokeJSFromDotNet",
                id,
                true,
                $"[{id}, true, \"{{\"]");

            var text = Assert.Single(
                Client.FindElementById("errormessage-malformed").Children.OfType<TextNode>(),
                e => expectedError == e.TextContent);

            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task JSInteropCompletionSuccess()
        {
            // Arrange
            await GoToTestComponent(Batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name));

            // Act
            await Client.ClickAsync("triggerjsinterop-success");

            var call = JSInteropCalls.FirstOrDefault(call => call.Identifier == "sendSuccessCallbackReturn");
            Assert.NotEqual(default, call);

            var id = call.AsyncHandle;
            await Client.HubConnection.InvokeAsync(
                "EndInvokeJSFromDotNet",
                id++,
                true,
                $"[{id}, true, null]");

            Assert.Single(
                Client.FindElementById("errormessage-success").Children.OfType<TextNode>(),
                e => "" == e.TextContent);

            Assert.Contains((LogLevel.Debug, "EndInvokeJSSucceeded"), logEvents);
        }

        [Fact]
        public async Task JSInteropThrowsInUserCode()
        {
            // Arrange
            await GoToTestComponent(Batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name));

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

            Assert.Contains((LogLevel.Debug, "EndInvokeJSFailed"), logEvents);

            Assert.DoesNotContain(logEvents, m => m.logLevel > LogLevel.Information);

            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task MalformedJSInteropCallbackDisposesCircuit()
        {
            // Arrange
            await GoToTestComponent(Batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name));

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

            Assert.Contains((LogLevel.Debug, "EndInvokeDispatchException"), logEvents);

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

            await GoToTestComponent(Batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                "[ \"invalidPayload\"}");

            // Assert
            Assert.Single(DotNetCompletions, c => c.Message == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithMalformedArgumentPayload()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027ReceiveTrivial\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.DetailedErrors\\u0027\"]";

            await GoToTestComponent(Batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "ReceiveTrivial",
                null,
                "[ { \"data\": {\"}} ]");

            // Assert
            Assert.Single(DotNetCompletions, c => c.Message == expectedError);
            await ValidateClientKeepsWorking(Client, Batches);
        }

        [Fact]
        public async Task DispatchingEventsWithInvalidPayloadsShutsDownCircuitGracefully()
        {
            // Arrange
            await GoToTestComponent(Batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name));

            // Act
            await Client.ExpectCircuitError(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                "DispatchBrowserEvent",
                null,
                null);
            });

            Assert.Contains(
                (LogLevel.Debug, "DispatchEventFailedToParseEventData"),
                logEvents);

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
            await GoToTestComponent(Batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name));

            // Act
            await Client.ExpectCircuitError(async () =>
            {
                await Client.HubConnection.InvokeAsync(
                "DispatchBrowserEvent",
                "{Invalid:{\"payload}",
                "{}");
            });

            Assert.Contains(
                (LogLevel.Debug, "DispatchEventFailedToParseEventData"),
                logEvents);

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
            await GoToTestComponent(Batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string eventIdName, Exception exception)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name, wc.Exception));

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
                logEvents,
                e => e.eventIdName == "DispatchEventFailedToParseEventData" && e.logLevel == LogLevel.Debug &&
                     e.exception.Message == "There was an error parsing the event arguments. EventId: '6'.");

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
            await GoToTestComponent(Batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string eventIdName, Exception exception)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name, wc.Exception));

            // Act
            var mouseEventArgs = new UIMouseEventArgs()
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
                logEvents,
                e => e.eventIdName == "DispatchEventFailedToDispatchEvent" && e.logLevel == LogLevel.Debug &&
                     e.exception is ArgumentException ae && ae.Message.Contains("There is no event handler associated with this event. EventId: '1'."));

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
            await GoToTestComponent(Batches);
            var sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            var logEvents = new List<(LogLevel logLevel, string eventIdName, Exception exception)>();
            sink.MessageLogged += (wc) => logEvents.Add((wc.LogLevel, wc.EventId.Name, wc.Exception));

            // Act
            await Client.ClickAsync("event-handler-throw-sync", expectRenderBatch: false);

            Assert.Contains(
                logEvents,
                e => LogLevel.Error == e.logLevel &&
                    "CircuitUnhandledException" == e.eventIdName &&
                    "Handler threw an exception" == e.exception.Message);

            // Now if you try to click again, you will get *forcibly* disconnected for trying to talk to
            // a circuit that's gone.
            await Client.ExpectCircuitErrorAndDisconnect(async () =>
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => Client.ClickAsync("event-handler-throw-sync", expectRenderBatch: true));
            });
        }

        private Task ValidateClientKeepsWorking(BlazorClient Client, IList<Batch> batches) =>
            ValidateClientKeepsWorking(Client, () => batches.Count);

        private async Task ValidateClientKeepsWorking(BlazorClient Client, Func<int> countAccessor)
        {
            var currentBatches = countAccessor();
            await Client.ClickAsync("thecounter");

            Assert.Equal(currentBatches + 1, countAccessor());
        }

        private async Task GoToTestComponent(IList<Batch> batches)
        {
            var rootUri = _serverFixture.RootUri;
            Assert.True(await Client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");
            Assert.Single(batches);

            await Client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            Assert.Equal(2, batches.Count);
        }

        public void Dispose()
        {
            TestSink.MessageLogged -= LogMessages;
        }

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

        private class DotNetCompletion
        {
            public DotNetCompletion(string message)
            {
                Message = message;
            }

            public string Message { get; }
        }

        private class JSInteropCall
        {
            public JSInteropCall(int asyncHandle, string identifier, string argsJson)
            {
                AsyncHandle = asyncHandle;
                Identifier = identifier;
                ArgsJson = argsJson;
            }

            public int AsyncHandle { get; }
            public string Identifier { get; }
            public string ArgsJson { get; }
        }
    }
}
