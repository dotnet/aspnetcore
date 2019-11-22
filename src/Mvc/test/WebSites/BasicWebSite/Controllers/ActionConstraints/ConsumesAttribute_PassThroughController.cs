// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ActionConstraints
{
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
}