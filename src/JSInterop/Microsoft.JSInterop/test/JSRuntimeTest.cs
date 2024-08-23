// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop.Implementation;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop;

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
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.Equal("my result", task.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
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
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        var poco = task.Result;
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        Debug.Assert(poco != null);
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
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        var poco = task.Result;
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        Debug.Assert(poco != null);
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
        var runtime = new TestJSRuntime();
        var exception = new Exception("Some really sensitive data in here");
        var invocation = new DotNetInvocationInfo("TestAssembly", "TestMethod", 0, "0");
        var result = new DotNetInvocationResult(exception, default);

        // Act
        runtime.EndInvokeDotNet(invocation, result);

        // Assert
        var call = runtime.EndInvokeDotNetCalls.Single();
        Assert.Equal("0", call.CallId);
        Assert.False(call.Success);

        var error = Assert.IsType<JSError>(call.ResultError);
        Assert.Same(exception, error.InnerException);
        Assert.Equal(invocation, error.InvocationInfo);
    }

    [Fact]
    public void ReceiveByteArray_AddsInitialByteArray()
    {
        // Arrange
        var runtime = new TestJSRuntime();

        var byteArray = new byte[] { 1, 5, 7 };

        // Act
        runtime.ReceiveByteArray(0, byteArray);

        // Assert
        Assert.Equal(1, runtime.ByteArraysToBeRevived.Count);
        Assert.Equal(byteArray, runtime.ByteArraysToBeRevived.Buffer[0]);
    }

    [Fact]
    public void ReceiveByteArray_AddsMultipleByteArrays()
    {
        // Arrange
        var runtime = new TestJSRuntime();

        var byteArrays = new byte[10][];
        for (var i = 0; i < 10; i++)
        {
            var byteArray = new byte[3];
            Random.Shared.NextBytes(byteArray);
            byteArrays[i] = byteArray;
        }

        // Act
        for (var i = 0; i < 10; i++)
        {
            runtime.ReceiveByteArray(i, byteArrays[i]);
        }

        // Assert
        Assert.Equal(10, runtime.ByteArraysToBeRevived.Count);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(byteArrays[i], runtime.ByteArraysToBeRevived.Buffer[i]);
        }
    }

    [Fact]
    public void ReceiveByteArray_ClearsByteArraysToBeRevivedWhenIdIsZero()
    {
        // Arrange
        var runtime = new TestJSRuntime();
        runtime.ByteArraysToBeRevived.Append(new byte[] { 1, 5, 7 });
        runtime.ByteArraysToBeRevived.Append(new byte[] { 3, 10, 15 });

        var byteArray = new byte[] { 1, 5, 7 };

        // Act
        runtime.ReceiveByteArray(0, byteArray);

        // Assert
        Assert.Equal(1, runtime.ByteArraysToBeRevived.Count);
        Assert.Equal(byteArray, runtime.ByteArraysToBeRevived.Buffer[0]);
    }

    [Fact]
    public void ReceiveByteArray_ThrowsExceptionIfUnexpectedId()
    {
        // Arrange
        var runtime = new TestJSRuntime();
        runtime.ByteArraysToBeRevived.Append(new byte[] { 1, 5, 7 });
        runtime.ByteArraysToBeRevived.Append(new byte[] { 3, 10, 15 });

        var byteArray = new byte[] { 1, 5, 7 };

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => runtime.ReceiveByteArray(7, byteArray));

        // Assert
        Assert.Equal(2, runtime.ByteArraysToBeRevived.Count);
        Assert.Equal("Element id '7' cannot be added to the byte arrays to be revived with length '2'.", ex.Message);
    }

    [Fact]
    public void BeginTransmittingStream_MultipleStreams()
    {
        // Arrange
        var runtime = new TestJSRuntime();
        var streamRef = new DotNetStreamReference(new MemoryStream());

        // Act & Assert
        for (var i = 1; i <= 10; i++)
        {
            Assert.Equal(i, runtime.BeginTransmittingStream(streamRef));
        }
    }

    [Fact]
    public async Task ReadJSDataAsStreamAsync_ThrowsNotSupportedException()
    {
        // Arrange
        var runtime = new TestJSRuntime();
        var dataReference = new JSStreamReference(runtime, 10, 10);

        // Act
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await runtime.ReadJSDataAsStreamAsync(dataReference, 10, CancellationToken.None));

        // Assert
        Assert.Equal("The current JavaScript runtime does not support reading data streams.", exception.Message);
    }

    private class JSError
    {
        public DotNetInvocationInfo InvocationInfo { get; set; }
        public Exception? InnerException { get; set; }

        public JSError(DotNetInvocationInfo invocationInfo, Exception? innerException)
        {
            InvocationInfo = invocationInfo;
            InnerException = innerException;
        }
    }

    private class TestPoco
    {
        public int Id { get; set; }

        public string? Name { get; set; }
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
            public string? Identifier { get; set; }
            public string? ArgsJson { get; set; }
        }

        public class EndInvokeDotNetArgs
        {
            public string? CallId { get; set; }
            public bool Success { get; set; }
            public string? ResultJson { get; set; }
            public JSError? ResultError { get; set; }
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
            EndInvokeDotNetCalls.Add(new EndInvokeDotNetArgs
            {
                CallId = invocationInfo.CallId,
                Success = invocationResult.Success,
                ResultJson = invocationResult.ResultJson,
                ResultError = invocationResult.Success ? null : new JSError(invocationInfo, invocationResult.Exception),
            });
        }

        protected override void BeginInvokeJS(long asyncHandle, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            BeginInvokeCalls.Add(new BeginInvokeAsyncArgs
            {
                AsyncHandle = asyncHandle,
                Identifier = identifier,
                ArgsJson = argsJson,
            });
        }

        protected internal override Task TransmitStreamAsync(long streamId, DotNetStreamReference dotNetStreamReference)
        {
            // No-op
            return Task.CompletedTask;
        }
    }
}
