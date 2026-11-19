// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop;

public class CancellationTokenReferenceTest
{
    [Fact]
    public void IncomingSignalIdSuppliesTokenInOrdinaryArgumentPosition()
    {
        using var runtime = new ReferenceRuntime();
        var arguments = DotNetDispatcher.ParseArguments(runtime, "Receive", """[{"__abortSignal":5,"aborted":false},"value"]""", [typeof(CancellationToken), typeof(string)]);
        var token = Assert.IsType<CancellationToken>(arguments[0]);
        Assert.True(token.CanBeCanceled);
        Assert.False(token.IsCancellationRequested);
        Assert.Equal("value", arguments[1]);
        Assert.Equal(1, AbortSignalSourceCount(runtime));

        SendAbort(runtime, 5);
        Assert.True(token.IsCancellationRequested);
        Assert.Equal(1, AbortSignalSourceCount(runtime));

        SendRelease(runtime, 5);
        Assert.Equal(0, AbortSignalSourceCount(runtime));
    }

    [Fact]
    public void AbortedStateSuppliesCanceledTokenTrackedUntilRelease()
    {
        using var runtime = new ReferenceRuntime();
        var arguments = DotNetDispatcher.ParseArguments(runtime, "Receive", """[{"__abortSignal":6,"aborted":true}]""", [typeof(CancellationToken)]);
        var token = Assert.IsType<CancellationToken>(arguments[0]);
        Assert.True(token.CanBeCanceled);
        Assert.True(token.IsCancellationRequested);
        Assert.Equal(1, AbortSignalSourceCount(runtime));

        SendRelease(runtime, 6);
        Assert.Equal(0, AbortSignalSourceCount(runtime));
    }

    [Fact]
    public void AbortedStateCancelsTheTokenAnEarlierCallReceived()
    {
        using var runtime = new ReferenceRuntime();
        var first = ReceiveToken(runtime, """[{"__abortSignal":8,"aborted":false}]""");
        var second = ReceiveToken(runtime, """[{"__abortSignal":8,"aborted":true}]""");
        Assert.Equal(first, second);
        Assert.True(first.IsCancellationRequested);
    }

    // JS sends "released" only after every call carrying the id has completed and the abort was acknowledged,
    // so the call and the abort are the only messages that can reach .NET in either order.
    [Theory]
    [InlineData("call", "abort")]
    [InlineData("abort", "call")]
    public void CallAndAbortInEitherOrderCancelTheTokenAndReleaseRemovesIt(string firstMessage, string secondMessage)
    {
        using var runtime = new ReferenceRuntime();
        CancellationToken? token = null;
        foreach (var message in new[] { firstMessage, secondMessage })
        {
            if (message == "call")
            {
                token = ReceiveToken(runtime, """[{"__abortSignal":9,"aborted":false}]""");
            }
            else
            {
                SendAbort(runtime, 9);
            }
        }

        Assert.True(token!.Value.IsCancellationRequested);
        Assert.Equal(1, AbortSignalSourceCount(runtime));
        SendRelease(runtime, 9);
        Assert.Equal(0, AbortSignalSourceCount(runtime));
    }

    [Fact]
    public async Task AbortIsAcknowledgedAfterCancellationCallbacksRun()
    {
        using var runtime = new ReferenceRuntime();
        var token = ReceiveToken(runtime, """[{"__abortSignal":10,"aborted":false}]""");
        using var entered = new ManualResetEventSlim();
        using var gate = new ManualResetEventSlim();
        using var registration = token.Register(() =>
        {
            entered.Set();
            gate.Wait();
        });

        Task<DotNetInvocationResult> acknowledgment;
        try
        {
            SendAbort(runtime, 10, "abort");
            acknowledgment = runtime.GetResult("abort");
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            await Task.Delay(50);
            Assert.False(acknowledgment.IsCompleted);
        }
        finally
        {
            gate.Set();
        }

        Assert.True((await acknowledgment).Success);
    }

    [Fact]
    public async Task AbortedStateAndAbortMessageShareOneCancellation()
    {
        using var runtime = new ReferenceRuntime();
        var token = ReceiveToken(runtime, """[{"__abortSignal":12,"aborted":false}]""");
        using var entered = new ManualResetEventSlim();
        using var gate = new ManualResetEventSlim();
        var callbackRuns = 0;
        using var registration = token.Register(() =>
        {
            Interlocked.Increment(ref callbackRuns);
            entered.Set();
            gate.Wait();
            throw new InvalidOperationException("callback failure");
        });

        Task<DotNetInvocationResult> acknowledgment;
        try
        {
            ReceiveToken(runtime, """[{"__abortSignal":12,"aborted":true}]""");
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            SendAbort(runtime, 12, "abort");
            acknowledgment = runtime.GetResult("abort");
            await Task.Delay(50);
            Assert.False(acknowledgment.IsCompleted);
        }
        finally
        {
            gate.Set();
        }

        var result = await acknowledgment;
        Assert.False(result.Success);
        Assert.Contains("callback failure", result.Exception!.ToString(), StringComparison.Ordinal);
        Assert.Equal(1, callbackRuns);
    }

