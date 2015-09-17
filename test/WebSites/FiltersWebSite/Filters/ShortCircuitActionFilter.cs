// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace FiltersWebSite
{
    public class ShortCircuitActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.Result = new ContentResult
            {
                Content = "The Action was never executed",
                ContentType = new MediaTypeHeaderValue("text/plain")
            };
        }
    }
}