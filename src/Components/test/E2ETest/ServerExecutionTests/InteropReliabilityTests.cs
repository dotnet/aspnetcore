// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
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
                "\"There was an exception invoking \\u0027WriteAllText\\u0027 on assembly \\u0027System.IO.FileSystem\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "System.IO.FileSystem",
                "WriteAllText",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            // Assert
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError));

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeNonExistingMethods()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027MadeUpMethod\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "MadeUpMethod",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            // Assert
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError));
            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongNumberOfArguments()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new[] { _serverFixture.RootUri }));

            // Assert
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError));

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyAssemblyName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            // Assert
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError));

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyMethodName()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            // Assert
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError));

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongReferenceId()
        {
            // Arrange
            var expectedDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027Reverse\\u0027 on assembly \\u0027\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";
            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedDotNetObjectRef));

            await Client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                1,
                JsonSerializer.Serialize(Array.Empty<object>()));

            // Assert
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", "[\"1\",true,\"tnatropmI\"]"));

            await Client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                3, // non existing ref
                JsonSerializer.Serialize(Array.Empty<object>()));
            
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError));

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWrongReferenceIdType()
        {
            // Arrange
            var expectedImportantDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027ReceiveTrivial\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedImportantDotNetObjectRef));

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "ReceiveTrivial",
                null,
                JsonSerializer.Serialize(new object[] { new { __dotNetObject = 1 } }));

            // Assert
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError));

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task ContinuesWorkingAfterInvalidAsyncReturnCallback()
        {
            // Arrange
            var expectedError = "An exception occurred executing JS interop: The JSON value could not be converted to System.Int32. Path: $ | LineNumber: 0 | BytePositionInLine: 3.. See InnerException for more details.";

            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.ClickAsync("triggerjsinterop");

            Assert.Single(interopCalls, (4, "sendMalformedCallbackReturn", (string)null));

            await Client.InvokeDotNetMethod(
                0,
                "Microsoft.JSInterop",
                "DotNetDispatcher.EndInvoke",
                null,
                "[4, true, \"{\"]");

            var text = Assert.Single(
                Client.FindElementById("errormessage").Children.OfType<TextNode>(),
                e => expectedError == e.TextContent);

            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithInvalidArgumentsPayload()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027NotifyLocationChanged\\u0027 on assembly \\u0027Microsoft.AspNetCore.Components.Server\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                "[ \"invalidPayload\"}");

            // Assert
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError));
            await ValidateClientKeepsWorking(Client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithMalformedArgumentPayload()
        {
            // Arrange
            var expectedError = "[\"1\"," +
                "false," +
                "\"There was an exception invoking \\u0027ReceiveTrivial\\u0027 on assembly \\u0027BasicTestApp\\u0027. For more details turn on detailed exceptions in \\u0027CircuitOptions.JSInteropDetailedErrors\\u0027\"]";

            var (interopCalls, batches) = ConfigureClient();
            await GoToTestComponent(batches);

            // Act
            await Client.InvokeDotNetMethod(
                "1",
                "BasicTestApp",
                "ReceiveTrivial",
                null,
                "[ { \"data\": {\"}} ]");

            // Assert
            Assert.Single(interopCalls, (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError));
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

        private (List<(int, string, string)>, List<(int, int, byte[])>) ConfigureClient()
        {
            var interopCalls = new List<(int, string, string)>();
            Client.JSInterop += (int arg1, string arg2, string arg3) => interopCalls.Add((arg1, arg2, arg3));
            var batches = new List<(int, int, byte[])>();
            Client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));
            return (interopCalls, batches);
        }
    }
}
