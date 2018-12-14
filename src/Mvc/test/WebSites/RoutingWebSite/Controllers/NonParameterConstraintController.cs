// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite.Controllers
{
    public class NonParameterConstraintController : Controller
    {
        public ActionResult Index()
        {
            return Ok();
        }
    }
}
