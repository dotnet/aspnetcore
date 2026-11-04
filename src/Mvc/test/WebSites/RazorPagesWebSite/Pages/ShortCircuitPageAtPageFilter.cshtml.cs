// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages;

[AsyncTestPageFilter]
[SyncTestPageFilter]
public class ShortCircuitAtPageFilterPageModel : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }

    private static bool ShouldShortCircuit(HttpContext httpContext, string currentTargetName)
    {
        return httpContext.Request.Query.TryGetValue("target", out var expectedTargetName)
            && string.Equals(expectedTargetName, currentTargetName, StringComparison.OrdinalIgnoreCase);
    }

    private class AsyncTestPageFilterAttribute : Attribute, IAsyncPageFilter
    {
        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public Task OnPageHandlerExecutionAsync(
            PageHandlerExecutingContext context,
            PageHandlerExecutionDelegate next)
        {
            if (ShouldShortCircuit(context.HttpContext, nameof(OnPageHandlerExecutionAsync)))
            {
                context.Result = new PageResult();
                return Task.CompletedTask;
            }
            return next();
        }
    }

    private class SyncTestPageFilterAttribute : Attribute, IPageFilter
    {
        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            if (ShouldShortCircuit(context.HttpContext, nameof(OnPageHandlerExecuting)))
            {
                context.Result = new PageResult();
            }
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }
    }
}
