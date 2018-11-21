// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SpaServices.Extensions.Util
{
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
}
