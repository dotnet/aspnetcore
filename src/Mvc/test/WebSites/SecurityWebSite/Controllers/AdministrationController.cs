// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers;

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
        return Content("Administration.EitherCookie:AuthorizeCount=" + countEvaluator.AuthorizeCount);
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
