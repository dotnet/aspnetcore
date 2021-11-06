// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite;

public class RoutingController : Controller
{
    public bool HasEndpointMatch()
    {
        var endpointFeature = HttpContext.Features.Get<IEndpointFeature>();
        return endpointFeature?.Endpoint != null;
    }
}
