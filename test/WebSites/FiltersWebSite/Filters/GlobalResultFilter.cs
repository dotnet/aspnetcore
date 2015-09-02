// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Filters;

namespace FiltersWebSite
{
    public class GlobalResultFilter : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext context)
        {
            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ProductsController.GetPrice")
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "Global Result Filter - OnResultExecuted");
            }
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ResultFilterController.GetHelloWorld")
            {
                context.Result = Helpers.GetContentResult(context.Result, "GlobalResultFilter.OnResultExecuting");
            }

            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ProductsController.GetPrice")
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "Global Result Filter - OnResultExecuted");
            }
        }
    }
}