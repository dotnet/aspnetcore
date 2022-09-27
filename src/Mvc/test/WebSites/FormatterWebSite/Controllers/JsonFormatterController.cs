// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;

namespace FormatterWebSite.Controllers;

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
        _indentingFormatter = new NewtonsoftJsonOutputFormatter(_indentedSettings, charPool, new MvcOptions(), new MvcNewtonsoftJsonOptions());
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

#nullable enable
    [HttpPost]
    public IActionResult ReturnNonNullableInput([FromBody] DummyClass dummyObject)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Content(dummyObject.SampleInt.ToString(CultureInfo.InvariantCulture));
    }
#nullable restore

    [HttpPost]
    public IActionResult ReturnInput([FromBody] DummyClass dummyObject)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Content(dummyObject?.SampleInt.ToString(CultureInfo.InvariantCulture));
    }

    [HttpPost]
    public IActionResult ValueTypeAsBody([FromBody] int value)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        return Content(value.ToString(CultureInfo.InvariantCulture));
    }

    [HttpPost]
    public ActionResult<SimpleModel> RoundtripSimpleModel([FromBody] SimpleModel model)
    {
        return model;
    }

    [HttpPost]
    public ActionResult<SimpleRecordModel> RoundtripRecordType([FromBody] SimpleRecordModel model) => model;

    public class SimpleModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string StreetName { get; set; }
    }

    public record SimpleRecordModel(int Id, string Name, string StreetName);

    public record SimpleModelWithValidation(
        [Range(1, 100)]
            int Id,

        [Required]
            [StringLength(8, MinimumLength = 2)]
            string Name,

        [Required]
            string StreetName);

    [HttpPost]
    public ActionResult<SimpleModelWithValidation> RoundtripModelWithValidation([FromBody] SimpleModelWithValidation model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }
        return model;
    }
}
