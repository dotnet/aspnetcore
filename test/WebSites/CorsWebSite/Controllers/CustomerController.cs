// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CorsWebSite
{
    [Route("NonCors/[action]")]
    public class CustomerController : Controller
    {
        [HttpOptions]
        public IEnumerable<string> GetOptions()
        {
            return new[] { "Create", "Update", "Delete" };
        }

        [HttpPost]
        public IEnumerable<string> Post()
        {
            return new[] { "customer1", "customer2", "customer3" };
        }
    }
}