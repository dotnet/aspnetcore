// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop.Infrastructure;
using Xunit;

namespace Microsoft.JSInterop
{
    public class JSRuntimeTest
    {
        [Fact]
        public void DispatchesAsyncCallsWithDistinctAsyncHandles()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act
            runtime.InvokeAsync<object>("test identifier 1", "arg1", 123, true);
            runtime.InvokeAsync<object>("test identifier 2", "some other arg");

            // Assert
            Assert.Collection(runtime.BeginInvokeCalls,
                call =>
                {
                    Assert.Equal("test identifier 1", call.Identifier);
                    Assert.Equal("[\"arg1\",123,true]", call.ArgsJson);
                },
                call =>
                {
                    Assert.Equal("test identifier 2", call.Identifier);
                    Assert.Equal("[\"some other arg\"]", call.ArgsJson);
                    Assert.NotEqual(runtime.BeginInvokeCalls[0].AsyncHandle, call.AsyncHandle);
                });
        }

        [Fact]
        public async Task InvokeAsync_CancelsAsyncTask_AfterDefaultTimeout()
        {
            // Arrange
            var runtime = new TestJSRuntime();
            runtime.DefaultTimeout = TimeSpan.FromSeconds(1);

            // Act
            var task = runtime.InvokeAsync<object>("test identifier 1", "arg1", 123, true);

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        [Fact]
        public void InvokeAsync_CompletesSuccessfullyBeforeTimeout()
        {
            // Arrange
            var runtime = new TestJSRuntime();
            runtime.DefaultTimeout = TimeSpan.FromSeconds(10);
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("null"));

            // Act
            var task = runtime.InvokeAsync<object>("test identifier 1", "arg1", 123, true);

            runtime.EndInvokeJS(2, succeeded: true, ref reader);

            Assert.True(task.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task InvokeAsync_CancelsAsyncTasksWhenCancellationTokenFires()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var runtime = new TestJSRuntime();

            // Act
            var task = runtime.InvokeAsync<object>("test identifier 1", cts.Token, new object[] { "arg1", 123, true });

            cts.Cancel();

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        [Fact]
        public async Task InvokeAsync_DoesNotStartWorkWhenCancellationHasBeenRequested()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var runtime = new TestJSRuntime();

            // Act
            var task = runtime.InvokeAsync<object>("test identifier 1", cts.Token, new object[] { "arg1", 123, true });

            cts.Cancel();

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
            Assert.Empty(runtime.BeginInvokeCalls);
        }

        [Fact]
        public void CanCompleteAsyncCallsAsSuccess()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act/Assert: Tasks not initially completed
            var unrelatedTask = runtime.InvokeAsync<string>("unrelated call", Array.Empty<object>());
            var task = runtime.InvokeAsync<string>("test identifier", Array.Empty<object>());
            Assert.False(unrelatedTask.IsCompleted);
            Assert.False(task.IsCompleted);
            var bytes = Encoding.UTF8.GetBytes("\"my result\"");
            var reader = new Utf8JsonReader(bytes);

            // Act/Assert: Task can be completed
            runtime.EndInvokeJS(
                runtime.BeginInvokeCalls[1].AsyncHandle,
                /* succeeded: */ true,
                ref reader);
            Assert.False(unrelatedTask.IsCompleted);
            Assert.True(task.IsCompleted);
            Assert.Equal("my result", task.Result);
        }

        [Fact]
        public void CanCompleteAsyncCallsWithComplexType()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            var task = runtime.InvokeAsync<TestPoco>("test identifier", Array.Empty<object>());
            var bytes = Encoding.UTF8.GetBytes("{\"id\":10, \"name\": \"Test\"}");
            var reader = new Utf8JsonReader(bytes);

