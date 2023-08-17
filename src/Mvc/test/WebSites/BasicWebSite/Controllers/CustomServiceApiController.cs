// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite;

[ApiController]
[Route("/services")]
public class CustomServicesApiController : Controller
{
    [HttpGet("GetOk")]
    public ActionResult<string> GetOk([FromKeyedServices("ok_service")] ICustomService service)
    {
        return service.Process();
    }

    [HttpGet("GetNotOk")]
    public ActionResult<string> GetNotOk([FromKeyedServices("not_ok_service")] ICustomService service)
    {
        return service.Process();
    }

    [HttpGet("GetBoth")]
    public ActionResult<string> GetBoth(
        [FromKeyedServices("ok_service")] ICustomService s1,
        [FromKeyedServices("not_ok_service")] ICustomService s2)
    {
        return $"{s1.Process()},{s2.Process()}";
    }
}
