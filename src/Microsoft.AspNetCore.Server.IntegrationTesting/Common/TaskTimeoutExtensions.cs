// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Threading.Tasks
{
    internal static class TaskTimeoutExtensions
    {
        public static async Task OrTimeout(this Task task, TimeSpan timeout)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeout));
            if (completed == task)
            {
                // Manifest any exception
                task.GetAwaiter().GetResult();
            }
            else
            {
                throw new TimeoutException();
            }
        }

        public static async Task<T> OrTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeout));
            if (completed == task)
            {
                return await task;
            }
            else
            {
                throw new TimeoutException();
            }
        }
    }
}