            // Act/Assert: Task can be completed
            runtime.EndInvokeJS(
                runtime.BeginInvokeCalls[0].AsyncHandle,
                /* succeeded: */ true,
                ref reader);
            Assert.True(task.IsCompleted);
            var poco = task.Result;
            Assert.Equal(10, poco.Id);
            Assert.Equal("Test", poco.Name);
        }

        [Fact]
        public void CanCompleteAsyncCallsWithComplexTypeUsingPropertyCasing()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            var task = runtime.InvokeAsync<TestPoco>("test identifier", Array.Empty<object>());
            var bytes = Encoding.UTF8.GetBytes("{\"Id\":10, \"Name\": \"Test\"}");
            var reader = new Utf8JsonReader(bytes);
            reader.Read();

            // Act/Assert: Task can be completed
            runtime.EndInvokeJS(
                runtime.BeginInvokeCalls[0].AsyncHandle,
                /* succeeded: */ true,
                ref reader);
            Assert.True(task.IsCompleted);
            var poco = task.Result;
            Assert.Equal(10, poco.Id);
            Assert.Equal("Test", poco.Name);
        }

        [Fact]
        public void CanCompleteAsyncCallsAsFailure()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act/Assert: Tasks not initially completed
            var unrelatedTask = runtime.InvokeAsync<string>("unrelated call", Array.Empty<object>());
            var task = runtime.InvokeAsync<string>("test identifier", Array.Empty<object>());
            Assert.False(unrelatedTask.IsCompleted);
            Assert.False(task.IsCompleted);
            var bytes = Encoding.UTF8.GetBytes("\"This is a test exception\"");
            var reader = new Utf8JsonReader(bytes);
            reader.Read();

            // Act/Assert: Task can be failed
            runtime.EndInvokeJS(
                runtime.BeginInvokeCalls[1].AsyncHandle,
                /* succeeded: */ false,
                ref reader);
            Assert.False(unrelatedTask.IsCompleted);
            Assert.True(task.IsCompleted);

            var exception = Assert.IsType<AggregateException>(task.AsTask().Exception);
            var jsException = Assert.IsType<JSException>(exception.InnerException);
            Assert.Equal("This is a test exception", jsException.Message);
        }

        [Fact]
        public Task CanCompleteAsyncCallsWithErrorsDuringDeserialization()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act/Assert: Tasks not initially completed
            var unrelatedTask = runtime.InvokeAsync<string>("unrelated call", Array.Empty<object>());
            var task = runtime.InvokeAsync<int>("test identifier", Array.Empty<object>());
            Assert.False(unrelatedTask.IsCompleted);
            Assert.False(task.IsCompleted);
            var bytes = Encoding.UTF8.GetBytes("Not a string");
            var reader = new Utf8JsonReader(bytes);

            // Act/Assert: Task can be failed
            runtime.EndInvokeJS(
                runtime.BeginInvokeCalls[1].AsyncHandle,
                /* succeeded: */ true,
                ref reader);
            Assert.False(unrelatedTask.IsCompleted);

            return AssertTask();

            async Task AssertTask()
            {
                var jsException = await Assert.ThrowsAsync<JSException>(async () => await task);
                Assert.IsAssignableFrom<JsonException>(jsException.InnerException);
            }
        }

        [Fact]
        public Task CompletingSameAsyncCallMoreThanOnce_IgnoresSecondResultAsync()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act/Assert
            var task = runtime.InvokeAsync<string>("test identifier", Array.Empty<object>());
            var asyncHandle = runtime.BeginInvokeCalls[0].AsyncHandle;
            var firstReader = new Utf8JsonReader(Encoding.UTF8.GetBytes("\"Some data\""));
            var secondReader = new Utf8JsonReader(Encoding.UTF8.GetBytes("\"Exception\""));

            runtime.EndInvokeJS(asyncHandle, true, ref firstReader);
            runtime.EndInvokeJS(asyncHandle, false, ref secondReader);

            return AssertTask();

            async Task AssertTask()
            {
                var result = await task;
                Assert.Equal("Some data", result);
            }
        }

        [Fact]
        public void SerializesDotNetObjectWrappersInKnownFormat()
        {
            // Arrange
            var runtime = new TestJSRuntime();
            var obj1 = new object();
            var obj2 = new object();
            var obj3 = new object();

            // Act
            // Showing we can pass the DotNetObject either as top-level args or nested
            var obj1Ref = DotNetObjectReference.Create(obj1);
            var obj1DifferentRef = DotNetObjectReference.Create(obj1);
            runtime.InvokeAsync<object>("test identifier",
                obj1Ref,
                new Dictionary<string, object>
                {
                    { "obj2", DotNetObjectReference.Create(obj2) },
                    { "obj3", DotNetObjectReference.Create(obj3) },
                    { "obj1SameRef", obj1Ref },
                    { "obj1DifferentRef", obj1DifferentRef },
                });

            // Assert: Serialized as expected
            var call = runtime.BeginInvokeCalls.Single();
            Assert.Equal("test identifier", call.Identifier);
            Assert.Equal("[{\"__dotNetObject\":1},{\"obj2\":{\"__dotNetObject\":2},\"obj3\":{\"__dotNetObject\":3},\"obj1SameRef\":{\"__dotNetObject\":1},\"obj1DifferentRef\":{\"__dotNetObject\":4}}]", call.ArgsJson);

            // Assert: Objects were tracked
            Assert.Same(obj1Ref, runtime.GetObjectReference(1));
            Assert.Same(obj1, obj1Ref.Value);
            Assert.NotSame(obj1Ref, runtime.GetObjectReference(2));
            Assert.Same(obj2, runtime.GetObjectReference(2).Value);
            Assert.Same(obj3, runtime.GetObjectReference(3).Value);
            Assert.Same(obj1, runtime.GetObjectReference(4).Value);
        }

        [Fact]
        public void CanSanitizeDotNetInteropExceptions()
        {
            // Arrange
            var expectedMessage = "An error ocurred while invoking '[Assembly]::Method'. Swapping to 'Development' environment will " +
                "display more detailed information about the error that occurred.";

            string GetMessage(DotNetInvocationInfo info) => $"An error ocurred while invoking '[{info.AssemblyName}]::{info.MethodIdentifier}'. Swapping to 'Development' environment will " +
                "display more detailed information about the error that occurred.";

            var runtime = new TestJSRuntime()
            {
                OnDotNetException = (invocationInfo) => new JSError { Message = GetMessage(invocationInfo) }
            };

            var exception = new Exception("Some really sensitive data in here");
            var invocation = new DotNetInvocationInfo("Assembly", "Method", 0, "0");
            var result = new DotNetInvocationResult(exception, default);

            // Act
            runtime.EndInvokeDotNet(invocation, result);

            // Assert
            var call = runtime.EndInvokeDotNetCalls.Single();
            Assert.Equal("0", call.CallId);
            Assert.False(call.Success);
            var jsError = Assert.IsType<JSError>(call.ResultOrError);
            Assert.Equal(expectedMessage, jsError.Message);
        }

        private class JSError
        {
            public string Message { get; set; }
        }

        private class TestPoco
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        class TestJSRuntime : JSRuntime
        {
            public List<BeginInvokeAsyncArgs> BeginInvokeCalls = new List<BeginInvokeAsyncArgs>();
            public List<EndInvokeDotNetArgs> EndInvokeDotNetCalls = new List<EndInvokeDotNetArgs>();

            public TimeSpan? DefaultTimeout
            {
                set
                {
                    base.DefaultAsyncTimeout = value;
                }
            }

            public class BeginInvokeAsyncArgs
            {
                public long AsyncHandle { get; set; }
                public string Identifier { get; set; }
                public string ArgsJson { get; set; }
            }

            public class EndInvokeDotNetArgs
            {
                public string CallId { get; set; }
                public bool Success { get; set; }
                public object ResultOrError { get; set; }
            }

            public Func<DotNetInvocationInfo, object> OnDotNetException { get; set; }

            protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            {
                var resultOrError = invocationResult.Success ? invocationResult.Result : invocationResult.Exception;
                if (OnDotNetException != null && !invocationResult.Success)
                {
                    resultOrError = OnDotNetException(invocationInfo);
                }

                EndInvokeDotNetCalls.Add(new EndInvokeDotNetArgs
                {
                    CallId = invocationInfo.CallId,
                    Success = invocationResult.Success,
                    ResultOrError = resultOrError,
                });
            }

            protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
            {
                BeginInvokeCalls.Add(new BeginInvokeAsyncArgs
                {
                    AsyncHandle = asyncHandle,
                    Identifier = identifier,
                    ArgsJson = argsJson,
                });
            }
        }
    }
}
