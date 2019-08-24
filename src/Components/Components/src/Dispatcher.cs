// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Dispatches external actions to be executed on the context of a <see cref="Renderer"/>.
    /// </summary>
    public abstract class Dispatcher
    {
        /// <summary>
        /// Creates a default instance of <see cref="Dispatcher"/>.
        /// </summary>
        /// <returns>A <see cref="Dispatcher"/> instance.</returns>
        public static Dispatcher CreateDefault() => new RendererSynchronizationContextDispatcher();

        /// <summary>
        /// Provides notifications of unhandled exceptions that occur within the dispatcher.
        /// </summary>
        internal event UnhandledExceptionEventHandler UnhandledException;

        /// <summary>
        /// Validates that the currently executing code is running inside the dispatcher.
        /// </summary>
        public void AssertAccess()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException(
                    "The current thread is not associated with the Dispatcher. " +
                    "Use InvokeAsync() to switch execution to the Dispatcher when " +
                    "triggering rendering or component state.");
            }
        }

        /// <summary>
        /// Returns a value that determines whether using the dispatcher to invoke a work item is required
        /// from the current context.
        /// </summary>
        /// <returns><c>true</c> if invoking is required, otherwise <c>false</c>.</returns>
        public abstract bool CheckAccess();

        /// <summary>
        /// Invokes the given <see cref="Action"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="workItem">The action to execute.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the action has finished executing.</returns>
        public abstract Task InvokeAsync(Action workItem);

        /// <summary>
        /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="workItem">The asynchronous action to execute.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the action has finished executing.</returns>
        public abstract Task InvokeAsync(Func<Task> workItem);

        /// <summary>
        /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="workItem">The function to execute.</param>
        /// <returns>A <see cref="Task{TResult}"/> that will be completed when the function has finished executing.</returns>
        public abstract Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem);

        /// <summary>
        /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="workItem">The asynchronous function to execute.</param>
        /// <returns>A <see cref="Task{TResult}"/> that will be completed when the function has finished executing.</returns>
        public abstract Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem);

        /// <summary>
        /// Called to notify listeners of an unhandled exception.
        /// </summary>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/>.</param>
        protected void OnUnhandledException(UnhandledExceptionEventArgs e)
        {
            if (e is null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            UnhandledException?.Invoke(this, e);
        }
    }
}
