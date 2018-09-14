// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite
{
    [Route("[controller]/[action]", Name = "[controller]_[action]")]
    public class ParameterTransformerController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public ParameterTransformerController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult MyAction()
        {
            return _generator.Generate("/parameter-transformer/my-action");
        }
    }
}
