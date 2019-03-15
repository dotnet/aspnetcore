// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite
{
    [Route("RequestScopedService/[action]")]
    public class RequestScopedServiceController : Controller
    {
        // This only matches a specific requestId value
        [HttpGet]
        [RequestScopedConstraint("b40f6ec1-8a6b-41c1-b3fe-928f581ebaf5")]
        public string FromConstraint()
        {
            return "b40f6ec1-8a6b-41c1-b3fe-928f581ebaf5";
        }

        [HttpGet]
        [TypeFilter(typeof(RequestScopedFilter))]
        public void FromFilter()
        {
        }

        [HttpGet]
        public IActionResult FromView()
        {
            return View("View");
        }

        [HttpGet]
        public IActionResult FromTagHelper()
        {
            return View("TagHelper");
        }

        [HttpGet]
        public IActionResult FromViewComponent()
        {
            return View("ViewComponent");
        }

        [HttpGet]
        public string FromActionArgument([FromServices] RequestIdService requestIdService)
        {
            return requestIdService.RequestId;
        }
    }
}