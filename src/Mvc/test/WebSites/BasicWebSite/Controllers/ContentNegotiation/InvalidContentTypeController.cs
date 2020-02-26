// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ContentNegotiation
{
    public class InvalidContentTypeController : Controller
    {
        [HttpGet("InvalidContentType/SetResponseContentTypeJson")]
        public IActionResult SetResponseContentTypeJson()
        {
            HttpContext.Response.ContentType = "json";
            return Ok(0);
        }
    }
}
