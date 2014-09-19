using Microsoft.AspNet.Mvc;
using System;

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