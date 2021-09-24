// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
