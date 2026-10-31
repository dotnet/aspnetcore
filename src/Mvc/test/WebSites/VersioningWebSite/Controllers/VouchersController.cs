// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite;

public class VouchersController : Controller
{
    private readonly TestResponseGenerator _generator;

    public VouchersController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    // We are verifying that the right constraint gets applied along the route.
    [VersionGet("1/Vouchers", versionRange: "[1]", Name = "V1")]
    [VersionGet("2/Vouchers", versionRange: "[2]", Name = "V2")]
    public IActionResult GetVouchersMultipleVersions(string version)
    {
        return _generator.Generate(Url.RouteUrl("V" + version, new { version = version }));
    }
}
