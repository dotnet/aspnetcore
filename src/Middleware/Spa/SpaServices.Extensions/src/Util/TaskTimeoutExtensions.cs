// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SpaServices.Extensions.Util;

internal static class TaskTimeoutExtensions
{
    public static async Task WithTimeout(this Task task, TimeSpan timeoutDelay, string message)
    {
        if (task == await Task.WhenAny(task, Task.Delay(timeoutDelay)))
        {
            task.Wait(); // Allow any errors to propagate
        }
        else
        {
            throw new TimeoutException(message);
        }
    }

    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeoutDelay, string message)
    {
        if (task == await Task.WhenAny(task, Task.Delay(timeoutDelay)))
        {
            return task.Result;
        }
        else
        {
            throw new TimeoutException(message);
        }
    }
}
