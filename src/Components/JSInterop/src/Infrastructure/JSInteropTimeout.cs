// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop.Infrastructure;

internal static class JSInteropTimeout
{
    public static ValueTask<T> InvokeAsync<T>(string identifier, TimeSpan timeout, Func<CancellationToken, ValueTask<T>> invoke)
    {
        var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        try
        {
            var task = invoke(cancellationTokenSource?.Token ?? CancellationToken.None);
            return AwaitAsync(task, identifier, timeout, cancellationTokenSource);
        }
        catch (OperationCanceledException ex) when (cancellationTokenSource is not null &&
            cancellationTokenSource.IsCancellationRequested && ex.CancellationToken == cancellationTokenSource.Token)
        {
            cancellationTokenSource.Dispose();
            throw new JSException($"The JS interop call '{identifier}' timed out after {timeout.TotalMilliseconds}ms.", ex);
        }
        catch
        {
            cancellationTokenSource?.Dispose();
            throw;
        }
    }

    public static ValueTask InvokeAsync(string identifier, TimeSpan timeout, Func<CancellationToken, ValueTask> invoke)
    {
        var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        try
        {
            var task = invoke(cancellationTokenSource?.Token ?? CancellationToken.None);
            return AwaitAsync(task, identifier, timeout, cancellationTokenSource);
        }
        catch (OperationCanceledException ex) when (cancellationTokenSource is not null &&
            cancellationTokenSource.IsCancellationRequested && ex.CancellationToken == cancellationTokenSource.Token)
        {
            cancellationTokenSource.Dispose();
            throw new JSException($"The JS interop call '{identifier}' timed out after {timeout.TotalMilliseconds}ms.", ex);
        }
        catch
        {
            cancellationTokenSource?.Dispose();
            throw;
        }
    }

    private static async ValueTask<T> AwaitAsync<T>(ValueTask<T> task, string identifier, TimeSpan timeout, CancellationTokenSource? cancellationTokenSource)
    {
        using (cancellationTokenSource)
        {
            try
            {
                return await task;
            }
            catch (OperationCanceledException ex) when (cancellationTokenSource is not null &&
                cancellationTokenSource.IsCancellationRequested && ex.CancellationToken == cancellationTokenSource.Token)
            {
                throw new JSException($"The JS interop call '{identifier}' timed out after {timeout.TotalMilliseconds}ms.", ex);
            }
        }
    }

    private static async ValueTask AwaitAsync(ValueTask task, string identifier, TimeSpan timeout, CancellationTokenSource? cancellationTokenSource)
    {
        using (cancellationTokenSource)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException ex) when (cancellationTokenSource is not null &&
                cancellationTokenSource.IsCancellationRequested && ex.CancellationToken == cancellationTokenSource.Token)
            {
                throw new JSException($"The JS interop call '{identifier}' timed out after {timeout.TotalMilliseconds}ms.", ex);
            }
        }
    }
}
