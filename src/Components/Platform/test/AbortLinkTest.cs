// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class AbortLinkTest
{
    [Fact]
    public async Task CreateAsync_ReturnsNullWithoutAnyCallForATokenThatCannotBeCanceled()
    {
        var jsRuntime = new RecordingJSRuntime();

        var link = await AbortLink.CreateAsync(jsRuntime, CancellationToken.None);

        Assert.Null(link);
        Assert.Empty(jsRuntime.Constructed);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWithoutAnyCallForACanceledToken()
    {
        var jsRuntime = new RecordingJSRuntime();
        var canceled = new CancellationToken(canceled: true);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AbortLink.CreateAsync(jsRuntime, canceled).AsTask());

        Assert.Equal(canceled, exception.CancellationToken);
        Assert.Empty(jsRuntime.Constructed);
    }

    [Fact]
    public async Task CreateAsync_CreatesAControllerAndReadsItsSignal()
    {
        var jsRuntime = new RecordingJSRuntime();
        using var cts = new CancellationTokenSource();

        await using var link = await AbortLink.CreateAsync(jsRuntime, cts.Token);

        Assert.NotNull(link);
        Assert.Same(jsRuntime.Signal, link.Signal);
        Assert.Equal(["AbortController"], jsRuntime.Constructed);
        Assert.Equal(["signal"], jsRuntime.Controller.RequestedValues);
        Assert.Empty(jsRuntime.Controller.Invocations);
    }

    [Fact]
    public async Task Cancel_AbortsTheController()
    {
        var jsRuntime = new RecordingJSRuntime();
        using var cts = new CancellationTokenSource();
        await using var link = await AbortLink.CreateAsync(jsRuntime, cts.Token);

        cts.Cancel();

        Assert.Equal(["abort"], jsRuntime.Controller.Invocations);
    }

    [Fact]
    public async Task Cancel_DoesNotThrowWhenAbortFails()
    {
        var jsRuntime = new RecordingJSRuntime();
        jsRuntime.Controller.InvokeException = new JSDisconnectedException("The circuit is disconnected.");
        using var cts = new CancellationTokenSource();
        await using var link = await AbortLink.CreateAsync(jsRuntime, cts.Token);

        cts.Cancel();

        Assert.Equal(["abort"], jsRuntime.Controller.Invocations);
    }

    [Fact]
    public async Task CreateAsync_ThrowsAndReleasesTheReferencesWhenCanceledDuringCreation()
    {
        var pending = new TaskCompletionSource<IJSObjectReference>();
        var jsRuntime = new RecordingJSRuntime { PendingConstructor = pending };
        using var cts = new CancellationTokenSource();

        var creation = AbortLink.CreateAsync(jsRuntime, cts.Token).AsTask();
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
        var disconnected = new JSDisconnectedException("The circuit is disconnected.");
        jsRuntime.Controller.GetValueException = disconnected;
        using var cts = new CancellationTokenSource();

        var exception = await Assert.ThrowsAsync<JSDisconnectedException>(() => AbortLink.CreateAsync(jsRuntime, cts.Token).AsTask());

        Assert.Same(disconnected, exception);
        Assert.True(jsRuntime.Controller.Disposed);
    }

    [Fact]
    public async Task DisposeAsync_StopsListeningAndDisposesBothReferences()
    {
        var jsRuntime = new RecordingJSRuntime();
        using var cts = new CancellationTokenSource();
        var link = await AbortLink.CreateAsync(jsRuntime, cts.Token);

        await link!.DisposeAsync();
        cts.Cancel();

        Assert.Empty(jsRuntime.Controller.Invocations);
        Assert.True(jsRuntime.Controller.Disposed);
        Assert.True(jsRuntime.Signal.Disposed);
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

        public bool Disposed { get; private set; }

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
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }
}
