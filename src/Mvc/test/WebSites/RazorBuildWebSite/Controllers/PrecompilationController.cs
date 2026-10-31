// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorBuildWebSite.Controllers;

public class PrecompilationController : Controller
{
    public new ActionResult View()
    {
        return base.View();
    }
}
