// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace CustomRouteSample.Web.Controllers
{
    public class OrdersControlller : Controller
    {
        [HttpGet("Orders/{id}")]
        public string Index(int id)
        {
            return "Hello from " + RouteData.Values["locale"] + ".";
        }
    }
}
