// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite
{
    public class MoviesV2Controller : Controller
    {
        private readonly TestResponseGenerator _generator;

        public MoviesV2Controller(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        [VersionPut("Movies/{id}", versionRange: "2")]
        public IActionResult Put(int id)
        {
            return _generator.Generate();
        }
    }
}