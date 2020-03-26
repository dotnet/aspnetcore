// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesClassLibrary
{
    public class Inventory : ResourcesController
    {
        [HttpGet]
        public IActionResult Get()
        {
            return new ContentResult { Content = "4" };
        }
    }
}