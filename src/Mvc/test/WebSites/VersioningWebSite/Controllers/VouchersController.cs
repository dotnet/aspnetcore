// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite
{
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
}