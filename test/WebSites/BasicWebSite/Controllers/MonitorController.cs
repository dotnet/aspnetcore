// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace BasicWebSite
{
    public class MonitorController : Controller
    {
        private readonly ActionDescriptorCreationCounter _counterService;

        public MonitorController(INestedProvider<ActionDescriptorProviderContext> counterService)
        {
            _counterService = (ActionDescriptorCreationCounter)counterService;
        }

        public IActionResult CountActionDescriptorInvocations()
        {
            return Content(_counterService.CallCount.ToString(CultureInfo.InvariantCulture));
        }
    }
}