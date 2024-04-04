// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;

namespace Microsoft.JSInterop;

internal sealed class JSInteropTask<TResult> : IJSInteropTask
{
    private readonly TaskCompletionSource<TResult> _tcs;
    private readonly CancellationTokenRegistration _cancellationTokenRegistration;
    private readonly Action? _onCanceled;

    public JsonSerializerOptions? DeserializeOptions { get; set; }

    public Task<TResult> Task => _tcs.Task;

    public Type ResultType => typeof(TResult);

    public JSInteropTask(CancellationToken cancellationToken, Action? onCanceled = null)
    {
        _tcs = new TaskCompletionSource<TResult>();
        _onCanceled = onCanceled;

        if (cancellationToken.CanBeCanceled)
        {
            _cancellationTokenRegistration = cancellationToken.Register(Cancel);
        }
    }

    public void SetResult(object? result)
    {
        if (result is not TResult typedResult)
        {
            typedResult = (TResult)Convert.ChangeType(result, typeof(TResult), CultureInfo.InvariantCulture)!;
        }

        _tcs.SetResult(typedResult);
        _cancellationTokenRegistration.Dispose();
    }

    public void SetException(Exception exception)
    {
        _tcs.SetException(exception);
        _cancellationTokenRegistration.Dispose();
    }

    private void Cancel()
    {
        _cancellationTokenRegistration.Dispose();
        _tcs.TrySetCanceled();
        _onCanceled?.Invoke();
    }

    public void Dispose()
    {
        _cancellationTokenRegistration.Dispose();
    }
}
