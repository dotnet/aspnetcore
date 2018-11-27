// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    public class ModelAsFilter : PageModel, IResultFilter
    {
        public string Message { get; set; }

        public void OnGet(string message)
        {
            Message = message;
        }

        public IActionResult OnGetTestResultFilter() => NotFound();

        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            context.HandlerArguments["message"] = "Hello from OnPageHandlerExecuting";
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is NotFoundResult)
            {
                context.Result = Redirect("/Different-Location");
            }
        }
    }
}
