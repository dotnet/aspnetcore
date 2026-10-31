// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ActionConstraints;

[Route("ConsumesAttribute_PassThrough/[action]")]
public class ConsumesAttribute_PassThroughController : Controller
{
    [Consumes("application/json")]
    public IActionResult CreateProduct(Product_Json jsonInput)
    {
        return Content("ConsumesAttribute_PassThrough_Product_Json");
    }

    [Consumes("application/json")]
    public IActionResult CreateProductMultiple(Product_Json jsonInput)
    {
        return Content("ConsumesAttribute_PassThrough_Product_Json");
    }

    [Consumes("application/xml")]
    public IActionResult CreateProductMultiple(Product_Xml jsonInput)
    {
        return Content("ConsumesAttribute_PassThrough_Product_Xml");
    }
}
