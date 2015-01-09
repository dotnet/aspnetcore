// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;

namespace ActionConstraintsWebSite
{
    [Route("ConsumesAttribute_PassThrough/[action]")]
    public class ConsumesAttribute_PassThroughController : Controller
    {
        [Consumes("application/json")]
        public Product CreateProduct([FromBody] Product_Json jsonInput)
        {
            return jsonInput;
        }
    }
}