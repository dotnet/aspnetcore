// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Formatters;
using Newtonsoft.Json;

namespace FormatterWebSite.Controllers
{
    public class JsonFormatterController : Controller
    {
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

            var jsonFormatter = new JsonOutputFormatter();
            jsonFormatter.SerializerSettings.Formatting = Formatting.Indented;

            var objectResult = new ObjectResult(user);
            objectResult.Formatters.Add(jsonFormatter);

            return objectResult;
        }

        [HttpPost]
        public IActionResult ReturnInput([FromBody]DummyClass dummyObject)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(StatusCodes.Status400BadRequest);
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