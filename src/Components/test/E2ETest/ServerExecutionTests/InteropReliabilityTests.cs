using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BasicTestApp;
using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests
{
    public class InteropReliabilityTests : BasicTestAppTestBase
    {
        private const int DefaultLatencyTimeout = 500;

        public InteropReliabilityTests(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture,
                  serverFixture.WithServerExecution().WithAdditionalArguments(new string[] { "--" + WebHostDefaults.DetailedErrorsKey, "true" }),
                  output)
        {
        }

        [Fact]
        public async Task CannotInvokeNonJSInvokableMethods()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027WriteAllText\\u0027 on assembly \\u0027System.IO.FileSystem\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            var rootUri = _serverFixture.RootUri;
            var initialRender = client.PrepareForNextBatch();
            Assert.True(await client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");

            await initialRender;
            Assert.Single(batches);

            var selectComponentRender = client.PrepareForNextBatch();
            await client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            await selectComponentRender;
            Assert.Equal(2, batches.Count);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "System.IO.FileSystem",
                "WriteAllText",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            await Task.Delay(1000);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError) == element;
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeNonExistingMethods()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027MadeUpMethod\\u0027 on assembly \\u0027ComponentsApp.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            var rootUri = _serverFixture.RootUri;
            var initialRender = client.PrepareForNextBatch();
            Assert.True(await client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");

            await initialRender;
            Assert.Single(batches);

            var selectComponentRender = client.PrepareForNextBatch();
            await client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            await selectComponentRender;
            Assert.Equal(2, batches.Count);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "ComponentsApp.Server",
                "MadeUpMethod",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError) == element;
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongNumberOfArguments()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            var rootUri = _serverFixture.RootUri;
            var initialRender = client.PrepareForNextBatch();
            Assert.True(await client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");

            await initialRender;
            Assert.Single(batches);

            var selectComponentRender = client.PrepareForNextBatch();
            await client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            await selectComponentRender;
            Assert.Equal(2, batches.Count);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new[] { _serverFixture.RootUri }));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError) == element;
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyAssemblyName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            var rootUri = _serverFixture.RootUri;
            var initialRender = client.PrepareForNextBatch();
            Assert.True(await client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");

            await initialRender;
            Assert.Single(batches);

            var selectComponentRender = client.PrepareForNextBatch();
            await client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            await selectComponentRender;
            Assert.Equal(2, batches.Count);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            await Task.Delay(DefaultLatencyTimeout);
            var (callId, functionName, arguments) = Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError) == element;
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyMethodName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            var rootUri = _serverFixture.RootUri;
            var initialRender = client.PrepareForNextBatch();
            Assert.True(await client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");

            await initialRender;
            Assert.Single(batches);

            var selectComponentRender = client.PrepareForNextBatch();
            await client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            await selectComponentRender;
            Assert.Equal(2, batches.Count);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError) == element;
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact(Skip = "Pending changes from extensions")]
        public async Task CannotInvokeJSInvokableMethodsWithWrongReferenceId()
        {
            // Arrange
            var expectedDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";

            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            var rootUri = _serverFixture.RootUri;
            var initialRender = client.PrepareForNextBatch();
            Assert.True(await client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");

            await initialRender;
            Assert.Single(batches);

            var selectComponentRender = client.PrepareForNextBatch();
            await client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            await selectComponentRender;
            Assert.Equal(2, batches.Count);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "ComponentsApp.Server",
                "CreateInformation",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedDotNetObjectRef);
            });

            client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                1,
                JsonSerializer.Serialize(Array.Empty<object>()));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", "[\"1\",true,\"egasseM\"]");
            });

            client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                3, // non existing ref
                JsonSerializer.Serialize(Array.Empty<object>()));

            await Task.Delay(5000);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", "[\"1\",true,\"egasseM\"]");
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWronReferenceIdType()
        {
            // Arrange
            var expectedImportantDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027ReceiveTrivial\\u0027 on assembly \\u0027ComponentsApp.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            var rootUri = _serverFixture.RootUri;
            var initialRender = client.PrepareForNextBatch();
            Assert.True(await client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");

            await initialRender;
            Assert.Single(batches);

            var selectComponentRender = client.PrepareForNextBatch();
            await client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            await selectComponentRender;
            Assert.Equal(2, batches.Count);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "ComponentsApp.Server",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedImportantDotNetObjectRef);
            });

            client.InvokeDotNetMethod(
                "1",
                "ComponentsApp.Server",
                "ReceiveTrivial",
                null,
                JsonSerializer.Serialize(new object[] { new { __dotNetObject = 1 } }));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError);
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task ContinuesWorkingAfterInvalidAsyncReturnCallback()
        {
            // Arrange
            var expectedError = "An exception occurred executing JS interop: The JSON value could not be converted to System.Int32. Path: $ | LineNumber: 0 | BytePositionInLine: 3.. See InnerException for more details.";
            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            var rootUri = _serverFixture.RootUri;
            var initialRender = client.PrepareForNextBatch();
            Assert.True(await client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");

            await initialRender;
            Assert.Single(batches);

            var selectComponentRender = client.PrepareForNextBatch();
            await client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            await selectComponentRender;
            Assert.Equal(2, batches.Count);

            var jsInteropTriggered = client.PrepareForNextBatch();
            await client.ClickAsync("triggerjsinterop");

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (4, "sendMalformedCallbackReturn", null);
            });

            var invalidJSInteropResponse = client.PrepareForNextBatch();
            client.InvokeDotNetMethod(
                0,
                "Microsoft.JSInterop",
                "DotNetDispatcher.EndInvoke",
                null,
                "[4, true, \"{\"]");

            await invalidJSInteropResponse;
            var text = Assert.Single(
                client.FindElementById("errormessage").Children.OfType<TextNode>(),
                e => expectedError == e.TextContent);

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithInvalidArgumentsPayload()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            var rootUri = _serverFixture.RootUri;
            var initialRender = client.PrepareForNextBatch();
            Assert.True(await client.ConnectAsync(new Uri(rootUri, "/subdir"), prerendered: false), "Couldn't connect to the app");

            await initialRender;
            Assert.Single(batches);

            var selectComponentRender = client.PrepareForNextBatch();
            await client.SelectAsync("test-selector-select", "BasicTestApp.ReliabilityComponent");
            await selectComponentRender;
            Assert.Equal(2, batches.Count);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                "[ \"invalidPayload\"}");

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError) == element;
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        private Task ValidateClientKeepsWorking(BlazorClient client, List<(int, int, byte[])> batches) =>
            ValidateClientKeepsWorking(client, () => batches.Count);

        private async Task ValidateClientKeepsWorking(BlazorClient client, Func<int> countAccessor)
        {
            var currentBatches = countAccessor();
            var nextClickRendered = client.PrepareForNextBatch();
            await client.ClickAsync("thecounter");
            await nextClickRendered;

            Assert.Equal(currentBatches + 1, countAccessor());
        }

    }
}
