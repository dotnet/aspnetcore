using System;
using Microsoft.AspNet.Mvc;
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
    }
}