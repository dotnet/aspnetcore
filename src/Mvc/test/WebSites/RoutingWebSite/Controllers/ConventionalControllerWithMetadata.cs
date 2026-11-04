// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Mvc.RoutingWebSite.Infrastructure;

namespace Mvc.RoutingWebSite.Controllers;

public class ConventionalControllerWithMetadata : Controller
{
    [Metadata("C")]
    public IActionResult GetMetadata()
    {
        return Ok(HttpContext.GetEndpoint().Metadata.GetOrderedMetadata<MetadataAttribute>().Select(m => m.Value));
    }
}
