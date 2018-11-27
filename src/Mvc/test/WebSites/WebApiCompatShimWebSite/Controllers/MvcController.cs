// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace WebApiCompatShimWebSite
{
    // This is reachable via our MVC routes, but not webapi routes
    public class MvcController : Controller
    {
        public string Index()
        {
            return "Hello, World!";
        }
    }
}