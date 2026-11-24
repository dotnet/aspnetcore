// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
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
    public async Task OutgoingTokenIsWrittenAsIdAndCancelSendsOneAbortAfterTheCall()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var id = TokenId(runtime.Sent[0]);
        Assert.True(id > 0);
        Assert.Equal($$"""[{"__dotNetCancellationToken":{{id}},"isCancellationRequested":false}]""", runtime.Sent[0].ArgsJson);

        source.Cancel();
        source.Cancel();
        AssertSent(runtime, "receive", Canceled(id));
        Assert.Equal(0, runtime.Sent[1].AsyncHandle);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoneAndCanceledTokensAreWrittenWithoutIdAndSendNothingLater(bool isCanceled)
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        if (isCanceled)
        {
            source.Cancel();
        }

        var token = isCanceled ? source.Token : CancellationToken.None;
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, token);
        Assert.Equal($$"""[{"__dotNetCancellationToken":null,"isCancellationRequested":{{(isCanceled ? "true" : "false")}}}]""", runtime.Sent[0].ArgsJson);

        CollectGarbage();
        await runtime.InvokeVoidAsync("other");
        AssertSent(runtime, "receive", "other");
    }

    [Fact]
    public async Task CancelDuringSerializationSendsAbortAfterTheCall()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, [source.Token, new CancelingValue(source)]);
        var id = TokenId(runtime.Sent[0]);
        AssertSent(runtime, "receive", Canceled(id));
    }

    [Fact]
    public async Task RepeatedOccurrencesOfOneTokenShareOneId()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        using var other = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, [source.Token, source.Token, other.Token]);
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var id = TokenId(runtime.Sent[0], 0);
        Assert.Equal(id, TokenId(runtime.Sent[0], 1));
        Assert.Equal(id, TokenId(runtime.Sent[1]));
        Assert.NotEqual(id, TokenId(runtime.Sent[0], 2));

        source.Cancel();
        AssertSent(runtime, "receive", "receive", Canceled(id));
    }

    // TryReset removes the registrations made before it, so only calls made after it are aborted, as in .NET.
    [Fact]
    public async Task ResetSourceGivesItsTokenANewIdAndCancelsOnlyLaterCalls()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var before = TokenId(runtime.Sent[0]);

        Assert.True(source.TryReset());
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var after = TokenId(runtime.Sent[1]);
        Assert.NotEqual(before, after);
        Assert.Equal(after, TokenId(runtime.Sent[2]));

        source.Cancel();
        AssertSent(runtime, "receive", "receive", "receive", Canceled(after));
    }

    [Fact]
    public async Task CanceledTokenIsWrittenWithoutIdEvenAfterItHadOne()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        source.Cancel();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        Assert.Equal("""[{"__dotNetCancellationToken":null,"isCancellationRequested":true}]""", runtime.Sent[2].ArgsJson);
    }

    [Fact]
    public async Task CanceledTokenKeepsItsIdUntilTheLinkIsCollected()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var id = TokenId(runtime.Sent[0]);

        source.Cancel();
        Assert.Equal(1, OutgoingTokenCount(runtime));

        // The source drops its registrations when canceled, so the link is collected while the source lives on.
        CollectGarbage();
        await runtime.InvokeVoidAsync("other");
        AssertSent(runtime, "receive", Canceled(id), "other", Released(id));
        Assert.Equal(0, OutgoingTokenCount(runtime));
    }

    [Fact]
    public void ReleaseWaitsForTheRepliesOfCallsCarryingTheId()
    {
        using var runtime = new ReferenceRuntime { HoldReplies = true };
        using var source = new CancellationTokenSource();
        _ = runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var id = TokenId(runtime.Sent[0]);

        source.Dispose();
        CollectGarbage();
        _ = runtime.InvokeVoidAsync("other");
        AssertSent(runtime, "receive", "other");

        // The link is already collected, so the last reply releases the id at once.
        runtime.Reply(0);
        AssertSent(runtime, "receive", "other", Released(id));
    }

    [Fact]
    public void ReleaseWaitsForEveryCallCarryingTheId()
    {
        using var runtime = new ReferenceRuntime { HoldReplies = true };
        using var source = new CancellationTokenSource();
        _ = runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        _ = runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var id = TokenId(runtime.Sent[0]);

        source.Dispose();
        runtime.Reply(0);
        CollectGarbage();
        _ = runtime.InvokeVoidAsync("other");
        AssertSent(runtime, "receive", "receive", "other");

        runtime.Reply(1);
        AssertSent(runtime, "receive", "receive", "other", Released(id));
    }

    [Fact]
    public async Task CallWithoutReplyDoesNotKeepItsSourceReachableOnceDotNetStopsWaiting()
    {
        using var runtime = new ReferenceRuntime { HoldReplies = true };
        var source = await SendTokenOfUnreachableSourceThenStopWaitingAsync(runtime);

        CollectGarbage();
        Assert.False(source.IsAlive);
    }

    [Fact]
    public async Task ReplyToACallThatStoppedWaitingStillEndsItsHold()
    {
        using var runtime = new ReferenceRuntime { HoldReplies = true };
        using var control = new CancellationTokenSource();
        using var source = new CancellationTokenSource();
        var call = runtime.InvokeVoidAsync("receive", control.Token, source.Token);
        var id = TokenId(runtime.Sent[0]);
        control.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await call);

        source.Dispose();
        CollectGarbage();
        _ = runtime.InvokeVoidAsync("other");
        AssertSent(runtime, "receive", "other");

        // The link is already collected, so the last reply releases the id at once.
        runtime.Reply(0);
        AssertSent(runtime, "receive", "other", Released(id));
    }

    [Fact]
    public async Task FailedDispatchDoesNotHoldTheId()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var id = TokenId(runtime.Sent[0]);
        runtime.ThrowOnDispatch = true;
        Assert.Throws<InvalidOperationException>(() => runtime.InvokeAsync<string>("receive", CancellationToken.None, [source.Token]));
        runtime.ThrowOnDispatch = false;

        source.Dispose();
        CollectGarbage();
        await runtime.InvokeVoidAsync("other");
        AssertSent(runtime, "receive", "receive", "other", Released(id));
    }

    [Fact]
    public async Task SynchronousCallDoesNotHoldTheIdAfterItReturns()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        runtime.InvokeVoid("receiveSync", source.Token);
        var id = TokenId(runtime.Sent[0]);

        source.Dispose();
        CollectGarbage();
        await runtime.InvokeVoidAsync("other");
        AssertSent(runtime, "receiveSync", "other", Released(id));
    }

    [Fact]
    public async Task SourceCanceledByAFinalizerStillSendsTheCancel()
    {
        using var runtime = new ReferenceRuntime();
        // Not disposed: a finalizer may still wait on it if the test fails.
        var gate = new ManualResetEventSlim();
        try
        {
            var id = SendTokenOfFinalizableOwner(runtime, gate);

            // The owner now waits for its finalizer, which keeps the source and the link reachable.
            GC.Collect();
            await runtime.InvokeVoidAsync("other");
            gate.Set();
            GC.WaitForPendingFinalizers();
            AssertSent(runtime, "receive", "other", Canceled(id));
        }
        finally
        {
            gate.Set();
        }
    }

    [Fact]
    public async Task LiveSourceIsNotReleasedByCollectionAndStillCancels()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var id = TokenId(runtime.Sent[0]);

        CollectGarbage();
        await runtime.InvokeVoidAsync("other");
        source.Cancel();
        AssertSent(runtime, "receive", "other", Canceled(id));
    }

    [Fact]
    public async Task DisposedSourceIsReleasedAtTheNextCallAfterCollection()
    {
        using var runtime = new ReferenceRuntime();
        var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);
        var id = TokenId(runtime.Sent[0]);

        source.Dispose();
        CollectGarbage();
        await runtime.InvokeVoidAsync("other");
        await runtime.InvokeVoidAsync("other");
        AssertSent(runtime, "receive", "other", Released(id), "other");
        Assert.Equal(0, runtime.Sent[2].AsyncHandle);
    }

    [Fact]
    public async Task UnreachableSourceIsNotKeptAliveAndIsReleasedAfterCollection()
    {
        using var runtime = new ReferenceRuntime();
        var source = SendTokenOfUnreachableSource(runtime);
        var id = TokenId(runtime.Sent[0]);

        CollectGarbage();
        Assert.False(source.IsAlive);
        await runtime.InvokeVoidAsync("other");
        AssertSent(runtime, "receive", "other", Released(id));
    }

    [Fact]
    public void SerializationFailureSendsNothingLater()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        Assert.Throws<InvalidOperationException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [source.Token, new ThrowingValue()]));
        source.Cancel();
        Assert.Empty(runtime.Sent);
    }

    [Fact]
    public void DispatchFailureSendsNothingLater()
    {
        using var runtime = new ReferenceRuntime { ThrowOnDispatch = true };
        using var source = new CancellationTokenSource();
        Assert.Throws<InvalidOperationException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [source.Token]));
        source.Cancel();
        AssertSent(runtime, "receive");
    }

    [Fact]
    public void NestedCallDuringFailedSerializationKeepsItsToken()
    {
        using var runtime = new ReferenceRuntime();
        using var outer = new CancellationTokenSource();
        using var inner = new CancellationTokenSource();
        Assert.Throws<InvalidOperationException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [outer.Token, new NestedThrowingValue(runtime, inner.Token)]));
        var id = TokenId(runtime.Sent[0]);

        outer.Cancel();
        inner.Cancel();
        AssertSent(runtime, "receive", Canceled(id));
    }

    [Fact]
    public void TokenWrittenInsideAnotherExecutionContextIsDroppedWithTheFailedCall()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        using var context = ExecutionContext.Capture();
        Assert.NotNull(context);
        Assert.Throws<InvalidOperationException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [new ExecutionContextThrowingValue(runtime, source.Token, context)]));
        source.Cancel();
        Assert.Empty(runtime.Sent);
    }

    [Fact]
    public void TokenSerializedOutsideAnInteropMessageIsRejected()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(source.Token, runtime.JsonSerializerOptions));
    }

    [Fact]
    public async Task DisposingRuntimeStopsAborts()
    {
        var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        await runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token);

        runtime.Dispose();
        source.Cancel();
        AssertSent(runtime, "receive");
    }

    [Fact]
    public void LongLivedSourceDoesNotKeepDisposedRuntimeAlive()
    {
        using var source = new CancellationTokenSource();
        var runtime = SendTokenThenDisposeRuntime(source.Token);

        CollectGarbage();
        Assert.False(runtime.IsAlive);
        source.Cancel();
    }

    [Fact]
    public void DisposedRuntimeRejectsTokens()
    {
        var runtime = new ReferenceRuntime();
        runtime.Dispose();
        using var source = new CancellationTokenSource();
        Assert.Throws<ObjectDisposedException>(() => runtime.InvokeAsync<string>(
            "receive", CancellationToken.None, [source.Token]));
        Assert.Empty(runtime.Sent);
    }

    [Fact]
    public void SynchronousCallLinksTokenAfterItReturns()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        runtime.InvokeVoid("receiveSync", source.Token, new CancelingValue(source));
        var id = TokenId(runtime.Sent[0]);
        AssertSent(runtime, "receiveSync", Canceled(id));
    }

    [Fact]
    public void TokenInSynchronousResultIsLinked()
    {
        using var runtime = new ReferenceRuntime();
        var target = new TokenTarget();
        var targetId = runtime.TrackObjectReference(DotNetObjectReference.Create(target));
        var resultJson = DotNetDispatcher.Invoke(runtime, new(null, nameof(TokenTarget.GetToken), targetId, null), "[]");
        var id = TokenId(resultJson!);

        target.Source.Cancel();
        AssertSent(runtime, Canceled(id));
    }

    [Fact]
    public void TokenInAsynchronousResultIsLinkedAfterTheResultIsSent()
    {
        using var runtime = new ReferenceRuntime();
        var target = new TokenTarget();
        var targetId = runtime.TrackObjectReference(DotNetObjectReference.Create(target));
        DotNetDispatcher.BeginInvokeDotNet(runtime, new(null, nameof(TokenTarget.GetCancelingResult), targetId, "call"), "[]");
        var id = TokenId(runtime.Sent[0].ArgsJson!, "token");
        AssertSent(runtime, "result:call", Canceled(id));
    }

    [Fact]
    public async Task TokenInTaskResultIsLinkedAfterTheResultIsSent()
    {
        using var runtime = new ReferenceRuntime();
        var target = new TokenTarget();
        var targetId = runtime.TrackObjectReference(DotNetObjectReference.Create(target));
        DotNetDispatcher.BeginInvokeDotNet(runtime, new(null, nameof(TokenTarget.GetTokenLaterAsync), targetId, "call"), "[]");
        Assert.Empty(runtime.Sent);

        target.Completion.SetResult();
        var result = await runtime.GetResult("call");
        var id = TokenId(result.ResultJson!);
        target.Source.Cancel();
        // The link is started on the thread pool after the result is sent, so it can come after this cancel.
        await AssertSentEventuallyAsync(runtime, "result:call", Canceled(id));
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

    private static int OutgoingTokenCount(JSRuntime runtime)
    {
        var field = typeof(JSRuntime).GetField("_outgoingCancellationTokens",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return ((System.Collections.ICollection)field.GetValue(runtime)!).Count;
    }

    private static void AssertSent(ReferenceRuntime runtime, params string[] expected)
        => Assert.Equal(expected, runtime.Sent.Select(Describe));

    private static async Task AssertSentEventuallyAsync(ReferenceRuntime runtime, params string[] expected)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!runtime.Sent.Select(Describe).SequenceEqual(expected) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        AssertSent(runtime, expected);
    }

    private static string Canceled(long id) => $"__CancellationTokenCanceled[{id}]";

    private static string Released(long id) => $"__CancellationTokenReleased[{id}]";

    // The identifier alone for ordinary calls; the one-way cancellation messages also show their ids.
    private static string Describe(SentMessage message)
        => message.Identifier.StartsWith("__CancellationToken", StringComparison.Ordinal)
            ? message.Identifier + message.ArgsJson
            : message.Identifier;

    private static long TokenId(SentMessage message, int index = 0)
    {
        using var document = JsonDocument.Parse(message.ArgsJson!);
        return document.RootElement[index].GetProperty("__dotNetCancellationToken").GetInt64();
    }

    private static long TokenId(string json, string? property = null)
    {
        using var document = JsonDocument.Parse(json);
        var element = property is null ? document.RootElement : document.RootElement.GetProperty(property);
        return element.GetProperty("__dotNetCancellationToken").GetInt64();
    }

    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference SendTokenOfUnreachableSource(ReferenceRuntime runtime)
    {
        var source = new CancellationTokenSource();
        Assert.True(runtime.InvokeVoidAsync("receive", CancellationToken.None, source.Token).IsCompletedSuccessfully);
        return new WeakReference(source);
    }

    // The wait ends as after DefaultAsyncTimeout; until then the caller's own pending call keeps its arguments.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> SendTokenOfUnreachableSourceThenStopWaitingAsync(ReferenceRuntime runtime)
    {
        using var control = new CancellationTokenSource();
        var source = new CancellationTokenSource();
        var call = runtime.InvokeVoidAsync("receive", control.Token, source.Token);
        var weak = new WeakReference(source);
        source = null;
        control.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await call);
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SendTokenOfFinalizableOwner(ReferenceRuntime runtime, ManualResetEventSlim gate)
    {
        var owner = new CancelingOwner(gate);
        Assert.True(runtime.InvokeVoidAsync("receive", CancellationToken.None, owner.Token).IsCompletedSuccessfully);
        return TokenId(runtime.Sent[^1]);
    }

    // Holds the only reference to its source and cancels it when finalized, after the test opens the gate.
    private sealed class CancelingOwner(ManualResetEventSlim gate)
    {
        private readonly CancellationTokenSource _source = new();

        public CancellationToken Token => _source.Token;

        ~CancelingOwner()
        {
            gate.Wait(TimeSpan.FromSeconds(10));
            _source.Cancel();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference SendTokenThenDisposeRuntime(CancellationToken token)
    {
        var runtime = new ReferenceRuntime();
        Assert.True(runtime.InvokeVoidAsync("receive", CancellationToken.None, token).IsCompletedSuccessfully);
        runtime.Dispose();
        return new WeakReference(runtime);
    }

    public sealed class ThrowingValue
    {
        public string Value => throw new InvalidOperationException("serialization failure");
    }

    // Serialized after the token, so the token is canceled between its serialization and the send.
    public sealed class CancelingValue(CancellationTokenSource source)
    {
        public string Value
        {
            get
            {
                source.Cancel();
                return "canceled";
            }
        }
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
                ExecutionContext.Run(context, _ => JsonSerializer.Serialize(token, runtime.JsonSerializerOptions), null);
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

    public sealed class TokenTarget
    {
        public CancellationTokenSource Source { get; } = new();
        public TaskCompletionSource Completion { get; } = new();

        [JSInvokable]
        public CancellationToken GetToken() => Source.Token;

        [JSInvokable]
        public CancelingResult GetCancelingResult() => new(Source);

        [JSInvokable]
        public async Task<CancellationToken> GetTokenLaterAsync()
        {
            await Completion.Task;
            return Source.Token;
        }
    }

    public sealed class CancelingResult(CancellationTokenSource source)
    {
        public CancellationToken Token => source.Token;
        public CancelingValue Value { get; } = new(source);
    }

    private sealed record SentMessage(long AsyncHandle, string Identifier, string? ArgsJson);

    private sealed class ReferenceRuntime : JSInProcessRuntime
    {
        private readonly List<SentMessage> _sent = [];

        // A snapshot, because cancels can be sent from the thread pool or a finalizer.
        public SentMessage[] Sent
        {
            get
            {
                lock (_sent)
                {
                    return [.. _sent];
                }
            }
        }

        public bool ThrowOnDispatch { get; set; }
        // When set, JS replies to calls only through Reply, in the order of the held calls.
        public bool HoldReplies { get; init; }
        private readonly List<long> _heldReplies = [];

        public void Reply(int heldIndex)
            => DotNetDispatcher.EndInvokeJS(this, $"[{_heldReplies[heldIndex]},true,null]");

        private void Record(SentMessage message)
        {
            lock (_sent)
            {
                _sent.Add(message);
            }
        }

        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<DotNetInvocationResult>> _results = new();

        public Task<DotNetInvocationResult> GetResult(string callId) => GetResultSource(callId).Task;

        private TaskCompletionSource<DotNetInvocationResult> GetResultSource(string callId)
            => _results.GetOrAdd(callId, static _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

        protected override string? InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
            => throw new NotImplementedException();

        protected override string? InvokeJS(in JSInvocationInfo invocationInfo)
        {
            Record(new(invocationInfo.AsyncHandle, invocationInfo.Identifier, invocationInfo.ArgsJson));
            return null;
        }

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
            => throw new NotImplementedException();

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            Record(new(invocationInfo.AsyncHandle, invocationInfo.Identifier, invocationInfo.ArgsJson));
            if (ThrowOnDispatch)
            {
                throw new InvalidOperationException("dispatch failure");
            }

            if (invocationInfo.AsyncHandle != 0)
            {
                if (HoldReplies)
                {
                    _heldReplies.Add(invocationInfo.AsyncHandle);
                }
                else
                {
                    DotNetDispatcher.EndInvokeJS(this, $"[{invocationInfo.AsyncHandle},true,null]");
                }
            }
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
            Record(new(0, $"result:{invocationInfo.CallId}", invocationResult.ResultJson));
            GetResultSource(invocationInfo.CallId!).SetResult(invocationResult);
        }

        protected internal override void SendByteArray(int id, byte[] data)
        {
        }
    }
}