    [Fact]
    public async Task AbortAcknowledgmentReportsCallbackFailure()
    {
        using var runtime = new ReferenceRuntime();
        var token = ReceiveToken(runtime, """[{"__abortSignal":11,"aborted":false}]""");
        using var registration = token.Register(() => throw new InvalidOperationException("callback failure"));

        SendAbort(runtime, 11, "abort");
        var result = await runtime.GetResult("abort");
        Assert.False(result.Success);
        Assert.Contains("callback failure", result.Exception!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TokenSurvivesInvocationReturnAndRepeatedIdsShareIt()
    {
        using var runtime = new ReferenceRuntime();
        var target = new Target();
        using var targetReference = DotNetObjectReference.Create(target);
        var targetId = runtime.TrackObjectReference(targetReference);
        var json = """[{"__abortSignal":7,"aborted":false},"retained"]""";
        DotNetDispatcher.BeginInvokeDotNet(runtime, new(null, nameof(Target.Retain), targetId, "call"), json);
        Assert.True((await runtime.GetResult("call")).Success);
        Assert.False(target.Token.IsCancellationRequested);
        Assert.Equal("retained", target.Value);

        var second = DotNetDispatcher.ParseArguments(runtime, "Other", json, [typeof(CancellationToken), typeof(string)]);
        Assert.Equal(target.Token, Assert.IsType<CancellationToken>(second[0]));
        Assert.Equal(1, AbortSignalSourceCount(runtime));

        SendAbort(runtime, 7);
        Assert.True(target.Token.IsCancellationRequested);
    }

    [Fact]
    public void IndependentSignalsDoNotShareCancellation()
    {
        using var runtime = new ReferenceRuntime();
        var arguments = DotNetDispatcher.ParseArguments(runtime, "Receive", """[{"__abortSignal":1,"aborted":false},{"__abortSignal":2,"aborted":false}]""", [typeof(CancellationToken), typeof(CancellationToken)]);
        var first = Assert.IsType<CancellationToken>(arguments[0]);
        var second = Assert.IsType<CancellationToken>(arguments[1]);
        Assert.NotEqual(first, second);

        SendAbort(runtime, 1);
        Assert.True(first.IsCancellationRequested);
        Assert.False(second.IsCancellationRequested);
    }

    [Fact]
    public void ReleasingSignalStopsTrackingWithoutCancelingRetainedToken()
    {
        using var runtime = new ReferenceRuntime();
        var token = ReceiveToken(runtime, """[{"__abortSignal":3,"aborted":false}]""");

        SendRelease(runtime, 3);
        Assert.Equal(0, AbortSignalSourceCount(runtime));
        Assert.False(token.IsCancellationRequested);
        var invoked = false;
        using var registration = token.Register(() => invoked = true);
        Assert.False(invoked);
    }

    [Fact]
    public void ReleaseForUnknownIdIsIgnored()
    {
        using var runtime = new ReferenceRuntime();
        var token = ReceiveToken(runtime, """[{"__abortSignal":1,"aborted":false}]""");

        SendRelease(runtime, 42);
        Assert.False(token.IsCancellationRequested);
        Assert.Equal(1, AbortSignalSourceCount(runtime));
    }

    [Fact]
    public void DisposingRuntimeStopsTrackingWithoutCancelingTokens()
    {
        var runtime = new ReferenceRuntime();
        var token = ReceiveToken(runtime, """[{"__abortSignal":4,"aborted":false}]""");

        runtime.Dispose();
        Assert.Equal(0, AbortSignalSourceCount(runtime));
        Assert.False(token.IsCancellationRequested);
    }

    [Theory]
    [InlineData("""[{"__abortSignal":0,"aborted":false}]""")]
    [InlineData("""[{"__abortSignal":-1,"aborted":false}]""")]
    [InlineData("""[{"__abortSignal":"1","aborted":false}]""")]
    [InlineData("""[{"__abortSignal":1}]""")]
    [InlineData("""[{"__abortSignal":1,"aborted":"true"}]""")]
    [InlineData("""[{"__abortSignal":1,"aborted":false,"other":2}]""")]
    [InlineData("""[{"aborted":false,"__abortSignal":1}]""")]
    [InlineData("""[true]""")]
    public void InvalidIncomingValueIsRejected(string json)
    {
        using var runtime = new ReferenceRuntime();
        Assert.Throws<JsonException>(() => DotNetDispatcher.ParseArguments(runtime, "Receive", json, [typeof(CancellationToken)]));
        Assert.Equal(0, AbortSignalSourceCount(runtime));
    }

    [Fact]
    public async Task OutgoingReferenceUsesExistingObjectReferenceSerialization()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var observer = runtime.LastObserver!;
        Assert.False(observer.Value.WaitForCancellationAsync().IsCompleted);
        source.Cancel();
        Assert.True(await observer.Value.WaitForCancellationAsync());
        observer.Value.Release();
        Assert.Throws<ObjectDisposedException>(() => observer.Value);
        Assert.Equal(0, AutomaticReferenceCount(runtime));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutgoingSnapshotAndObserverReflectInitialCancellation(bool isCanceled)
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        if (isCanceled)
        {
            source.Cancel();
        }

        var token = isCanceled ? source.Token : CancellationToken.None;
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, token);
        Assert.Equal(isCanceled, runtime.InitialCancellation);
        Assert.Null(runtime.LastObserver);
        Assert.Equal(0, AutomaticReferenceCount(runtime));
    }

