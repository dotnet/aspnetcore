// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.UI.Areas.Identity.Filters;

internal sealed class ExternalLoginsPageFilter<TUser> : IAsyncPageFilter where TUser : class
{
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var result = await next();
        if (result.Result is PageResult page)
        {
            var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<TUser>>();
            var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
            var hasExternalLogins = schemes.Any();

            page.ViewData["ManageNav.HasExternalLogins"] = hasExternalLogins;
        }
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }
}
