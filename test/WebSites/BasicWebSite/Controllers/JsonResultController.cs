// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Serialization;

namespace BasicWebSite.Controllers
{
    public class JsonResultController : Controller
    {
        public JsonResult Plain()
        {
            return Json(new { Message = "hello" });
        }

        public JsonResult CustomFormatter()
        {
            var formatter = new JsonOutputFormatter();
            formatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            return new JsonResult(new { Message = "hello" }, formatter);
        }

        public JsonResult CustomContentType()
        {
            var formatter = new JsonOutputFormatter();
            formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/message+json"));

            var result = new JsonResult(new { Message = "hello" }, formatter);
            result.ContentTypes.Add(MediaTypeHeaderValue.Parse("application/message+json"));
            return result;
        }

        public JsonResult Null()
        {
            return Json(null);
        }

        public JsonResult String()
        {
            return Json("hello");
        }
    }
}