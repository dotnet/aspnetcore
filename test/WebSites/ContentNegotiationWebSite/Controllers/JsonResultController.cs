// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ActionResults;

namespace ContentNegotiationWebSite
{
    public class JsonResultController : Controller
    {
        [Produces("application/xml")]
        public IActionResult Produces_WithNonObjectResult()
        {
            return new JsonResult(new { MethodName = "Produces_WithNonObjectResult" });
        }
    }
}