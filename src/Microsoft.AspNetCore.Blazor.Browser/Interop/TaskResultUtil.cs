// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

internal class TaskResultUtil
{
    private static ConcurrentDictionary<Type, ITaskResultGetter> _cachedGetters = new ConcurrentDictionary<Type, ITaskResultGetter>();

    private interface ITaskResultGetter
    {
        object GetResult(Task task);
    }

    private class TaskResultGetter<T> : ITaskResultGetter
    {
        public object GetResult(Task task) => ((Task<T>)task).Result;
    }

    public static object GetTaskResult(Task task)
    {
        var getter = _cachedGetters.GetOrAdd(task.GetType(), taskType =>
        {
            var resultType = taskType.GetGenericArguments().Single();
            return (ITaskResultGetter)Activator.CreateInstance(
                typeof(TaskResultGetter<>).MakeGenericType(resultType));
        });
        return getter.GetResult(task);
    }
}