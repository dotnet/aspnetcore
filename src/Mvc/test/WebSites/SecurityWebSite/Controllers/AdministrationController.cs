// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityWebSite.Controllers
{
    // This controller is secured through the globally added authorize filter which
    // allows only authenticated users.
    public class AdministrationController : Controller
    {
        public IActionResult Index()
        {
            return Content("Administration.Index");
        }

        // Either cookie should allow access to this action.
        [Authorize(AuthenticationSchemes = "Cookie2")]
        public IActionResult EitherCookie()
        {
            var countEvaluator = (CountingPolicyEvaluator)HttpContext.RequestServices.GetRequiredService<IPolicyEvaluator>();
            return Content("Administration.EitherCookie:AuthorizeCount="+countEvaluator.AuthorizeCount);
        }

        [AllowAnonymous]
        public IActionResult AllowAnonymousAction()
        {
            return Content("Administration.AllowAnonymousAction");
        }

        [AllowAnonymous]
        public async Task<IActionResult> SignInCookie2()
        {
            await HttpContext.SignInAsync("Cookie2", new ClaimsPrincipal(new ClaimsIdentity("Cookie2")));
            return Content("SignInCookie2");
        }

    }
}
