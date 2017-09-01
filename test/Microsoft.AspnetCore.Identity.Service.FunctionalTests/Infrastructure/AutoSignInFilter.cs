// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Identity.OpenIdConnect.WebSite.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspnetCore.Identity.Service.FunctionalTests
{
    public class AutoSignInFilter : IAsyncActionFilter
    {
        private readonly string _loginPath;

        public AutoSignInFilter(string loginPath = "/tfp/Identity/Account/Login")
        {
            _loginPath = loginPath;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var services = context.HttpContext.RequestServices;

            if (request.Path.StartsWithSegments(
                _loginPath,
                StringComparison.OrdinalIgnoreCase))
            {
                var referenceData = services.GetRequiredService<ReferenceData>();
                var signInManager = services.GetRequiredService<SignInManager<ApplicationUser>>();
                var (user, password) = request.Headers.TryGetValue("X-Identity-Test-User-Hint", out var userName) ?
                    referenceData.GetUser(userName) :
                    referenceData.GetDefaultUser();

                var result = await signInManager.PasswordSignInAsync(user.UserName, password, false, false);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException();
                }

                if (context.ActionArguments.TryGetValue("returnUrl", out var redirect))
                {
                    context.Result = new RedirectResult((string)redirect);
                    return;
                }
            }

            await next();
        }
    }
}
