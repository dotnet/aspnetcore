// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

[RequestFormLimits(ValueCountLimit = 2)]
public class RequestFormLimitsController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult<Product> RequestFormLimitsBeforeAntiforgeryValidation(Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return product;
    }

    [HttpPost]
    [RequestFormLimits(ValueCountLimit = 5)]
    public ActionResult<Product> OverrideControllerLevelLimits(Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        return product;
    }

    [HttpPost]
    [RequestFormLimits]
    public ActionResult<Product> OverrideControllerLevelLimitsUsingDefaultLimits(Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        return product;
    }

    [HttpPost]
    [RequestFormLimits(ValueCountLimit = 2)]
    [RequestSizeLimit(100)]
    [ValidateAntiForgeryToken]
    public ActionResult<Product> RequestSizeLimitBeforeRequestFormLimits(Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        return product;
    }
}
