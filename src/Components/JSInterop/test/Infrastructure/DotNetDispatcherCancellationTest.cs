// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Microsoft.JSInterop.Infrastructure;

public class DotNetDispatcherCancellationTest
{
    [Fact]
    public async Task CancellationReachesRunningOperationWithoutCancelingAnotherInvocation()
    {
        using var runtime = new CancellationTestRuntime();
        using var first = DotNetObjectReference.Create(new CancellationTarget());
        using var second = DotNetObjectReference.Create(new CancellationTarget());
        Begin(runtime, first, "first", nameof(CancellationTarget.Run));
        Begin(runtime, second, "second", nameof(CancellationTarget.Run));
        Assert.True(first.Value.Token.CanBeCanceled);
        Assert.False(first.Value.Operation.IsCompleted);
        Assert.False(second.Value.Operation.IsCompleted);

        Cancel(runtime, "cancel", "first");
        Assert.True((await runtime.Result("cancel")).Success);
        Assert.True(first.Value.Token.IsCancellationRequested);
        Assert.True(first.Value.Operation.IsCanceled);
        Assert.IsType<TaskCanceledException>((await runtime.Result("first")).Exception);
        Assert.False(second.Value.Token.IsCancellationRequested);
        Assert.False(second.Value.Operation.IsCompleted);

        second.Value.Gate.SetResult("second result");
        Assert.Equal("\"second result\"", (await runtime.Result("second")).ResultJson);
        AssertNoPendingInvocations(runtime);
    }

    [Fact]
    public async Task HandlerCanIgnoreCancellationAndCompleteLater()
    {
        using var runtime = new CancellationTestRuntime();
        using var target = DotNetObjectReference.Create(new CancellationTarget());
        Begin(runtime, target, "call", nameof(CancellationTarget.IgnoreCancellation));
        Cancel(runtime, "cancel", "call");
        Assert.True((await runtime.Result("cancel")).Success);
        Assert.True(target.Value.Token.IsCancellationRequested);
        Assert.False(target.Value.Operation.IsCompleted);

        target.Value.Gate.SetResult("finished");
        Assert.Equal("\"finished\"", (await runtime.Result("call")).ResultJson);
        AssertNoPendingInvocations(runtime);
    }

    [Theory]
    [InlineData(nameof(CancellationTarget.Synchronous), "42")]
    [InlineData(nameof(CancellationTarget.TaskResult), "42")]
    [InlineData(nameof(CancellationTarget.ValueTaskResult), "42")]
    [InlineData(nameof(CancellationTarget.NonGenericTask), "null")]
    [InlineData(nameof(CancellationTarget.NonGenericValueTask), "null")]
    public async Task CompletionReleasesInvocation(string method, string expectedJson)
    {
        using var runtime = new CancellationTestRuntime();
        using var target = DotNetObjectReference.Create(new CancellationTarget());
        Begin(runtime, target, "call", method);
        var result = await runtime.Result("call");
        Assert.True(result.Success, result.Exception?.ToString());
        Assert.Equal(expectedJson, result.ResultJson);
        AssertNoPendingInvocations(runtime);

        Cancel(runtime, "late", "call");
        Assert.True((await runtime.Result("late")).Success);
    }

    [Theory]
    [InlineData(nameof(CancellationTarget.ThrowSynchronously))]
    [InlineData(nameof(CancellationTarget.ThrowAsynchronously))]
    public async Task FailurePreservesExceptionAndReleasesInvocation(string method)
    {
        using var runtime = new CancellationTestRuntime();
        using var target = DotNetObjectReference.Create(new CancellationTarget());
        Begin(runtime, target, "call", method);
        var result = await runtime.Result("call");
        Assert.False(result.Success);
        Assert.Equal("Expected failure", Assert.IsType<InvalidOperationException>(result.Exception).Message);
        AssertNoPendingInvocations(runtime);
    }

    [Fact]
    public async Task CancellationCallbackFailureIsReported()
    {
        using var runtime = new CancellationTestRuntime();
        using var target = DotNetObjectReference.Create(new CancellationTarget());
        Begin(runtime, target, "call", nameof(CancellationTarget.ThrowOnCancellation));
        Cancel(runtime, "cancel", "call");
        var cancellation = await runtime.Result("cancel");
        Assert.False(cancellation.Success);
        Assert.Equal("Cancellation callback failure", cancellation.Exception!.Message);
        Assert.True(target.Value.Token.IsCancellationRequested);
        target.Value.Gate.SetResult("finished");
        Assert.True((await runtime.Result("call")).Success);
        AssertNoPendingInvocations(runtime);
    }

