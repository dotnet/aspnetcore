// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages;

[TestPageFilter]
public class ViewDataAvailableAfterHandlerExecutedModel : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }

    private class TestPageFilterAttribute : Attribute, IPageFilter
    {
        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
            // This usage mimics Identity UI where it sets data into ViewData in a PageFilters's
            // PageHandlerExecuted method.
            if (context.Result is PageResult pageResult)
            {
                pageResult.ViewData["Foo"] = "Bar";
            }
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }
    }
}
