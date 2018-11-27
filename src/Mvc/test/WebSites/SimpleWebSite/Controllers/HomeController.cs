// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SimpleWebSite.Controllers
{
    public class HomeController
    {
        public IDictionary<string, string> Index()
        {
            return new Dictionary<string, string> {
                {"first", "wall" },
                {"second", "floor" }
            };
        }
    }
}
