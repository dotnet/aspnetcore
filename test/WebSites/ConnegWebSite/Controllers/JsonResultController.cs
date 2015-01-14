// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Net.Http.Headers;

namespace ConnegWebSite
{
    public class JsonResultController : Controller
    {
        public IActionResult ReturnJsonResult()
        {
            return new JsonResult(new { MethodName = "ReturnJsonResult" });
        }

        public IActionResult ReturnJsonResult_WithCustomMediaType()
        {
            var jsonResult = new JsonResult(new { MethodName = "ReturnJsonResult_WithCustomMediaType" },
                                            new CustomFormatter("application/custom-json"));
            jsonResult.ContentTypes.Add(MediaTypeHeaderValue.Parse("application/custom-json"));
            return jsonResult;
        }

        public IActionResult ReturnJsonResult_WithCustomMediaType_NoFormatter()
        {
            var jsonResult = new JsonResult(new { MethodName = "ReturnJsonResult_WithCustomMediaType_NoFormatter" });
            jsonResult.ContentTypes.Add(MediaTypeHeaderValue.Parse("application/custom-json"));
            return jsonResult;
        }

        [Produces("application/xml")]
        public IActionResult Produces_WithNonObjectResult()
        {
            return new JsonResult(new { MethodName = "Produces_WithNonObjectResult" });
        }
    }
}