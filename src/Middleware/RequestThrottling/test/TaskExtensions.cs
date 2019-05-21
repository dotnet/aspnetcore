// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Testing;

namespace System.Threading.Tasks
{
#if TESTUTILS
    public
#else
    internal
#endif
    static class TaskExtensions
    {
        private const int DefaultTimeout = 30 * 1000;

        public static Task OrTimeout(this Task task, int milliseconds = DefaultTimeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds), memberName, filePath, lineNumber);
        }

        public static Task OrTimeout(this Task task, TimeSpan timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return task.TimeoutAfter(timeout, filePath, lineNumber ?? 0);
        }

        public static Task<T> OrTimeout<T>(this ValueTask<T> task, int milliseconds = DefaultTimeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null) =>
            OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds), memberName, filePath, lineNumber);

        public static Task<T> OrTimeout<T>(this ValueTask<T> task, TimeSpan timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null) =>
            task.AsTask().OrTimeout(timeout, memberName, filePath, lineNumber);

        public static Task<T> OrTimeout<T>(this Task<T> task, int milliseconds = DefaultTimeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds), memberName, filePath, lineNumber);
        }

        public static Task<T> OrTimeout<T>(this Task<T> task, TimeSpan timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return task.TimeoutAfter(timeout, filePath, lineNumber ?? 0);
        }

        public static async Task OrThrowIfOtherFails(this Task task, Task otherTask)
        {
            var completed = await Task.WhenAny(task, otherTask);
            if (completed == otherTask && otherTask.IsFaulted)
            {
                // Manifest the exception
                otherTask.GetAwaiter().GetResult();
                throw new Exception("Unreachable code");
            }
            else
            {
                // Await the task we were asked to await. Either it's finished, or the otherTask finished successfully, and it's not our job to check that
                await task;
            }
        }

        public static async Task<T> OrThrowIfOtherFails<T>(this Task<T> task, Task otherTask)
        {
            await OrThrowIfOtherFails((Task)task, otherTask);

            // If we get here, 'task' is finished and succeeded.
            return task.GetAwaiter().GetResult();
        }
    }
}
