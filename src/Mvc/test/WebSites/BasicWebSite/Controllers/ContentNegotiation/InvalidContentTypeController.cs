// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ContentNegotiation;

public class InvalidContentTypeController : Controller
{
    [HttpGet("InvalidContentType/SetResponseContentTypeJson")]
    public IActionResult SetResponseContentTypeJson()
    {
        HttpContext.Response.ContentType = "json";
        return Ok(0);
    }
}
