// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Filters;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

// This controller is reachable via traditional routing.
public class AntiforgeryController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Antiforgery/Login
    [AllowAnonymous]
    public ActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        return View();
    }

    [AllowAnonymous]
    public string UseFacebookLogin()
    {
        return "somestring";
    }

    // POST: /Antiforgery/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public string Login(LoginViewModel model)
    {
        return "OK";
    }

    // POST: /Antiforgery/LoginWithRedirectResultFilter
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [TypeFilter<RedirectAntiforgeryValidationFailedResultFilter>]
    public string LoginWithRedirectResultFilter(LoginViewModel model)
    {
        return "Ok";
    }

    // GET: /Antiforgery/FlushAsyncLogin
    [AllowAnonymous]
    public ActionResult FlushAsyncLogin(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        return View();
    }

    // POST: /Antiforgery/FlushAsyncLogin
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public string FlushAsyncLogin(LoginViewModel model)
    {
        return "OK";
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 60)]
    public ActionResult AntiforgeryTokenAndResponseCaching()
    {
        return View();
    }
}
