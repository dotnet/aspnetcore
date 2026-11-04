// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class ActionResultOfTController : Controller
{
    [HttpGet]
    public ActionResult<Product> GetProduct(int? productId)
    {
        if (productId == null)
        {
            return BadRequest();
        }

        return new Product { SampleInt = productId.Value, };
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsAsync()
    {
        await Task.Delay(0);
        return new[] { new Product(), new Product() };
    }
}
