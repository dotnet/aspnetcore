// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    internal static class ForceAsyncTaskExtensions
    {
        /// <summary>
        /// Returns an awaitable/awaiter that will ensure the continuation is executed
        /// asynchronously on the thread pool, even if the task is already completed
        /// by the time the await occurs.  Effectively, it is equivalent to awaiting
        /// with ConfigureAwait(false) and then queuing the continuation with Task.Run,
        /// but it avoids the extra hop if the continuation already executed asynchronously.
        /// </summary>
        public static ForceAsyncAwaiter ForceAsync(this Task task)
        {
            return new ForceAsyncAwaiter(task);
        }

        public static ForceAsyncAwaiter<T> ForceAsync<T>(this Task<T> task)
        {
            return new ForceAsyncAwaiter<T>(task);
        }
    }

    internal readonly struct ForceAsyncAwaiter : ICriticalNotifyCompletion
    {
        private readonly Task _task;

        internal ForceAsyncAwaiter(Task task) { _task = task; }

        public ForceAsyncAwaiter GetAwaiter() { return this; }

        // The purpose of this type is to always force a continuation
        public bool IsCompleted => false;

        public void GetResult() { _task.GetAwaiter().GetResult(); }

        public void OnCompleted(Action action)
        {
            _task.ConfigureAwait(false).GetAwaiter().OnCompleted(action);
        }

        public void UnsafeOnCompleted(Action action)
        {
            _task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(action);
        }
    }

    internal readonly struct ForceAsyncAwaiter<T> : ICriticalNotifyCompletion
    {
        private readonly Task<T> _task;

        internal ForceAsyncAwaiter(Task<T> task) { _task = task; }

        public ForceAsyncAwaiter<T> GetAwaiter() { return this; }

        // The purpose of this type is to always force a continuation
        public bool IsCompleted => false;

        public T GetResult() { return _task.GetAwaiter().GetResult(); }

        public void OnCompleted(Action action)
        {
            _task.ConfigureAwait(false).GetAwaiter().OnCompleted(action);
        }

        public void UnsafeOnCompleted(Action action)
        {
            _task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(action);
        }
    }
}
