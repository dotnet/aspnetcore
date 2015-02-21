// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ActionFilterAttribute :
        Attribute, IActionFilter, IAsyncActionFilter, IResultFilter, IAsyncResultFilter, IOrderedFilter
    {
        public int Order { get; set; }

        public virtual void OnActionExecuting([NotNull] ActionExecutingContext context)
        {
        }

        public virtual void OnActionExecuted([NotNull] ActionExecutedContext context)
        {
        }

        public virtual async Task OnActionExecutionAsync(
            [NotNull] ActionExecutingContext context,
            [NotNull] ActionExecutionDelegate next)
        {
            OnActionExecuting(context);
            if (context.Result == null)
            {
                OnActionExecuted(await next());
            }
        }

        public virtual void OnResultExecuting([NotNull] ResultExecutingContext context)
        {
        }

        public virtual void OnResultExecuted([NotNull] ResultExecutedContext context)
        {
        }

        public virtual async Task OnResultExecutionAsync(
            [NotNull] ResultExecutingContext context,
            [NotNull] ResultExecutionDelegate next)
        {
            OnResultExecuting(context);
            if (!context.Cancel)
            {
                OnResultExecuted(await next());
            }
        }
    }
}