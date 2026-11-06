// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class CancellationTokenObserver : IDisposable
{
    private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenRegistration _registration;
    internal DotNetObjectReference<CancellationTokenObserver> Reference { get; }

    public CancellationTokenObserver(CancellationToken cancellationToken)
    {
        Reference = DotNetObjectReference.Create(this);
        _registration = cancellationToken.Register(() => _completion.TrySetResult(true));
    }

    [JSInvokable]
    public Task<bool> WaitForCancellationAsync() => _completion.Task;

    [JSInvokable]
    public void Release() => Dispose();

    public void Dispose()
    {
        _registration.Dispose();
        _completion.TrySetResult(false);
        Reference.Dispose();
    }
}
