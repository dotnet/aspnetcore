// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActionConstraintsWebSite
{
    [Consumes("application/json")]
    public class ConsumesAttribute_OverridesBaseController : Controller
    {
        [Consumes("text/json")]
        public Product CreateProduct([FromBody] Product_Json product)
        {
            // should be picked if request content type is application/xml and not application/json.
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
}