// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite;

// Scenario
// Same template disjoint version sets.
// Same template overlapping version sets disambiguated by order.
public class AddressController : Controller
{
    private readonly TestResponseGenerator _generator;

    public AddressController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [VersionRoute("api/Addresses", versionRange: "[1]")]
    public IActionResult GetV1()
    {
        return _generator.Generate("api/addresses");
    }

    [VersionRoute("api/Addresses", versionRange: "[2]")]
    public IActionResult GetV2()
    {
        return _generator.Generate("api/addresses");
    }

    [VersionRoute("api/addresses/all", versionRange: "[1]")]
    public IActionResult GetAllV1(string version)
    {
        return _generator.Generate(
            Url.Action("GetAllV1",
            new { version = version }), Url.RouteUrl(new { version = version }));
    }

    [VersionRoute("api/addresses/all", versionRange: "[1-2]", Order = 1)]
    public IActionResult GetAllV2(string version)
    {
        return _generator.Generate(
            Url.Action("GetAllV2", new { version = version }),
            Url.RouteUrl(new { version = version }));
    }
}
