// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite;

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
