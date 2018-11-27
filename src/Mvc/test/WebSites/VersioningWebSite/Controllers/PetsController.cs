// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite
{
    // Scenario
    // The version is in the path of the URL
    public class PetsController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public PetsController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        [HttpGet("v1/Pets")]
        [HttpGet("v2/Pets")]
        public IActionResult Get()
        {
            return _generator.Generate();
        }

        [HttpGet("v1/Pets/{id}")]
        public IActionResult GetV1(int id)
        {
            return _generator.Generate();
        }

        [HttpGet("v2/Pets/{id}")]
        public IActionResult GetV2(int id)
        {
            return _generator.Generate();
        }

        [HttpPost("v1/Pets")]
        public IActionResult PostV1()
        {
            return _generator.Generate();
        }

        [HttpPost("v{version:Min(2)}/Pets")]
        public IActionResult PostV2()
        {
            return _generator.Generate();
        }
    }
}