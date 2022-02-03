// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

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
