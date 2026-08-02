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

    [HttpGet("GetKeyNull")]
    public ActionResult<string> GetKeyNull([FromKeyedServices(null)] ICustomService service)
    {
        return service.Process();
    }

# nullable enable

    [HttpGet("GetOptionalNotRegistered")]
    public ActionResult<string> GetOptionalNotRegistered([FromKeyedServices("no_existing_key")] ICustomService? service)
    {
        if (service != null)
        {
            throw new Exception("Service should not have been resolved");
        }
        return string.Empty;
    }

    [HttpGet("GetRequiredNotRegistered")]
    public ActionResult<string> GetRequiredNotRegistered([FromKeyedServices("no_existing_key")] ICustomService service)
    {
        return service.Process();
    }
}
