// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Components;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class PassThroughController : Controller
    {
        public IActionResult Index(long value)
        {
            return ViewComponent(typeof(PassThroughViewComponent), new { value });
        }
    }
}
