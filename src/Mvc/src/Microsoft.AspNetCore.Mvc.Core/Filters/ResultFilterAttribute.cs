// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// An abstract filter that asynchronously surrounds execution of the action result. Subclasses
    /// must override <see cref="OnResultExecuting"/>, <see cref="OnResultExecuted"/> or
    /// <see cref="OnResultExecutionAsync"/> but not <see cref="OnResultExecutionAsync"/> and either of the other two.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ResultFilterAttribute : Attribute, IResultFilter, IAsyncResultFilter, IOrderedFilter
    {
        /// <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public virtual void OnResultExecuting(ResultExecutingContext context)
        {
        }

        /// <inheritdoc />
        public virtual void OnResultExecuted(ResultExecutedContext context)
        {
        }

        /// <inheritdoc />
        public virtual async Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            OnResultExecuting(context);
            if (!context.Cancel)
            {
                OnResultExecuted(await next());
            }
        }
    }
}