    [Fact]
    public async Task RuntimeAndObjectReferenceLifetimesAreSeparateFromInvocation()
    {
        using var runtime = new CancellationTestRuntime();
        var target = DotNetObjectReference.Create(new CancellationTarget());
        var instance = target.Value;
        Begin(runtime, target, "call", nameof(CancellationTarget.Run));
        target.Dispose();
        Cancel(runtime, "cancel", "call");
        Assert.True((await runtime.Result("cancel")).Success);
        Assert.True(instance.Token.IsCancellationRequested);
        Assert.IsType<TaskCanceledException>((await runtime.Result("call")).Exception);
        AssertNoPendingInvocations(runtime);
    }

    [Fact]
    public async Task RuntimeDisposalReleasesResourcesWithoutCancelingApplicationLifetime()
    {
        var runtime = new CancellationTestRuntime();
        using var target = DotNetObjectReference.Create(new CancellationTarget());
        Begin(runtime, target, "call", nameof(CancellationTarget.IgnoreCancellation));
        runtime.Dispose();
        AssertNoPendingInvocations(runtime);
        Assert.False(target.Value.Token.IsCancellationRequested);
        target.Value.Gate.SetResult("finished");
        Assert.True((await runtime.Result("call")).Success);
    }

    [Fact]
    public async Task CancellationIsIsolatedByRuntime()
    {
        using var runtime = new CancellationTestRuntime();
        using var otherRuntime = new CancellationTestRuntime();
        using var target = DotNetObjectReference.Create(new CancellationTarget());
        Begin(runtime, target, "call", nameof(CancellationTarget.Run));
        Cancel(otherRuntime, "cancel-other", "call");
        Assert.True((await otherRuntime.Result("cancel-other")).Success);
        Assert.False(target.Value.Token.IsCancellationRequested);
        Cancel(runtime, "cancel", "call");
        Assert.True((await runtime.Result("cancel")).Success);
        Assert.IsType<TaskCanceledException>((await runtime.Result("call")).Exception);
        AssertNoPendingInvocations(runtime);
        AssertNoPendingInvocations(otherRuntime);
    }

    [Theory]
    [InlineData("""{"__dotNetCancellation":"wrong","args":[]}""", nameof(CancellationTarget.Run))]
    [InlineData("""{"__dotNetCancellation":"call","args":[1]}""", nameof(CancellationTarget.Run))]
    [InlineData("""{"__dotNetCancellation":"call","args":[]}""", nameof(CancellationTarget.NoToken))]
    [InlineData("""{"__dotNetCancellation":"call","args":[]}""", nameof(CancellationTarget.TokenNotLast))]
    [InlineData("""{"__dotNetCancellation":"call","args":[]}""", nameof(CancellationTarget.MultipleTokens))]
    [InlineData("""{"__dotNetCancellation":"call","args":{} }""", nameof(CancellationTarget.Run))]
    [InlineData("""{"__dotNetCancellation":"call","args":[],"extra":true}""", nameof(CancellationTarget.Run))]
    public async Task InvalidEnvelopeDoesNotStartOperationOrRetainSource(string json, string method)
    {
        using var runtime = new CancellationTestRuntime();
        using var target = DotNetObjectReference.Create(new CancellationTarget());
        var objectId = runtime.TrackObjectReference(target);
        DotNetDispatcher.BeginInvokeDotNet(runtime, new DotNetInvocationInfo(null, method, objectId, "call"), json);
        Assert.IsType<JsonException>((await runtime.Result("call")).Exception);
        Assert.False(target.Value.Token.CanBeCanceled);
        AssertNoPendingInvocations(runtime);
    }

    [Fact]
    public void SynchronousInvocationCannotAllocateCancellationSource()
    {
        using var runtime = new CancellationTestRuntime();
        using var target = DotNetObjectReference.Create(new CancellationTarget());
        var objectId = runtime.TrackObjectReference(target);
        Assert.Throws<JsonException>(() => DotNetDispatcher.Invoke(
            runtime, new DotNetInvocationInfo(null, nameof(CancellationTarget.Run), objectId, "call"),
            """{"__dotNetCancellation":"call","args":[]}"""));
        Assert.False(target.Value.Token.CanBeCanceled);
        AssertNoPendingInvocations(runtime);
    }

