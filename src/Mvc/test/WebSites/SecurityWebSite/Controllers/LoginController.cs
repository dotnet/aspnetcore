// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers;

[AllowAnonymous]
public class LoginController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> LoginDefaultScheme()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("ClaimA", "Value") }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(scheme: null, new ClaimsPrincipal(identity));
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> LoginClaimA()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("ClaimA", "Value") }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> LoginClaimAB()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("ClaimA", "Value"), new Claim("ClaimB", "Value") }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Ok();
    }

    public IActionResult LoginBearerClaimA()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("ClaimA", "Value") });
        return Content(BearerAuth.GetTokenText(identity.Claims));
    }
}
