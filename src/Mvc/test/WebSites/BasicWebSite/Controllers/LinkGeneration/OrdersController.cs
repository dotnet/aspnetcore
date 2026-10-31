// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.LinkGeneration;

[Route("api/orders/{id?}", Name = "OrdersApi")]
public class OrdersController : Controller
{
    [HttpGet]
    public IActionResult GetAll()
    {
        throw new NotImplementedException();
    }

    [HttpGet]
    public IActionResult GetById(int id)
    {
        throw new NotImplementedException();
    }
}
