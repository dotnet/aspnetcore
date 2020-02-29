// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorBuildWebSite
{
    public class UpdateableViewsController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult Update([FromServices] UpdateableFileProvider fileProvider, string path, string content)
        {
            fileProvider.UpdateContent(path, content);
            return Ok();
        }

        [HttpPost]
        public IActionResult UpdateRazorPages([FromServices] UpdateableFileProvider fileProvider)
        {
            fileProvider.CancelRazorPages();
            return Ok();
        }
    }
}
