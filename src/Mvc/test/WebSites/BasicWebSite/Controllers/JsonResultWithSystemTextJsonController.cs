// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class JsonResultWithSystemTextJsonController : Controller
    {
        private static readonly JsonSerializerOptions _customSerializerSettings;

        static JsonResultWithSystemTextJsonController()
        {
            _customSerializerSettings = new JsonSerializerOptions();
        }

        public JsonResult Plain()
        {
            return new JsonResult(new { Message = "hello" });
        }

        public JsonResult CustomContentType()
        {
            var result = new JsonResult(new { Message = "hello" });
            result.ContentType = "application/message+json";
            return result;
        }

        public JsonResult CustomSerializerSettings()
        {
            return new JsonResult(new { Message = "hello" }, _customSerializerSettings);
        }

        public JsonResult Null()
        {
            return new JsonResult(null);
        }

        public JsonResult String()
        {
            return new JsonResult("hello");
        }
    }
}