    [Fact]
    public async Task CompletionWaitsToDisposeSourceUntilCancellationCallbacksFinish()
    {
        using var runtime = new CancellationTestRuntime();
        using var target = DotNetObjectReference.Create(new CancellationTarget());
        Begin(runtime, target, "call", nameof(CancellationTarget.IgnoreCancellation));
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseCallback = new ManualResetEventSlim();
        using var registration = target.Value.Token.Register(() =>
        {
            callbackStarted.SetResult();
            Assert.True(releaseCallback.Wait(TimeSpan.FromSeconds(10)));
            Assert.NotNull(target.Value.Token.WaitHandle);
        });
        Cancel(runtime, "cancel", "call");
        try
        {
            await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            target.Value.Gate.SetResult("finished");
            Assert.True((await runtime.Result("call")).Success);
            AssertNoPendingInvocations(runtime);
        }
        finally
        {
            releaseCallback.Set();
        }

        var cancellation = await runtime.Result("cancel");
        Assert.True(cancellation.Success, cancellation.Exception?.ToString());
    }

    [Fact]
    public async Task StaticInvocationReceivesCancellationToken()
    {
        using var runtime = new CancellationTestRuntime();
        DotNetDispatcher.BeginInvokeDotNet(
            runtime,
            new DotNetInvocationInfo(typeof(DotNetDispatcherCancellationTest).Assembly.GetName().Name, nameof(StaticToken), 0, "call"),
            """{"__dotNetCancellation":"call","args":[]}""");
        var result = await runtime.Result("call");
        Assert.True(result.Success, result.Exception?.ToString());
        Assert.Equal("true", result.ResultJson);
        AssertNoPendingInvocations(runtime);
    }

    [JSInvokable]
    public static bool StaticToken(CancellationToken cancellationToken) => cancellationToken.CanBeCanceled;

    private static void Begin(CancellationTestRuntime runtime, DotNetObjectReference<CancellationTarget> target, string callId, string method)
    {
        var objectId = runtime.TrackObjectReference(target);
        var json = $$"""{"__dotNetCancellation":"{{callId}}","args":[]}""";
        DotNetDispatcher.BeginInvokeDotNet(runtime, new DotNetInvocationInfo(null, method, objectId, callId), json);
    }

    private static void Cancel(CancellationTestRuntime runtime, string cancelCallId, string targetCallId)
    {
        DotNetDispatcher.BeginInvokeDotNet(
            runtime, new DotNetInvocationInfo("Microsoft.JSInterop", "__CancelInvocation", 0, cancelCallId),
            JsonSerializer.Serialize(new[] { targetCallId }));
    }

    private static void AssertNoPendingInvocations(JSRuntime runtime)
    {
        var field = typeof(JSRuntime).GetField("_pendingDotNetInvocations", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable>(field.GetValue(runtime)));
    }

    private sealed class CancellationTestRuntime : TestJSRuntime
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource<DotNetInvocationResult>> _results = new();

        public Task<DotNetInvocationResult> Result(string callId)
            => _results.GetOrAdd(callId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously)).Task.WaitAsync(TimeSpan.FromSeconds(10));

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            => _results.GetOrAdd(invocationInfo.CallId!, _ => new(TaskCreationOptions.RunContinuationsAsynchronously)).SetResult(invocationResult);
    }

    public sealed class CancellationTarget
    {
        public TaskCompletionSource<string> Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken Token { get; private set; }
        public Task<string> Operation { get; private set; } = Task.FromResult("not started");

        [JSInvokable]
        public Task<string> Run(CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            Operation = Gate.Task.WaitAsync(cancellationToken);
            return Operation;
        }

        [JSInvokable]
        public Task<string> IgnoreCancellation(CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            Operation = Gate.Task;
            return Operation;
        }

        [JSInvokable]
        public Task<string> ThrowOnCancellation(CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            cancellationToken.Register(() => throw new InvalidOperationException("Cancellation callback failure"));
            return Gate.Task;
        }

        [JSInvokable]
        public int Synchronous(CancellationToken cancellationToken) => 42;

        [JSInvokable]
        public Task<int> TaskResult(CancellationToken cancellationToken) => Task.FromResult(42);

        [JSInvokable]
        public ValueTask<int> ValueTaskResult(CancellationToken cancellationToken) => ValueTask.FromResult(42);

        [JSInvokable]
        public Task NonGenericTask(CancellationToken cancellationToken) => Task.CompletedTask;

        [JSInvokable]
        public ValueTask NonGenericValueTask(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        [JSInvokable]
        public void ThrowSynchronously(CancellationToken cancellationToken) => throw new InvalidOperationException("Expected failure");

        [JSInvokable]
        public Task ThrowAsynchronously(CancellationToken cancellationToken) => Task.FromException(new InvalidOperationException("Expected failure"));

        [JSInvokable]
        public void NoToken() => throw new InvalidOperationException("Must not be invoked");

        [JSInvokable]
        public void TokenNotLast(CancellationToken cancellationToken, string value) => throw new InvalidOperationException("Must not be invoked");

        [JSInvokable]
        public void MultipleTokens(CancellationToken first, CancellationToken second) => throw new InvalidOperationException("Must not be invoked");
    }
}
