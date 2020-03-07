// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite
{
    public class RoutingController : Controller
    {
        public bool HasEndpointMatch()
        {
            var endpointFeature = HttpContext.Features.Get<IEndpointFeature>();
            return endpointFeature?.Endpoint != null;
        }
    }
}