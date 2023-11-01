// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.InternalTesting;

// Copied from https://github.com/dotnet/extensions/blob/master/src/TestingUtils/Microsoft.AspNetCore.InternalTesting/src/TaskExtensions.cs
// Required because Microsoft.AspNetCore.InternalTesting is not shipped
internal static class TaskExtensions
{
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

    private static string CreateMessage(TimeSpan timeout, string filePath, int lineNumber)
        => string.IsNullOrEmpty(filePath)
        ? $"The operation timed out after reaching the limit of {timeout.TotalMilliseconds}ms."
        : $"The operation at {filePath}:{lineNumber} timed out after reaching the limit of {timeout.TotalMilliseconds}ms.";
}
