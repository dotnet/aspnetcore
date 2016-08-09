// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TestSyncResourceFilter : Attribute, IResourceFilter, IOrderedFilter
    {
        public enum Action
        {
            PassThrough,
            ThrowException,
            Shortcircuit
        }

        public readonly string ExceptionMessage = $"Error!! in {nameof(TestSyncResourceFilter)}";

        public readonly string ShortcircuitMessage = $"Shortcircuited by {nameof(TestSyncResourceFilter)}";

        public Action FilterAction { get; set; }

        public int Order { get; set; }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (FilterAction == Action.PassThrough)
            {
                return;
            }
            else if (FilterAction == Action.ThrowException)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }
            else
            {
                context.Result = new ContentResult()
                {
                    Content = ShortcircuitMessage,
                    StatusCode = 400,
                    ContentType = "text/abcd"
                };
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TestAsyncResourceFilter : Attribute, IAsyncResourceFilter, IOrderedFilter
    {
        public enum Action
        {
            PassThrough,
            ThrowException,
            Shortcircuit
        }

        public readonly string ExceptionMessage = $"Error!! in {nameof(TestAsyncResourceFilter)}";

        public readonly string ShortcircuitMessage = $"Shortcircuited by {nameof(TestAsyncResourceFilter)}";

        public Action FilterAction { get; set; }

        public int Order { get; set; }

        public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (FilterAction == Action.PassThrough)
            {
                return next();
            }
            else if (FilterAction == Action.ThrowException)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }
            else
            {
                context.Result = new ContentResult()
                {
                    Content = ShortcircuitMessage,
                    StatusCode = 400,
                    ContentType = "text/abcd"
                };
                return Task.FromResult(true);
            }
        }
    }
}
