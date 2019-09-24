// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ActionConstraints
{
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
}