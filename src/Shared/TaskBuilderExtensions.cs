// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    internal static class TaskBuilderExtensions
    {
        private static OperationCanceledException s_operationCanceledException = new();

        public static void SetResult(this AsyncTaskMethodBuilder atmb, bool runContinuationsAsynchronously)
        {
            if (!runContinuationsAsynchronously)
            {
                atmb.SetResult();
                return;
            }

#if NET6_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(static atmb => atmb.SetResult(), atmb, preferLocal: true);
#else
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                AsyncTaskMethodBuilder atmb = Unsafe.Unbox<AsyncTaskMethodBuilder>(state);
                atmb.SetResult();
            }, atmb);
#endif
        }

        public static void SetResult<TResult>(this AsyncTaskMethodBuilder<TResult> atmb, TResult result, bool runContinuationsAsynchronously)
        {
            if (!runContinuationsAsynchronously)
            {
                atmb.SetResult(result);
                return;
            }

#if NET6_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                var (atmb, result) = state;
                atmb.SetResult(result);
            }, (atmb, result), preferLocal: true);
#else
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                var (atmb, result) = Unsafe.Unbox<(AsyncTaskMethodBuilder<TResult>, TResult)>(state);
                atmb.SetResult(result);
            }, (atmb, result));
#endif
        }

        public static void SetResult<TResult>(this AsyncValueTaskMethodBuilder<TResult> avtmb, TResult result, bool runContinuationsAsynchronously)
        {
            if (!runContinuationsAsynchronously)
            {
                avtmb.SetResult(result);
                return;
            }

            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                var (avtmb, result) = state;
                avtmb.SetResult(result);
            }, (avtmb, result), preferLocal: true);
        }

        public static void SetException(this AsyncTaskMethodBuilder atmb, Exception exception, bool runContinuationsAsynchronously)
        {
            if (!runContinuationsAsynchronously)
            {
                atmb.SetException(exception);
                return;
            }

#if NET6_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                var (atmb, exception) = state;
                atmb.SetException(exception);
            }, (atmb, exception), preferLocal: true);
#else
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                var (atmb, exception) = Unsafe.Unbox<(AsyncTaskMethodBuilder, Exception)>(state);
                atmb.SetException(exception);
            }, (atmb, exception));
#endif
        }

        public static void SetException<TResult>(this AsyncTaskMethodBuilder<TResult> atmb, Exception exception, bool runContinuationsAsynchronously)
        {
            if (!runContinuationsAsynchronously)
            {
                atmb.SetException(exception);
                return;
            }

#if NET6_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                var (atmb, exception) = state;
                atmb.SetException(exception);
            }, (atmb, exception), preferLocal: true);
#else
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                var (atmb, exception) = Unsafe.Unbox<(AsyncTaskMethodBuilder<TResult>, Exception)>(state);
                atmb.SetException(exception);
            }, (atmb, exception));
#endif
        }

        public static void SetException<TResult>(this AsyncValueTaskMethodBuilder<TResult> avtmb, Exception exception, bool runContinuationsAsynchronously)
        {
            if (!runContinuationsAsynchronously)
            {
                avtmb.SetException(exception);
                return;
            }

            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                var (atmb, exception) = state;
                atmb.SetException(exception);
            }, (avtmb, exception), preferLocal: true);
        }

        public static void SetCanceled(this AsyncTaskMethodBuilder atmb, bool runContinuationsAsynchronously = false, OperationCanceledException? operationCanceledException = null)
        {
            // This will mark the Task as canceled internally.
            SetException(atmb, operationCanceledException ?? s_operationCanceledException, runContinuationsAsynchronously);
        }

        public static void SetCanceled<TResult>(this AsyncTaskMethodBuilder<TResult> atmb, bool runContinuationsAsynchronously = false, OperationCanceledException? operationCanceledException = null)
        {
            // This will mark the Task as canceled internally.
            SetException(atmb, operationCanceledException ?? s_operationCanceledException, runContinuationsAsynchronously);
        }
    }
}
