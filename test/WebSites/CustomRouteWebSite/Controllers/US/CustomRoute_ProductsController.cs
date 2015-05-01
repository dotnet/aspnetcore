// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace CustomRouteWebSite.Controllers.US
{
    [Locale("en-US")]
    public class CustomRoute_ProductsController : Controller
    {
        public string Index()
        {
            return "Hello from the USA.";
        }
    }
}