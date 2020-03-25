// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Helper methods for using Tasks to implement the APM pattern.
//
// Example usage, wrapping a Task<int>-returning FooAsync method with Begin/EndFoo methods:
//
//     public IAsyncResult BeginFoo(..., AsyncCallback callback, object state) =>
//         TaskToApm.Begin(FooAsync(...), callback, state);
//
//     public int EndFoo(IAsyncResult asyncResult) =>
//         TaskToApm.End<int>(asyncResult);

#nullable enable
using System.Diagnostics;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides support for efficiently using Tasks to implement the APM (Begin/End) pattern.
    /// </summary>
    internal static class TaskToApm
    {
        /// <summary>
        /// Marshals the Task as an IAsyncResult, using the supplied callback and state
        /// to implement the APM pattern.
        /// </summary>
        /// <param name="task">The Task to be marshaled.</param>
        /// <param name="callback">The callback to be invoked upon completion.</param>
        /// <param name="state">The state to be stored in the IAsyncResult.</param>
        /// <returns>An IAsyncResult to represent the task's asynchronous operation.</returns>
        public static IAsyncResult Begin(Task task, AsyncCallback? callback, object? state) =>
            new TaskAsyncResult(task, state, callback);

        /// <summary>Processes an IAsyncResult returned by Begin.</summary>
        /// <param name="asyncResult">The IAsyncResult to unwrap.</param>
        public static void End(IAsyncResult asyncResult)
        {
            if (asyncResult is TaskAsyncResult twar)
            {
                twar._task.GetAwaiter().GetResult();
                return;
            }

            throw new ArgumentNullException();
        }

        /// <summary>Processes an IAsyncResult returned by Begin.</summary>
        /// <param name="asyncResult">The IAsyncResult to unwrap.</param>
        public static TResult End<TResult>(IAsyncResult asyncResult)
        {
            if (asyncResult is TaskAsyncResult twar && twar._task is Task<TResult> task)
            {
                return task.GetAwaiter().GetResult();
            }

            throw new ArgumentNullException();
        }

        /// <summary>Provides a simple IAsyncResult that wraps a Task.</summary>
        /// <remarks>
        /// We could use the Task as the IAsyncResult if the Task's AsyncState is the same as the object state,
        /// but that's very rare, in particular in a situation where someone cares about allocation, and always
        /// using TaskAsyncResult simplifies things and enables additional optimizations.
        /// </remarks>
        internal sealed class TaskAsyncResult : IAsyncResult
        {
            /// <summary>The wrapped Task.</summary>
            internal readonly Task _task;
            /// <summary>Callback to invoke when the wrapped task completes.</summary>
            private readonly AsyncCallback? _callback;

            /// <summary>Initializes the IAsyncResult with the Task to wrap and the associated object state.</summary>
            /// <param name="task">The Task to wrap.</param>
            /// <param name="state">The new AsyncState value.</param>
            /// <param name="callback">Callback to invoke when the wrapped task completes.</param>
            internal TaskAsyncResult(Task task, object? state, AsyncCallback? callback)
            {
                Debug.Assert(task != null);
                _task = task;
                AsyncState = state;

                if (task.IsCompleted)
                {
                    // Synchronous completion.  Invoke the callback.  No need to store it.
                    CompletedSynchronously = true;
                    callback?.Invoke(this);
                }
                else if (callback != null)
                {
                    // Asynchronous completion, and we have a callback; schedule it. We use OnCompleted rather than ContinueWith in
                    // order to avoid running synchronously if the task has already completed by the time we get here but still run
                    // synchronously as part of the task's completion if the task completes after (the more common case).
                    _callback = callback;
                    _task.ConfigureAwait(continueOnCapturedContext: false)
                         .GetAwaiter()
                         .OnCompleted(InvokeCallback); // allocates a delegate, but avoids a closure
                }
            }

            /// <summary>Invokes the callback.</summary>
            private void InvokeCallback()
            {
                Debug.Assert(!CompletedSynchronously);
                Debug.Assert(_callback != null);
                _callback.Invoke(this);
            }

            /// <summary>Gets a user-defined object that qualifies or contains information about an asynchronous operation.</summary>
            public object? AsyncState { get; }
            /// <summary>Gets a value that indicates whether the asynchronous operation completed synchronously.</summary>
            /// <remarks>This is set lazily based on whether the <see cref="_task"/> has completed by the time this object is created.</remarks>
            public bool CompletedSynchronously { get; }
            /// <summary>Gets a value that indicates whether the asynchronous operation has completed.</summary>
            public bool IsCompleted => _task.IsCompleted;
            /// <summary>Gets a <see cref="WaitHandle"/> that is used to wait for an asynchronous operation to complete.</summary>
            public WaitHandle AsyncWaitHandle => ((IAsyncResult)_task).AsyncWaitHandle;
        }
    }
}