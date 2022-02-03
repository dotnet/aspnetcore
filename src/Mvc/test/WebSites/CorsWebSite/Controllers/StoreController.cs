// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CorsWebSite.Controllers;

[AllRequestsBlockingAuthorizationFilter]
[EnableCors("AllowAll")]
[Route("api/store/[action]")]
public class StoreController : Controller
{
    [HttpGet]
    public IEnumerable<string> ActionUsingControllerCorsSettings()
    {
        return new string[] { "product1", "product2" };
    }

    [HttpGet]
    [EnableCors("Allow example.com")]
    public string ActionWithCorsSettings()
    {
        return "product1";
    }

    // Irrespective of where(controller or action) the Cors filter is applied, Cors filters should be
    // executed before any other type of authorization filters.
    [HttpGet]
    [DisableCors]
    public string ActionWithCorsDisabled()
    {
        return "product1";
    }

    // Irrespective of where(controller or action) the Cors filter is applied, Cors filters should be
    // executed before any other type of authorization filters.
    [HttpGet]
    [EnableCors("Allow example.com")]
    public string ActionWithDifferentCorsPolicy()
    {
        return "product1";
    }
}
