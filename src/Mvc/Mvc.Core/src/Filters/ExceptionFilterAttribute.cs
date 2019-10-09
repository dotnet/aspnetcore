// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// An abstract filter that runs asynchronously after an action has thrown an <see cref="Exception"/>. Subclasses
    /// must override <see cref="OnException"/> or <see cref="OnExceptionAsync"/> but not both.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ExceptionFilterAttribute : Attribute, IAsyncExceptionFilter, IExceptionFilter, IOrderedFilter
    {
        /// <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public virtual Task OnExceptionAsync(ExceptionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            OnException(context);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual void OnException(ExceptionContext context)
        {
        }
    }
}
