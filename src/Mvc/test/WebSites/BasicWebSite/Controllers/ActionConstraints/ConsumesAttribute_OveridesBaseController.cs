// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ActionConstraints;

[Consumes("application/json")]
public class ConsumesAttribute_OverridesBaseController : Controller
{
    [Consumes("text/json")]
    public Product CreateProduct([FromBody] Product_Json product)
    {
        // should be picked if request content type is text/json and not application/json.
        product.SampleString = "ConsumesAttribute_OverridesBaseController_text/json";
        return product;
    }

    public virtual IActionResult CreateProduct([FromBody] Product product)
    {
        // should be picked if request content type is application/json.
        product.SampleString = "ConsumesAttribute_OverridesBaseController_application/json";
        return new ObjectResult(product);
    }
}
