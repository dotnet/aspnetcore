// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite
{
    public class NonParameterConstraintController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public NonParameterConstraintController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult Index()
        {
            return _generator.Generate("/NonParameterConstraintRoute/NonParameterConstraint/Index");
        }
    }
}
