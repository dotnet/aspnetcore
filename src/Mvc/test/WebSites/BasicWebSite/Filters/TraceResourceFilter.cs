// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite
{
    public class TraceResourceFilter : IResourceFilter
    {

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            context.HttpContext.Items[nameof(TraceResourceFilter)] = $"This value was set by {nameof(TraceResourceFilter)}";
        }
    }
}
