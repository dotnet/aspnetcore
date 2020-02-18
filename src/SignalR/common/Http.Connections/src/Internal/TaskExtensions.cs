// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    internal static class TaskExtensions
    {
        public static async Task NoThrow(this Task task)
        {
            await new NoThrowAwaiter(task);
        }
    }
    internal readonly struct NoThrowAwaiter : ICriticalNotifyCompletion
    {
        private readonly Task _task;
        public NoThrowAwaiter(Task task) { _task = task; }
        public NoThrowAwaiter GetAwaiter() => this;
        public bool IsCompleted => _task.IsCompleted;
        // Observe exception
        public void GetResult() { _ = _task.Exception; }
        public void OnCompleted(Action continuation) => _task.GetAwaiter().OnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
    }
}
