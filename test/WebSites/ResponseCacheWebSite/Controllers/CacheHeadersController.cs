// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ResponseCacheWebSite
{
    public class CacheHeadersController : Controller
    {
        [HttpGet("/CacheHeaders/Index")]
        [ResponseCache(Duration = 100, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept")]
        public IActionResult Index()
        {
            return Content("Hello World!");
        }

        [HttpGet("/CacheHeaders/PublicCache")]
        [ResponseCache(Duration = 100, Location = ResponseCacheLocation.Any)]
        public IActionResult PublicCache()
        {
            return Content("Hello World!");
        }

        [HttpGet("/CacheHeaders/ClientCache")]
        [ResponseCache(Duration = 100, Location = ResponseCacheLocation.Client)]
        public IActionResult ClientCache()
        {
            return Content("Hello World!");
        }

        [HttpGet("/CacheHeaders/NoStore")]
        [ResponseCache(NoStore = true, Duration = 0)]
        public IActionResult NoStore()
        {
            return Content("Hello World!");
        }

        [HttpGet("/CacheHeaders/NoCacheAtAll")]
        [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
        public IActionResult NoCacheAtAll()
        {
            return Content("Hello World!");
        }

        [HttpGet("/CacheHeaders/SetHeadersInAction")]
        [ResponseCache(Duration = 40)]
        public IActionResult SetHeadersInAction()
        {
            Response.Headers["Cache-control"] = "max-age=10";
            return Content("Hello World!");
        }

        [HttpGet("/CacheHeaders/SetsCacheControlPublicByDefault")]
        [ResponseCache(Duration = 40)]
        public IActionResult SetsCacheControlPublicByDefault()
        {
            return Content("Hello World!");
        }

        [HttpGet("/CacheHeaders/ThrowsWhenDurationIsNotSet")]
        [ResponseCache(VaryByHeader = "Accept")]
        public IActionResult ThrowsWhenDurationIsNotSet()
        {
            return Content("Hello World!");
        }
    }
}