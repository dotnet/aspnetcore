// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace FormatterWebSite.Controllers
{
    public class JsonFormatterController : Controller
    {
        private static readonly JsonSerializerSettings _indentedSettings;
        private readonly JsonOutputFormatter _indentingFormatter;

        static JsonFormatterController()
        {
            _indentedSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
            _indentedSettings.Formatting = Formatting.Indented;
        }

        public JsonFormatterController(ArrayPool<char> charPool)
        {
            _indentingFormatter = new JsonOutputFormatter(_indentedSettings, charPool);
        }

        public IActionResult ReturnsIndentedJson()
        {
            var user = new User()
            {
                Id = 1,
                Alias = "john",
                description = "Administrator",
                Designation = "Administrator",
                Name = "John Williams"
            };

            var objectResult = new ObjectResult(user);
            objectResult.Formatters.Add(_indentingFormatter);

            return objectResult;
        }

        [HttpPost]
        public IActionResult ReturnInput([FromBody]DummyClass dummyObject)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Content(dummyObject.SampleInt.ToString());
        }

        [HttpPost]
        public IActionResult ValueTypeAsBody([FromBody] int value)
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
            }

            return Content(value.ToString());
        }
    }
}