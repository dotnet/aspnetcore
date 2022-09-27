// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages;

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
