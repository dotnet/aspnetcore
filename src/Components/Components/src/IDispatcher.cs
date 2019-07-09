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
    public interface IDispatcher
    {
        /// <summary>
        /// Invokes the given <see cref="Action"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="workItem">The action to execute.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the action has finished executing.</returns>
        Task InvokeAsync(Action workItem);

        /// <summary>
        /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="workItem">The asynchronous action to execute.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the action has finished executing.</returns>
        Task InvokeAsync(Func<Task> workItem);

        /// <summary>
        /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="workItem">The function to execute.</param>
        /// <returns>A <see cref="Task{TResult}"/> that will be completed when the function has finished executing.</returns>
        Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem);

        /// <summary>
        /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
        /// </summary>
        /// <param name="workItem">The asynchronous function to execute.</param>
        /// <returns>A <see cref="Task{TResult}"/> that will be completed when the function has finished executing.</returns>
        Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem);
    }
}
