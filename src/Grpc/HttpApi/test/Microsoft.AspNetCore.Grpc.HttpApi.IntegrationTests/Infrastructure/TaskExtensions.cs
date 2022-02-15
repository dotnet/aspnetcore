// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Tests.Infrastructure;

internal static class TaskExtensions
{
#if NET472
    // Allow AsTask in tests where the Task/ValueTask is already a task.
    public static Task<T> AsTask<T>(this Task<T> task)
    {
        return task;
    }

    public static Task AsTask(this Task task)
    {
        return task;
    }
#endif

    public static Task<T> DefaultTimeout<T>(this Task<T> task,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = default)
    {
        return task.TimeoutAfter(TimeSpan.FromSeconds(5), filePath, lineNumber);
    }

    public static Task DefaultTimeout(this Task task,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = default)
    {
        return task.TimeoutAfter(TimeSpan.FromSeconds(5), filePath, lineNumber);
    }

    public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = default)
    {
        // Don't create a timer if the task is already completed
        // or the debugger is attached
        if (task.IsCompleted || Debugger.IsAttached)
        {
            return await task;
        }

        var cts = new CancellationTokenSource();
        if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)))
        {
            cts.Cancel();
            return await task;
        }
        else
        {
            throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
        }
    }

    public static async Task TimeoutAfter(this Task task, TimeSpan timeout,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = default)
    {
        // Don't create a timer if the task is already completed
        // or the debugger is attached
        if (task.IsCompleted || Debugger.IsAttached)
        {
            await task;
            return;
        }

        var cts = new CancellationTokenSource();
        if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)))
        {
            cts.Cancel();
            await task;
        }
        else
        {
            throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
        }
    }

    private static string CreateMessage(TimeSpan timeout, string? filePath, int lineNumber)
        => string.IsNullOrEmpty(filePath)
        ? $"The operation timed out after reaching the limit of {timeout.TotalMilliseconds}ms."
        : $"The operation at {filePath}:{lineNumber} timed out after reaching the limit of {timeout.TotalMilliseconds}ms.";

#if !NET472
    public static IAsyncEnumerable<T> DefaultTimeout<T>(this IAsyncEnumerable<T> enumerable,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = default)
    {
        return enumerable.TimeoutAfter(TimeSpan.FromSeconds(5), filePath, lineNumber);
    }

    public static IAsyncEnumerable<T> TimeoutAfter<T>(this IAsyncEnumerable<T> enumerable, TimeSpan timeout,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = default)
    {
        return new TimeoutAsyncEnumerable<T>(enumerable, timeout, filePath, lineNumber);
    }

    private class TimeoutAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _inner;
        private readonly TimeSpan _timeout;
        private readonly string? _filePath;
        private readonly int _lineNumber;

        public TimeoutAsyncEnumerable(IAsyncEnumerable<T> inner, TimeSpan timeout, string? filePath, int lineNumber)
        {
            _inner = inner;
            _timeout = timeout;
            _filePath = filePath;
            _lineNumber = lineNumber;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TimeoutAsyncEnumerator<T>(
                _inner.GetAsyncEnumerator(cancellationToken),
                _timeout,
                _filePath,
                _lineNumber);
        }
    }

    private class TimeoutAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IAsyncEnumerator<T> _enumerator;
        private readonly TimeSpan _timeout;
        private readonly string? _filePath;
        private readonly int _lineNumber;

        public TimeoutAsyncEnumerator(IAsyncEnumerator<T> enumerator, TimeSpan timeout, string? filePath, int lineNumber)
        {
            _enumerator = enumerator;
            _timeout = timeout;
            _filePath = filePath;
            _lineNumber = lineNumber;
        }

        public T Current => _enumerator.Current;

        public ValueTask DisposeAsync()
        {
            return _enumerator.DisposeAsync();
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_enumerator.MoveNextAsync().AsTask().TimeoutAfter(_timeout, _filePath, _lineNumber));
        }
    }
#endif
}
