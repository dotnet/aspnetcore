// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Microsoft.JSInterop;

public class CancellationTokenReferenceTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IncomingReferenceSuppliesTokenInOrdinaryArgumentPosition(bool isAborted)
    {
        using var runtime = new ReferenceRuntime();
        var signal = CreateSignal();
        var reference = JSCancellationTokenReference.Create(signal.Object, isAborted);
        var json = JsonSerializer.Serialize(new object[] { reference, "value" }, runtime.JsonSerializerOptions);
        var arguments = DotNetDispatcher.ParseArguments(runtime, "Receive", json, [typeof(CancellationToken), typeof(string)]);
        var token = Assert.IsType<CancellationToken>(arguments[0]);
        Assert.True(token.CanBeCanceled);
        Assert.Equal(isAborted, token.IsCancellationRequested);
        Assert.Equal("value", arguments[1]);

        await reference.Value.CancelAsync();
        Assert.True(token.IsCancellationRequested);
        await reference.Value.DisposeAsync();
        signal.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task TokenSurvivesInvocationReturnAndCanBeReused()
    {
        using var runtime = new ReferenceRuntime();
        var reference = JSCancellationTokenReference.Create(CreateSignal().Object, false);
        var target = new Target();
        using var targetReference = DotNetObjectReference.Create(target);
        var targetId = runtime.TrackObjectReference(targetReference);
        var json = JsonSerializer.Serialize(new object[] { reference, "retained" }, runtime.JsonSerializerOptions);
        DotNetDispatcher.BeginInvokeDotNet(runtime, new(null, nameof(Target.Retain), targetId, "call"), json);
        Assert.True((await runtime.Result.Task).Success);
        Assert.False(target.Token.IsCancellationRequested);
        Assert.Equal("retained", target.Value);

        await reference.Value.CancelAsync();
        Assert.True(target.Token.IsCancellationRequested);
        var second = DotNetDispatcher.ParseArguments(runtime, "Other", json, [typeof(CancellationToken), typeof(string)]);
        Assert.Equal(target.Token, Assert.IsType<CancellationToken>(second[0]));
        await reference.Value.DisposeAsync();
    }

    [Fact]
    public async Task DisposingIncomingReferenceDoesNotCancelRetainedToken()
    {
        var reference = JSCancellationTokenReference.Create(CreateSignal().Object, false);
        var owner = reference.Value;
        var token = owner.Token;
        await owner.DisposeAsync();
        await owner.DisposeAsync();
        Assert.False(token.IsCancellationRequested);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => owner.CancelAsync());
        Assert.Throws<ObjectDisposedException>(() => reference.Value);
    }

    [Fact]
    public async Task IncomingCallbackFailureRemainsObservableAndCleanupStillRuns()
    {
        var signal = CreateSignal();
        var reference = JSCancellationTokenReference.Create(signal.Object, false);
        var owner = reference.Value;
        using var registration = owner.Token.Register(() => throw new InvalidOperationException("callback failure"));
        var exception = await Assert.ThrowsAsync<AggregateException>(() => owner.CancelAsync());
        Assert.Equal("callback failure", Assert.Single(exception.InnerExceptions).Message);
        await Assert.ThrowsAsync<AggregateException>(async () => await owner.DisposeAsync());
        signal.Verify(x => x.DisposeAsync(), Times.Once);
        Assert.Throws<ObjectDisposedException>(() => reference.Value);
    }

    [Fact]
    public async Task DisposalAfterPendingCancellationResumesOnOwningSynchronizationContext()
    {
        var context = new Mock<SynchronizationContext>();
        var posted = new TaskCompletionSource<(SendOrPostCallback Callback, object? State)>(TaskCreationOptions.RunContinuationsAsynchronously);
        context.Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object?>()))
            .Callback((SendOrPostCallback callback, object? state) => posted.SetResult((callback, state)));
        var signal = CreateSignal();
        SynchronizationContext? disposalContext = null;
        signal.Setup(x => x.InvokeAsync<IJSVoidResult>("dispose", It.IsAny<object?[]?>()))
            .Callback(() => disposalContext = SynchronizationContext.Current)
            .ReturnsAsync(Mock.Of<IJSVoidResult>());
        var reference = JSCancellationTokenReference.Create(signal.Object, false);
        var owner = reference.Value;
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseCallback = new ManualResetEventSlim();
        using var registration = owner.Token.Register(() =>
        {
            callbackStarted.SetResult();
            Assert.True(releaseCallback.Wait(TimeSpan.FromSeconds(10)));
        });

        Task cancellation;
        Task disposal;
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context.Object);
        try
        {
            cancellation = owner.CancelAsync();
            Assert.Same(cancellation, owner.CancelAsync());
            disposal = owner.DisposeAsync().AsTask();
            Assert.Same(disposal, owner.DisposeAsync().AsTask());
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        try
        {
            await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(disposal.IsCompleted);
            signal.Verify(x => x.InvokeAsync<IJSVoidResult>("dispose", It.IsAny<object?[]?>()), Times.Never);
        }
        finally
        {
            releaseCallback.Set();
        }

        await cancellation.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.WhenAny(posted.Task, disposal).WaitAsync(TimeSpan.FromSeconds(10));
        if (posted.Task.IsCompletedSuccessfully)
        {
            var work = await posted.Task;
            SynchronizationContext.SetSynchronizationContext(context.Object);
            try
            {
                work.Callback(work.State);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        await disposal.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Same(context.Object, disposalContext);
        signal.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task IndependentReferencesDoNotShareCancellation()
    {
        var first = JSCancellationTokenReference.Create(CreateSignal().Object, false);
        var second = JSCancellationTokenReference.Create(CreateSignal().Object, false);
        var secondToken = second.Value.Token;
        await first.Value.CancelAsync();
        Assert.False(secondToken.IsCancellationRequested);
        await first.Value.DisposeAsync();
        await second.Value.DisposeAsync();
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
    public async Task ReleasedIncomingTokenStillSupportsLateCancellationRegistration()
    {
        var reference = JSCancellationTokenReference.Create(CreateSignal().Object, false);
        var owner = reference.Value;
        var token = owner.Token;
        await owner.CancelAsync();
        await owner.DisposeAsync();
        var invoked = false;
        using var registration = token.Register(() => invoked = true);
        Assert.True(invoked);
    }

    [Fact]
    public void MissingArgumentDoesNotInjectCancellationToken()
    {
        using var runtime = new ReferenceRuntime();
        Assert.Throws<ArgumentException>(() => DotNetDispatcher.ParseArguments(runtime, "Receive", "[]", [typeof(CancellationToken)]));
    }

    private static Mock<IJSObjectReference> CreateSignal()
    {
        var signal = new Mock<IJSObjectReference>(MockBehavior.Strict);
        signal.Setup(x => x.InvokeAsync<IJSVoidResult>("dispose", It.IsAny<object?[]?>())).ReturnsAsync(Mock.Of<IJSVoidResult>());
        signal.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return signal;
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
        public TaskCompletionSource<DotNetInvocationResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
            => Result.SetResult(invocationResult);
    }
}
