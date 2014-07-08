// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PlainView()
        {
            return View();
        }

        public IActionResult NoContentResult()
        {
            return new HttpStatusCodeResult(204);
        }

        public async Task ActionReturningTask()
        {
            await Context.Response.WriteAsync("Hello world");
        }
    }
}