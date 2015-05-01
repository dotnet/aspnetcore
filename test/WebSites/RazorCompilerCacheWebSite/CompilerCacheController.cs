// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RazorCompilerCacheWebSite
{
    public class CompilerCacheController : Controller
    {
        [HttpGet("/cache-status")]
        [Produces("text/plain")]
        public ContentResult GetCompilerCacheInitializationStatus(
            [FromServices] CompilerCacheInitialiedService service)
        {
            return Content(service.Initialized.ToString());
        }

        [HttpGet("/statuscode")]
        public IActionResult StatusCodeAction()
        {
            return new EmptyResult();
        }

        [HttpGet("/file")]
        public IActionResult FileAction()
        {
            return File("HelloWorld.htm", "application/text");
        }

        [HttpGet("/view")]
        public ViewResult Index()
        {
            return View("~/Index");
        }
    }
}
