// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    internal static class TaskGenericsUtil
    {
        private static ConcurrentDictionary<Type, ITaskResultGetter> _cachedResultGetters
            = new ConcurrentDictionary<Type, ITaskResultGetter>();

        private static ConcurrentDictionary<Type, ITcsResultSetter> _cachedResultSetters
            = new ConcurrentDictionary<Type, ITcsResultSetter>();

        public static void SetTaskCompletionSourceResult(object taskCompletionSource, object result)
            => CreateResultSetter(taskCompletionSource).SetResult(taskCompletionSource, result);

        public static void SetTaskCompletionSourceException(object taskCompletionSource, Exception exception)
            => CreateResultSetter(taskCompletionSource).SetException(taskCompletionSource, exception);

        public static object GetTaskResult(Task task)
        {
            var getter = _cachedResultGetters.GetOrAdd(task.GetType(), taskType =>
            {
                if (taskType.IsGenericType)
                {
                    var resultType = taskType.GetGenericArguments().Single();
                    return (ITaskResultGetter)Activator.CreateInstance(
                        typeof(TaskResultGetter<>).MakeGenericType(resultType));
                }
                else
                {
                    return new VoidTaskResultGetter();
                }
            });
            return getter.GetResult(task);
        }

        interface ITcsResultSetter
        {
            void SetResult(object taskCompletionSource, object result);
            void SetException(object taskCompletionSource, Exception exception);
        }

        private interface ITaskResultGetter
        {
            object GetResult(Task task);
        }

        private class TaskResultGetter<T> : ITaskResultGetter
        {
            public object GetResult(Task task) => ((Task<T>)task).Result;
        }

        private class VoidTaskResultGetter : ITaskResultGetter
        {
            public object GetResult(Task task)
            {
                task.Wait(); // Throw if the task failed
                return null;
            }
        }

        private class TcsResultSetter<T> : ITcsResultSetter
        {
            public void SetResult(object tcs, object result)
            {
                var typedTcs = (TaskCompletionSource<T>)tcs;
                typedTcs.SetResult((T)result);
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
                var resultType = tcsType.GetGenericArguments().Single();
                return (ITcsResultSetter)Activator.CreateInstance(
                    typeof(TcsResultSetter<>).MakeGenericType(resultType));
            });
        }
    }
}
