// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages
{
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
}
