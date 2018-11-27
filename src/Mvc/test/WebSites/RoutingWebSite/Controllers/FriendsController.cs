// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite
{
    [Route("Friends")]
    public class FriendsController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public FriendsController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        [HttpGet]
        [HttpGet("{id}")]
        public IActionResult Get([FromRoute]string id)
        {
            return _generator.Generate(id == null ? "/Friends" : $"/Friends/{id}");
        }

        [HttpDelete]
        public IActionResult Delete()
        {
            return _generator.Generate("/Friends");
        }
    }
}
