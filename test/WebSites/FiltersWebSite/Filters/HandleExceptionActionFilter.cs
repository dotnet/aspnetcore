// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class HandleExceptionActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                context.Result = Helpers.GetContentResult(null, "Hi from Action Filter");

                context.Exception = null;
            }
        }
    }
}