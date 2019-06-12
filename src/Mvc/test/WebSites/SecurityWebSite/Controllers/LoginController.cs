// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers
{
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
}
