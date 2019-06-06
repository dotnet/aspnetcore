// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// Dispatches external actions to be executed on the context of a <see cref="Renderer"/>.
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Invokes the given <see cref="Action"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the action has finished executing.</returns>
        Task Invoke(Action action);

        /// <summary>
        /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to execute.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the action has finished executing.</returns>
        Task InvokeAsync(Func<Task> asyncAction);

        /// <summary>
        /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="function">The function to execute.</param>
        /// <returns>A <see cref="Task{TResult}"/> that will be completed when the function has finished executing.</returns>
        Task<TResult> Invoke<TResult>(Func<TResult> function);

        /// <summary>
        /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="asyncFunction">The asynchronous function to execute.</param>
        /// <returns>A <see cref="Task{TResult}"/> that will be completed when the function has finished executing.</returns>
        Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> asyncFunction);
    }
}
