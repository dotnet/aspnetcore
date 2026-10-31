// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

[RequestSizeLimit(500)]
public class RequestSizeLimitController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult<Product> RequestSizeLimitCheckBeforeAntiforgeryValidation(Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return product;
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    public ActionResult<Product> DisableRequestSizeLimit([FromBody] Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        return product;
    }
}
