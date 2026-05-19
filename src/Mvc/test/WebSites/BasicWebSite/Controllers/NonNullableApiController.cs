// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

[ApiController]
[Route("api/NonNullable")]
public class NonNullableApiController : ControllerBase
{
    // GET: api/<controller>
    [HttpGet]
    public ActionResult<string> Get(string language = "pt-br")
    {
        return language;
    }
}
