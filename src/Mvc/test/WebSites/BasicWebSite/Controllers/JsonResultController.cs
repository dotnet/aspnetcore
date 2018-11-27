// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BasicWebSite.Controllers
{
    public class JsonResultController : Controller
    {
        private static readonly JsonSerializerSettings _customSerializerSettings;

        static JsonResultController()
        {
            _customSerializerSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
            _customSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        }

        public JsonResult Plain()
        {
            return Json(new { Message = "hello" });
        }

        public JsonResult CustomContentType()
        {
            var result = new JsonResult(new { Message = "hello" });
            result.ContentType = "application/message+json";
            return result;
        }

        public JsonResult CustomSerializerSettings()
        {
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