    [Fact]
    public async Task DisposingOutgoingReferenceStopsObservationWithoutCancelingToken()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var notification = runtime.LastObserver!.Value.WaitForCancellationAsync();
        runtime.Dispose();
        Assert.False(await notification);
        Assert.False(source.IsCancellationRequested);
        source.Cancel();
        Assert.False(await notification);
    }

    [Fact]
    public void SerializationFailureRollsBackAutomaticReferences()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        Assert.Throws<InvalidOperationException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [source.Token, new ThrowingValue()]));
        Assert.Equal(0, AutomaticReferenceCount(runtime));
    }

    [Fact]
    public void DispatchFailureRollsBackAutomaticReferencesBeforeBrowserReceivesThem()
    {
        using var runtime = new ReferenceRuntime { ThrowOnDispatch = true };
        using var source = new CancellationTokenSource();
        Assert.Throws<InvalidOperationException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [source.Token]));
        Assert.Equal(1, runtime.ReferenceCountAtDispatch);
        Assert.Equal(0, AutomaticReferenceCount(runtime));
        source.Cancel();
        Assert.Equal(0, AutomaticReferenceCount(runtime));
    }

    [Fact]
    public async Task RepeatedRawTokenOccurrencesHaveIndependentReferenceLifetimes()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var first = runtime.LastObserver!;
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var second = runtime.LastObserver!;
        Assert.NotSame(first, second);
        Assert.Equal(2, AutomaticReferenceCount(runtime));
        first.Value.Release();
        Assert.Equal(1, AutomaticReferenceCount(runtime));
        source.Cancel();
        Assert.True(await second.Value.WaitForCancellationAsync());
        second.Value.Release();
        Assert.Equal(0, AutomaticReferenceCount(runtime));
    }

    [Fact]
    public async Task SerializationRollbackDoesNotReleaseNestedInvocationProjection()
    {
        using var runtime = new ReferenceRuntime();
        using var outer = new CancellationTokenSource();
        using var inner = new CancellationTokenSource();
        Assert.Throws<InvalidOperationException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [outer.Token, new NestedThrowingValue(runtime, inner.Token)]));
        Assert.Equal(1, AutomaticReferenceCount(runtime));
        var observer = runtime.LastObserver!;
        inner.Cancel();
        Assert.True(await observer.Value.WaitForCancellationAsync());
        observer.Value.Release();
        Assert.Equal(0, AutomaticReferenceCount(runtime));
    }

    [Fact]
    public void SerializationRollbackUsesRuntimeScopeAcrossExecutionContexts()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        using var context = ExecutionContext.Capture();
        Assert.NotNull(context);
        Assert.Throws<InvalidOperationException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [new ExecutionContextThrowingValue(runtime, source.Token, context)]));
        Assert.Equal(0, AutomaticReferenceCount(runtime));
    }

    [Fact]
    public void DisposedRuntimeCannotAllocateAnotherAutomaticProjection()
    {
        var runtime = new ReferenceRuntime();
        runtime.Dispose();
        using var source = new CancellationTokenSource();
        Assert.Throws<ObjectDisposedException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [source.Token]));
        Assert.Equal(0, AutomaticReferenceCount(runtime));
    }

    [Fact]
    public void MissingArgumentDoesNotInjectCancellationToken()
    {
        using var runtime = new ReferenceRuntime();
        Assert.Throws<ArgumentException>(() => DotNetDispatcher.ParseArguments(runtime, "Receive", "[]", [typeof(CancellationToken)]));
    }

    private static CancellationToken ReceiveToken(JSRuntime runtime, string json)
        => Assert.IsType<CancellationToken>(DotNetDispatcher.ParseArguments(runtime, "Receive", json, [typeof(CancellationToken)])[0]);

    // JS sends the abort as an acknowledged call; a null call id is only for tests that ignore the acknowledgment.
    private static void SendAbort(JSRuntime runtime, long abortSignalId, string? callId = null)
        => DotNetDispatcher.BeginInvokeDotNet(runtime, new("Microsoft.JSInterop", "__AbortSignalAborted", 0, callId), $"[{abortSignalId}]");

    private static void SendRelease(JSRuntime runtime, long abortSignalId)
        => DotNetDispatcher.BeginInvokeDotNet(runtime, new("Microsoft.JSInterop", "__AbortSignalReleased", 0, null), $"[{abortSignalId}]");

    private static int AbortSignalSourceCount(JSRuntime runtime)
    {
        var field = typeof(JSRuntime).GetField("_abortSignalSources",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return ((System.Collections.ICollection)field.GetValue(runtime)!).Count;
    }

    private static int AutomaticReferenceCount(JSRuntime runtime)
    {
        var field = typeof(JSRuntime).GetField("_automaticCancellationReferences",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var references = (System.Collections.Concurrent.ConcurrentDictionary<long, Action>)field.GetValue(runtime)!;
        return references.Count;
    }

    public sealed class ThrowingValue
    {
        public string Value => throw new InvalidOperationException("serialization failure");
    }

    private sealed class NestedThrowingValue(ReferenceRuntime runtime, CancellationToken token)
    {
        public string Value
        {
            get
            {
                var call = runtime.InvokeVoidAsync("receive", CancellationToken.None, token);
                Assert.True(call.IsCompletedSuccessfully);
                throw new InvalidOperationException("outer serialization failure");
            }
        }
    }

    private sealed class ExecutionContextThrowingValue(ReferenceRuntime runtime, CancellationToken token, ExecutionContext context)
    {
        public string Value
        {
            get
            {
                ExecutionContext.Run(context, _ =>
                {
                    JsonSerializer.Serialize(token, runtime.JsonSerializerOptions);
                    Assert.Equal(1, AutomaticReferenceCount(runtime));
                }, null);
                throw new InvalidOperationException("serialization failure");
            }
        }
    }

    public sealed class Target
    {
        public CancellationToken Token { get; private set; }
        public string? Value { get; private set; }

        [JSInvokable]
        public string Retain(CancellationToken cancellationToken, string value)
        {
            Token = cancellationToken;
            Value = value;
            return value;
        }
    }

    private sealed class ReferenceRuntime : TestJSRuntime
    {
        public DotNetObjectReference<CancellationTokenObserver>? LastObserver { get; private set; }
        public bool InitialCancellation { get; private set; }
        public bool ThrowOnDispatch { get; init; }
        public int ReferenceCountAtDispatch { get; private set; }
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<DotNetInvocationResult>> _results = new();

        public Task<DotNetInvocationResult> GetResult(string callId) => GetResultSource(callId).Task;

        private TaskCompletionSource<DotNetInvocationResult> GetResultSource(string callId)
            => _results.GetOrAdd(callId, static _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            ReferenceCountAtDispatch = AutomaticReferenceCount(this);
            if (ThrowOnDispatch)
            {
                throw new InvalidOperationException("dispatch failure");
            }
            var result = "null";
            if (invocationInfo.Identifier == "receive")
            {
                using var args = JsonDocument.Parse(invocationInfo.ArgsJson!);
                var descriptor = args.RootElement[0];
                var reference = descriptor.GetProperty("__dotNetCancellationToken");
                LastObserver = reference.ValueKind == JsonValueKind.Null ? null :
                    Assert.IsType<DotNetObjectReference<CancellationTokenObserver>>(GetObjectReference(reference.GetProperty("__dotNetObject").GetInt64()));
                InitialCancellation = descriptor.GetProperty("isCancellationRequested").GetBoolean();
            }
            else
            {
                throw new InvalidOperationException(invocationInfo.Identifier);
            }

            DotNetDispatcher.EndInvokeJS(this, $"[{invocationInfo.AsyncHandle},true,{result}]");
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            => GetResultSource(invocationInfo.CallId!).SetResult(invocationResult);
    }
}
