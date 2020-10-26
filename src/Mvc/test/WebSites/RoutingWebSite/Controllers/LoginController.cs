// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite
{
    // This controller is reachable via traditional routing.
    public class LoginController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public LoginController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult Index()
        {
            return _generator.Generate(Url.RouteUrl("ActionAsMethod", null, Url.ActionContext.HttpContext.Request.Scheme));
        }

        public IActionResult Sso()
        {
            return _generator.Generate(Url.RouteUrl("ActionAsMethod", null, Url.ActionContext.HttpContext.Request.Scheme));
        }
    }
}