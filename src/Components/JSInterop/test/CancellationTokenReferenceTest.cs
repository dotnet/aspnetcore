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
        var reference = await DotNetCancellationTokenReference.CreateAsync(runtime, source.Token);
        Assert.Equal("""{"__jsObjectId":42}""", JsonSerializer.Serialize(reference, runtime.JsonSerializerOptions));
        Assert.False(runtime.Observer!.Value.WaitForCancellationAsync().IsCompleted);
        source.Cancel();
        Assert.True(await runtime.Observer.Value.WaitForCancellationAsync());
        await reference.DisposeAsync();
        await reference.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => JsonSerializer.Serialize(reference, runtime.JsonSerializerOptions));
        Assert.Throws<ObjectDisposedException>(() => runtime.Observer.Value);
        Assert.Equal(1, runtime.SignalDisposals);
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
        var reference = await DotNetCancellationTokenReference.CreateAsync(runtime, token);
        Assert.Equal(isCanceled, runtime.InitialCancellation);
        var notification = runtime.Observer!.Value.WaitForCancellationAsync();
        Assert.Equal(isCanceled, notification.IsCompleted);
        await reference.DisposeAsync();
        Assert.Equal(isCanceled, await notification);
    }

    [Fact]
    public async Task DisposingOutgoingReferenceStopsObservationWithoutCancelingToken()
    {
        using var runtime = new ReferenceRuntime();
        using var source = new CancellationTokenSource();
        var reference = await DotNetCancellationTokenReference.CreateAsync(runtime, source.Token);
        var notification = runtime.Observer!.Value.WaitForCancellationAsync();
        await reference.DisposeAsync();
        Assert.False(await notification);
        Assert.False(source.IsCancellationRequested);
        source.Cancel();
        Assert.False(await notification);
    }

    [Fact]
    public void RawOutgoingTokenRequiresExplicitLifetimeOwner()
    {
        using var runtime = new ReferenceRuntime();
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(CancellationToken.None, runtime.JsonSerializerOptions));
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
        public DotNetObjectReference<CancellationTokenObserver>? Observer { get; private set; }
        public bool InitialCancellation { get; private set; }
        public int SignalDisposals { get; private set; }
        public TaskCompletionSource<DotNetInvocationResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            var result = "null";
            if (invocationInfo.Identifier == "DotNet.createAbortSignal")
            {
                using var args = JsonDocument.Parse(invocationInfo.ArgsJson!);
                Observer = Assert.IsType<DotNetObjectReference<CancellationTokenObserver>>(
                    GetObjectReference(args.RootElement[0].GetProperty("__dotNetObject").GetInt64()));
                InitialCancellation = args.RootElement[1].GetBoolean();
                result = """{"__jsObjectId":42}""";
            }
            else if (invocationInfo.Identifier == "DotNet.disposeAbortSignal")
            {
                Assert.True(Observer!.Value.WaitForCancellationAsync().IsCompleted);
            }
            else if (invocationInfo.Identifier == "DotNet.disposeJSObjectReferenceById")
            {
                SignalDisposals++;
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
