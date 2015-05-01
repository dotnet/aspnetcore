// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ControllerDiscoveryConventionsWebSite
{
    [NonController]
    public class NonControllerController : Controller
    {
        public string GetValue()
        {
            return nameof(NonControllerController);
        }
    }
}