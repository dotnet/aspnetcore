// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Tasks;
#if TESTUTILS
    public
#else
internal
#endif
    static class TaskExtensions
{
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
