// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace BasicWebSite
{
    public class TraceResultOutputFilter : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var trace = context.HttpContext.Items[nameof(TraceResourceFilter)];
            context.Result = new ObjectResult(trace);
        }
    }
}
