// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    public class RegularController : Controller
    {
        public async Task<EmptyResult> Index()
        {
            // This verifies that ModelState and Context are activated.
            if (ModelState.IsValid)
            {
                await HttpContext.Response.WriteAsync("Hello world");
            }

            return new EmptyResult();
        }
    }
}