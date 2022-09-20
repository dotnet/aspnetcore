// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;

namespace Microsoft.JSInterop.Infrastructure;

internal static class TaskGenericsUtil
{
    private static readonly ConcurrentDictionary<Type, ITaskResultGetter> _cachedResultGetters
        = new ConcurrentDictionary<Type, ITaskResultGetter>();

    private static readonly ConcurrentDictionary<Type, ITcsResultSetter> _cachedResultSetters
        = new ConcurrentDictionary<Type, ITcsResultSetter>();

    public static void SetTaskCompletionSourceResult(object taskCompletionSource, object? result)
        => CreateResultSetter(taskCompletionSource).SetResult(taskCompletionSource, result);

    public static void SetTaskCompletionSourceException(object taskCompletionSource, Exception exception)
        => CreateResultSetter(taskCompletionSource).SetException(taskCompletionSource, exception);

    public static Type GetTaskCompletionSourceResultType(object taskCompletionSource)
        => CreateResultSetter(taskCompletionSource).ResultType;

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

    interface ITcsResultSetter
    {
        Type ResultType { get; }
        void SetResult(object taskCompletionSource, object? result);
        void SetException(object taskCompletionSource, Exception exception);
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

    private sealed class TcsResultSetter<T> : ITcsResultSetter
    {
        public Type ResultType => typeof(T);

        public void SetResult(object tcs, object? result)
        {
            var typedTcs = (TaskCompletionSource<T>)tcs;

            // If necessary, attempt a cast
            var typedResult = result is T resultT
                ? resultT
                : (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture)!;

            typedTcs.SetResult(typedResult!);
        }

        public void SetException(object tcs, Exception exception)
        {
            var typedTcs = (TaskCompletionSource<T>)tcs;
            typedTcs.SetException(exception);
        }
    }

    private static ITcsResultSetter CreateResultSetter(object taskCompletionSource)
    {
        return _cachedResultSetters.GetOrAdd(taskCompletionSource.GetType(), tcsType =>
        {
            var resultType = tcsType.GetGenericArguments()[0];
            return (ITcsResultSetter)Activator.CreateInstance(
                typeof(TcsResultSetter<>).MakeGenericType(resultType))!;
        });
    }
}
