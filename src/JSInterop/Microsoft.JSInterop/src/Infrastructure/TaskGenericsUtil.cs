// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.JSInterop.Infrastructure;

internal static class TaskGenericsUtil
{
    private static readonly ConcurrentDictionary<Type, ITaskResultGetter> _cachedResultGetters = [];

    public static object? GetTaskResult(Task task)
    {
        var getter = _cachedResultGetters.GetOrAdd(task.GetType(), taskInstanceType =>
        {
            var resultType = GetTaskResultType(taskInstanceType);
            return resultType == null
                ? new VoidTaskResultGetter()
                : (ITaskResultGetter)Activator.CreateInstance(
                    typeof(TaskResultGetter<>).MakeGenericType(resultType))!;
        });
        return getter.GetResult(task);
    }

    private static Type? GetTaskResultType(Type taskType)
    {
        // It might be something derived from Task or Task<T>, so we have to scan
        // up the inheritance hierarchy to find the Task or Task<T>
        while (taskType != typeof(Task) &&
            (!taskType.IsGenericType || taskType.GetGenericTypeDefinition() != typeof(Task<>)))
        {
            taskType = taskType.BaseType
                ?? throw new ArgumentException($"The type '{taskType.FullName}' is not inherited from '{typeof(Task).FullName}'.");
        }

        return taskType.IsGenericType
            ? taskType.GetGenericArguments()[0]
            : null;
    }

    private interface ITaskResultGetter
    {
        object? GetResult(Task task);
    }

    private sealed class TaskResultGetter<T> : ITaskResultGetter
    {
        public object? GetResult(Task task) => ((Task<T>)task).Result!;
    }

    private sealed class VoidTaskResultGetter : ITaskResultGetter
    {
        public object? GetResult(Task task)
        {
            task.Wait(); // Throw if the task failed
            return null;
        }
    }
}
