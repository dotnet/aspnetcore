// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        private const int DefaultTimeout = 5000;

        public static Task OrTimeout(this Task task, int milliseconds = DefaultTimeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds), memberName, filePath, lineNumber);
        }

        public static async Task OrTimeout(this Task task, TimeSpan timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            var completed = await Task.WhenAny(task, Task.Delay(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : timeout));
            if (completed != task)
            {
                throw new TimeoutException(GetMessage(memberName, filePath, lineNumber));
            }

            await task;
        }

        public static Task<T> OrTimeout<T>(this Task<T> task, int milliseconds = DefaultTimeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds), memberName, filePath, lineNumber);
        }

        public static async Task<T> OrTimeout<T>(this Task<T> task, TimeSpan timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            var completed = await Task.WhenAny(task, Task.Delay(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : timeout));
            if (completed != task)
            {
                throw new TimeoutException(GetMessage(memberName, filePath, lineNumber));
            }

            return await task;
        }

        private static string GetMessage(string memberName, string filePath, int? lineNumber)
        {
            if (!string.IsNullOrEmpty(memberName))
            {
                return $"Operation in {memberName} timed out at {filePath}:{lineNumber}";
            }
            else
            {
                return "Operation timed out";
            }
        }
    }
}
