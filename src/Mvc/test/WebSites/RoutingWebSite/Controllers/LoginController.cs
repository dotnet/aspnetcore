// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

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
