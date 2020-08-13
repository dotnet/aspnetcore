// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.NodeServices;

namespace NodeServicesExamples.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(int pageIndex)
        {
            return View();
        }

        public IActionResult ES2015Transpilation()
        {
            return View();
        }

#pragma warning disable 0618
        public async Task<IActionResult> Chart([FromServices] INodeServices nodeServices)
#pragma warning restore 0618
        {
            var options = new { width = 400, height = 200, showArea = true, showPoint = true, fullWidth = true };
            var data = new
            {
                labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" },
                series = new[] {
                    new[] { 1, 5, 2, 5, 4, 3 },
                    new[] { 2, 3, 4, 8, 1, 2 },
                    new[] { 5, 4, 3, 2, 1, 0 }
                }
            };

            ViewData["ChartMarkup"] = await nodeServices.InvokeAsync<string>("./Node/renderChart", "line", options, data);

            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
