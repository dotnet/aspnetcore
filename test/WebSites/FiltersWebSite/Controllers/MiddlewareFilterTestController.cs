// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace FiltersWebSite
{
    public class MiddlewareFilterTestController : Controller
    {
        [Route("{culture}/[controller]/[action]")]
        [MiddlewareFilter(typeof(LocalizationPipeline))]
        public IActionResult CultureFromRouteData()
        {
            return Content($"CurrentCulture:{CultureInfo.CurrentCulture.Name},CurrentUICulture:{CultureInfo.CurrentUICulture.Name}");
        }
    }
}
