// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers
{
    // This controller is secured through the globally added authorize filter which
    // allows only authenticated users.
    public class AdministrationController : Controller
    {
        public IActionResult Index()
        {
            return Content("Administration.Index");
        }

        [AllowAnonymous]
        public IActionResult AllowAnonymousAction()
        {
            return Content("Administration.AllowAnonymousAction");
        }
    }
}
