// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ActionConstraints;

[Route("ConsumesAttribute_WithFallbackActionController/[action]")]
public class ConsumesAttribute_WithFallbackActionController : Controller
{
    [Consumes("application/json")]
    [ActionName("CreateProduct")]
    public IActionResult CreateProductJson()
    {
        return Content("CreateProduct_Product_Json");
    }

    [Consumes("application/xml")]
    [ActionName("CreateProduct")]
    public IActionResult CreateProductXml()
    {
        return Content("CreateProduct_Product_Xml");
    }

    public IActionResult CreateProduct()
    {
        return Content("CreateProduct_Product_Text");
    }
}
