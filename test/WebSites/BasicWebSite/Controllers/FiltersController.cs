// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;

namespace BasicWebSite.Controllers
{
    public class FiltersController : Controller
    {
        [HttpPost]
        [Consumes("application/yaml")]
        [UnprocessableResultFilter]
        public IActionResult AlwaysRunResultFiltersCanRunWhenResourceFilterShortCircuit([FromBody] Product product) =>
            throw new Exception("Shouldn't be executed");
    }
}
