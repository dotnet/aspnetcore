// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        public static async Task<T> OrTimeout<T>(this Task<T> task, int timeout = 30, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            await OrTimeout((Task)task, timeout, file, line);
            return task.Result;
        }

        public static async Task OrTimeout(this Task task, int timeout = 30, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            var finished = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timeout)));
            if (!ReferenceEquals(finished, task))
            {
                throw new TimeoutException($"Task exceeded max running time of {timeout}s at {file}:{line}");
            }
        }
    }
}