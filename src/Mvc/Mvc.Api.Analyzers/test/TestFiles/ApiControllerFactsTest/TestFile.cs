// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class ApiConventionAnalyzerTest_IndexModel : PageModel
    {
        public IActionResult OnGet() => null;
    }

    public class ApiConventionAnalyzerTest_NotApiController : Controller
    {
        public IActionResult Index() => null;
    }

    public class ApiConventionAnalyzerTest_NotAction : Controller
    {
        [NonAction]
        public IActionResult Index() => null;
    }

    [ApiController]
    public class ApiConventionAnalyzerTest_Valid : Controller
    {
        public IActionResult Index() => null;
    }
}
