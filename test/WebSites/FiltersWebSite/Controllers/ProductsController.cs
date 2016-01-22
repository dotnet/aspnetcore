// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    // This controller will list the filters that are configured for each action in a header.
    // This exercises the merging of filters with the global filters collection.
    [ControllerResultFilter]
    [ControllerActionFilter]
    [ControllerAuthorizationFilter]
    [TracingResourceFilter("Controller Resource Filter")]
    public class ProductsController : Controller, IResultFilter
    {
        [PassThroughResultFilter]
        [PassThroughActionFilter]
        [AuthorizeUser]
        [TracingResourceFilter("Action Resource Filter")]
        public IActionResult GetPrice(int id)
        {
            Response.Headers.Append("filters", "Executing Action");
            // This skips the ExecuteResultAsync in ActionResult. Thus result is not set.
            // Hence we can see all the OnResultExecuted functions in the response.
            return new TestActionResult();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Response.Headers.Append("filters", "Controller Override - OnActionExecuting");
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            context.HttpContext.Response.Headers.Append("filters", "Controller Override - OnActionExecuted");
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            context.HttpContext.Response.Headers.Append("filters", "Controller Override - OnResultExecuted");
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers.Append("filters", "Controller Override - OnResultExecuting");
        }
    }
}