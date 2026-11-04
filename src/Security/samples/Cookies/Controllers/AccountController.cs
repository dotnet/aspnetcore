// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AuthSamples.Cookies.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    private bool ValidateLogin(string userName, string password)
    {
        // For this sample, all logins are successful.
        return true;
    }

    [HttpPost]
    public async Task<IActionResult> Login(string userName, string password, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        // Normally Identity handles sign in, but you can do it directly
        if (ValidateLogin(userName, password))
        {
            var claims = new List<Claim>
                {
                    new Claim("user", userName),
                    new Claim("role", "Member")
                };

            await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies", "user", "role")));

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return Redirect("/");
            }
        }

        return View();
    }

    public IActionResult AccessDenied(string returnUrl = null)
    {
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Redirect("/");
    }
}
