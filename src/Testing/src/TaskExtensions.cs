// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Testing
{
    public static class TaskExtensions
    {
        private const int DefaultTimeoutDuration = 30 * 1000;

        public static TimeSpan DefaultTimeoutTimeSpan { get; } = TimeSpan.FromMilliseconds(DefaultTimeoutDuration);

        public static Task DefaultTimeout(this Task task, int milliseconds = DefaultTimeoutDuration, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
        }

        public static Task DefaultTimeout(this Task task, TimeSpan timeout, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(timeout, filePath, lineNumber);
        }

        public static Task DefaultTimeout(this ValueTask task, int milliseconds = DefaultTimeoutDuration, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.AsTask().TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
        }

        public static Task DefaultTimeout(this ValueTask task, TimeSpan timeout, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.AsTask().TimeoutAfter(timeout, filePath, lineNumber);
        }

        public static Task<T> DefaultTimeout<T>(this Task<T> task, int milliseconds = DefaultTimeoutDuration, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
        }

        public static Task<T> DefaultTimeout<T>(this Task<T> task, TimeSpan timeout, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(timeout, filePath, lineNumber);
        }

        public static Task<T> DefaultTimeout<T>(this ValueTask<T> task, int milliseconds = DefaultTimeoutDuration, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.AsTask().TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
        }

        public static Task<T> DefaultTimeout<T>(this ValueTask<T> task, TimeSpan timeout, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.AsTask().TimeoutAfter(timeout, filePath, lineNumber);
        }


        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            // Don't create a timer if the task is already completed
            // or the debugger is attached
            if (task.IsCompleted || Debugger.IsAttached)
            {
                return await task;
            }
#if NET6_0
            try
            {
                return await task.WaitAsync(timeout);
            }
            catch (TimeoutException ex) when (ex.Source == nameof(Microsoft.AspNetCore.Testing))
            {
                throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
#else
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
#endif
        }

        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            // Don't create a timer if the task is already completed
            // or the debugger is attached
            if (task.IsCompleted || Debugger.IsAttached)
            {
                await task;
                return;
            }
#if NET6_0
            try
            {
                await task.WaitAsync(timeout);
            }
            catch (TimeoutException ex) when (ex.Source == nameof(Microsoft.AspNetCore.Testing))
            {
                throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
#else
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
#endif
        }

        private static string CreateMessage(TimeSpan timeout, string filePath, int lineNumber)
            => string.IsNullOrEmpty(filePath)
            ? $"The operation timed out after reaching the limit of {timeout.TotalMilliseconds}ms."
            : $"The operation at {filePath}:{lineNumber} timed out after reaching the limit of {timeout.TotalMilliseconds}ms.";
    }
}
