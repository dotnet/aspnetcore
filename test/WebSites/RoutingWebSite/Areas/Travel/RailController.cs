// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite
{
    [Area("Travel")]
    [Route("ContosoCorp/Trains")]
    public class RailController
    {
        private readonly TestResponseGenerator _generator;

        public RailController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult Index()
        {
            return _generator.Generate("/ContosoCorp/Trains");
        }

        [HttpGet("CheckSchedule")]
        public IActionResult Schedule()
        {
            return _generator.Generate("/ContosoCorp/Trains/Schedule");
        }
    }
}