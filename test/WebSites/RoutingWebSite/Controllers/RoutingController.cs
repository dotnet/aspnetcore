// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RoutingWebSite
{
    public class RoutingController : Controller
    {
        public ActionResult HasEndpointMatch()
        {
            var endpointFeature = HttpContext.Features.Get<IEndpointFeature>();
            return Json(endpointFeature?.Endpoint != null);
        }
    }
}