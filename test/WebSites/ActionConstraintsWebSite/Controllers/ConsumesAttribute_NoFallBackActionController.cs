// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActionConstraintsWebSite
{
    [Route("ConsumesAttribute_AmbiguousActions/[action]")]
    public class ConsumesAttribute_NoFallBackActionController : Controller
    {
        [Consumes("application/json", "text/json")]
        public Product CreateProduct([FromBody] Product_Json jsonInput)
        {
            return jsonInput;
        }

        [Consumes("application/xml")]
        public Product CreateProduct([FromBody] Product_Xml xmlInput)
        {
            return xmlInput;
        }
    }
}