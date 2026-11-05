// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class DotNetInvocationCancellation : IDisposable
{
    private readonly object _lock = new();
    private readonly CancellationTokenSource _source = new();
    private Task? _cancellation;
    private bool _disposed;

    public CancellationToken Token => _source.Token;

    public Task CancelAsync()
    {
        lock (_lock)
        {
            return _disposed ? Task.CompletedTask : _cancellation ??= _source.CancelAsync();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_cancellation is { IsCompleted: false })
            {
                // The invocation can finish while its cancellation callbacks are still running.
                _ = _cancellation.ContinueWith(_ => _source.Dispose(), TaskScheduler.Default);
            }
            else
            {
                _source.Dispose();
            }
        }
    }
}
