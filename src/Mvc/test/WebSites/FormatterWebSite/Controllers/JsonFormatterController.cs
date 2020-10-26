// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;

namespace FormatterWebSite.Controllers
{
    public class JsonFormatterController : Controller
    {
        private static readonly JsonSerializerSettings _indentedSettings;
        private readonly NewtonsoftJsonOutputFormatter _indentingFormatter;

        static JsonFormatterController()
        {
            _indentedSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
            _indentedSettings.Formatting = Formatting.Indented;
        }

        public JsonFormatterController(ArrayPool<char> charPool)
        {
            _indentingFormatter = new NewtonsoftJsonOutputFormatter(_indentedSettings, charPool, new MvcOptions());
        }

        public IActionResult ReturnsIndentedJson()
        {
            var user = new User()
            {
                Id = 1,
                Alias = "john",
                description = "This is long so we can test large objects " + new string('a', 1024 * 65),
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

        [HttpPost]
        public ActionResult<SimpleModel> RoundtripSimpleModel([FromBody] SimpleModel model)
        {
            return model;
        }

        public class SimpleModel
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string StreetName { get; set; }
        }
    }
}