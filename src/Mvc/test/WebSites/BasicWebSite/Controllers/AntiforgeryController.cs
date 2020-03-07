// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Filters;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
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
        [TypeFilter(typeof(RedirectAntiforgeryValidationFailedResultFilter))]
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
}