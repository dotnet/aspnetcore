// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class AbortLinkTest
{
    [Fact]
    public async Task CreateAsync_ReturnsNullWithoutAnyCallForATokenThatCannotBeCanceled()
    {
        var jsRuntime = new RecordingJSRuntime();
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());

        var link = await context.AbortLinks.CreateAsync(CancellationToken.None);

        Assert.Null(link);
        Assert.Empty(jsRuntime.Constructed);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWithoutAnyCallForACanceledToken()
    {
        var jsRuntime = new RecordingJSRuntime();
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        var canceled = new CancellationToken(canceled: true);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.AbortLinks.CreateAsync(canceled).AsTask());

        Assert.Equal(canceled, exception.CancellationToken);
        Assert.Empty(jsRuntime.Constructed);
    }

    [Fact]
    public async Task CreateAsync_CreatesAControllerAndReadsItsSignal()
    {
        var jsRuntime = new RecordingJSRuntime();
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        using var cts = new CancellationTokenSource();

        await using var link = await context.AbortLinks.CreateAsync(cts.Token);

        Assert.NotNull(link);
        Assert.Same(jsRuntime.Signal, link.Signal);
        Assert.Equal(["AbortController"], jsRuntime.Constructed);
        Assert.Equal(["signal"], jsRuntime.Controller.RequestedValues);
        Assert.Empty(jsRuntime.Controller.Invocations);
    }

    [Fact]
    public void AbortLinks_IsSharedByTheScope()
    {
        var context = new FeatureContext(new RecordingJSRuntime(), new RecordingLoggerFactory());

        Assert.Same(context.AbortLinks, context.AbortLinks);
    }

    [Fact]
    public async Task Cancel_AbortsTheController()
    {
        var jsRuntime = new RecordingJSRuntime();
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        using var cts = new CancellationTokenSource();
        await using var link = await context.AbortLinks.CreateAsync(cts.Token);

        cts.Cancel();

        Assert.Equal(["abort"], jsRuntime.Controller.Invocations);
    }

    [Fact]
    public async Task Cancel_IgnoresADisconnectedCircuit()
    {
        var jsRuntime = new RecordingJSRuntime();
        var loggerFactory = new RecordingLoggerFactory();
        var context = new FeatureContext(jsRuntime, loggerFactory);
        jsRuntime.Controller.InvokeException = new JSDisconnectedException("The circuit is disconnected.");
        using var cts = new CancellationTokenSource();
        await using var link = await context.AbortLinks.CreateAsync(cts.Token);

        cts.Cancel();

        Assert.Equal(["abort"], jsRuntime.Controller.Invocations);
        Assert.Empty(loggerFactory.Entries);
    }

    [Fact]
    public async Task Cancel_LogsAnyOtherFailureOfAbort()
    {
        var jsRuntime = new RecordingJSRuntime();
        var loggerFactory = new RecordingLoggerFactory();
        var context = new FeatureContext(jsRuntime, loggerFactory);
        var failure = new JSException("abort failed");
        jsRuntime.Controller.InvokeException = failure;
        using var cts = new CancellationTokenSource();
        await using var link = await context.AbortLinks.CreateAsync(cts.Token);

        cts.Cancel();

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("AbortFailed", entry.EventId.Name);
        Assert.Same(failure, entry.Exception);
    }

    [Fact]
    public async Task CreateAsync_ThrowsAndReleasesTheReferencesWhenCanceledDuringCreation()
    {
        var pending = new TaskCompletionSource<IJSObjectReference>();
        var jsRuntime = new RecordingJSRuntime { PendingConstructor = pending };
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        using var cts = new CancellationTokenSource();

        var creation = context.AbortLinks.CreateAsync(cts.Token).AsTask();
        cts.Cancel();
        pending.SetResult(jsRuntime.Controller);
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => creation);

        Assert.Equal(cts.Token, exception.CancellationToken);
        Assert.True(jsRuntime.Controller.Disposed);
        Assert.True(jsRuntime.Signal.Disposed);
        Assert.Empty(jsRuntime.Controller.Invocations);
    }

    [Fact]
    public async Task CreateAsync_DisposesTheControllerWhenReadingTheSignalFails()
    {
        var jsRuntime = new RecordingJSRuntime();
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        var disconnected = new JSDisconnectedException("The circuit is disconnected.");
        jsRuntime.Controller.GetValueException = disconnected;
        using var cts = new CancellationTokenSource();

        var exception = await Assert.ThrowsAsync<JSDisconnectedException>(() => context.AbortLinks.CreateAsync(cts.Token).AsTask());

        Assert.Same(disconnected, exception);
        Assert.True(jsRuntime.Controller.Disposed);
    }

    [Fact]
    public async Task DisposeAsync_StopsListeningAndDisposesBothReferences()
    {
        var jsRuntime = new RecordingJSRuntime();
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        using var cts = new CancellationTokenSource();
        var link = await context.AbortLinks.CreateAsync(cts.Token);

        await link!.DisposeAsync();
        cts.Cancel();

        Assert.Empty(jsRuntime.Controller.Invocations);
        Assert.True(jsRuntime.Controller.Disposed);
        Assert.True(jsRuntime.Signal.Disposed);
    }

    [Fact]
    public async Task ScopeDisposal_ReleasesTheLinksStillOpen()
    {
        var jsRuntime = new RecordingJSRuntime();
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        using var cts = new CancellationTokenSource();
        await context.AbortLinks.CreateAsync(cts.Token);
        Assert.False(jsRuntime.Controller.Disposed);

        await ((IInternalAsyncDisposal)context).InternalDisposeAsync();
        cts.Cancel();

        Assert.Empty(jsRuntime.Controller.Invocations);
        Assert.True(jsRuntime.Controller.Disposed);
        Assert.True(jsRuntime.Signal.Disposed);
    }

    [Fact]
    public async Task ScopeDisposal_DoesNotReleaseALinkTwice()
    {
        var jsRuntime = new RecordingJSRuntime();
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        using var cts = new CancellationTokenSource();
        var link = await context.AbortLinks.CreateAsync(cts.Token);
        await link!.DisposeAsync();

        await ((IInternalAsyncDisposal)context).InternalDisposeAsync();

        Assert.Equal(1, jsRuntime.Controller.DisposeCount);
        Assert.Equal(1, jsRuntime.Signal.DisposeCount);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWithoutAnyCallAfterScopeDisposal()
    {
        var jsRuntime = new RecordingJSRuntime();
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        var abortLinks = context.AbortLinks;
        using var cts = new CancellationTokenSource();

        await ((IInternalAsyncDisposal)context).InternalDisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => abortLinks.CreateAsync(cts.Token).AsTask());
        Assert.Empty(jsRuntime.Constructed);
    }

    [Fact]
    public async Task CreateAsync_ThrowsAndReleasesTheReferencesWhenTheScopeEndsDuringCreation()
    {
        var pending = new TaskCompletionSource<IJSObjectReference>();
        var jsRuntime = new RecordingJSRuntime { PendingConstructor = pending };
        var context = new FeatureContext(jsRuntime, new RecordingLoggerFactory());
        using var cts = new CancellationTokenSource();

        var creation = context.AbortLinks.CreateAsync(cts.Token).AsTask();
        await ((IInternalAsyncDisposal)context).InternalDisposeAsync();
        pending.SetResult(jsRuntime.Controller);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => creation);
        cts.Cancel();

        Assert.True(jsRuntime.Controller.Disposed);
        Assert.True(jsRuntime.Signal.Disposed);
        Assert.Empty(jsRuntime.Controller.Invocations);
    }

    [Fact]
    public void CreateCanceledException_KeepsTheTokenAndTheBrowserError()
    {
        using var cts = new CancellationTokenSource();
        var browserError = new JSException("signal is aborted without reason");

        var exception = AbortLink.CreateCanceledException(browserError, cts.Token);

        Assert.Equal(cts.Token, exception.CancellationToken);
        Assert.Same(browserError, exception.InnerException);
    }

    private sealed class RecordingJSRuntime : IJSRuntime
    {
        public RecordingJSObjectReference Controller { get; } = new() { Signal = new() };

        public RecordingJSObjectReference Signal => Controller.Signal!;

        public List<string> Constructed { get; } = [];

        public TaskCompletionSource<IJSObjectReference>? PendingConstructor { get; init; }

        // Only the overload without a token keeps the interop timeout, so the token overloads are left unimplemented.
        public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object?[]? args)
        {
            Constructed.Add(identifier);

            return PendingConstructor is { } pending
                ? new ValueTask<IJSObjectReference>(pending.Task)
                : ValueTask.FromResult<IJSObjectReference>(Controller);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new NotSupportedException();
    }

    private sealed class RecordingJSObjectReference : IJSObjectReference
    {
        public RecordingJSObjectReference? Signal { get; init; }

        public List<string> RequestedValues { get; } = [];

        public List<string> Invocations { get; } = [];

        public Exception? GetValueException { get; set; }

        public Exception? InvokeException { get; set; }

        public int DisposeCount { get; private set; }

        public bool Disposed => DisposeCount > 0;

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier)
        {
            RequestedValues.Add(identifier);

            return GetValueException is { } exception
                ? ValueTask.FromException<TValue>(exception)
                : ValueTask.FromResult((TValue)(object)Signal!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add(identifier);

            return InvokeException is { } exception
                ? ValueTask.FromException<TValue>(exception)
                : ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            DisposeCount++;

            return ValueTask.CompletedTask;
        }
    }
}
