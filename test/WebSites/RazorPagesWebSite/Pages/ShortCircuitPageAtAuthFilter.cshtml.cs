// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages
{
    [AsyncTestAuthorizationFilter]
    [SyncTestAuthorizationFilter]
    public class ShortCircuitAtAuthFilterPageModel : PageModel
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

        private class AsyncTestAuthorizationFilterAttribute : Attribute, IAsyncAuthorizationFilter
        {
            public Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                if (ShouldShortCircuit(context.HttpContext, nameof(OnAuthorizationAsync)))
                {
                    context.Result = new PageResult();
                }
                return Task.CompletedTask;
            }
        }

        private class SyncTestAuthorizationFilterAttribute : Attribute, IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationFilterContext context)
            {
                if (ShouldShortCircuit(context.HttpContext, nameof(OnAuthorization)))
                {
                    context.Result = new PageResult();
                }
            }
        }
    }
}
