// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BasicWebSite.Controllers
{
    public class JsonResultController : Controller
    {
        private static JsonSerializerSettings _customSerializerSettings;

        public JsonResult Plain()
        {
            return Json(new { Message = "hello" });
        }

        public JsonResult CustomContentType()
        {
            var result = new JsonResult(new { Message = "hello" });
            result.ContentType = MediaTypeHeaderValue.Parse("application/message+json");
            return result;
        }

        public JsonResult CustomSerializerSettings()
        {
            if (_customSerializerSettings == null)
            {
                _customSerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
            }

            return Json(new { Message = "hello" }, _customSerializerSettings);
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