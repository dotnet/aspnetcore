// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    public class RegularController : Controller
    {
        public void Index()
        {
            // This verifies that ModelState and Context are activated.
            if (ModelState.IsValid)
            {
                Context.Response.WriteAsync("Hello world").Wait();
            }
        }
    }
}