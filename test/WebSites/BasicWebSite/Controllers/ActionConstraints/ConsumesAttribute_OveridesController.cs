// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ActionConstraints
{
    [Consumes("application/xml")]
    public class ConsumesAttribute_OverridesController : ConsumesAttribute_OverridesBaseController
    {
        public override IActionResult CreateProduct([FromBody] Product product)
        {
            // should be picked if request content type is text/json.
            product.SampleString = "ConsumesAttribute_OverridesController_application/xml";
            return new JsonResult(product);
        }
    }
}