// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Produces("application/json")]
public class JsonOutputFormatterController : ControllerBase
{
    [HttpGet]
    public ActionResult<int> IntResult() => 2;

    [HttpGet]
    public ActionResult<string> StringResult() => "Hello world";

    [HttpGet]
    public ActionResult<string> StringWithUnicodeResult() => "Hello Mr. 🦊";

    [HttpGet]
    public ActionResult<string> StringWithNonAsciiContent() => "Une bête de cirque";

    [HttpGet]
    public ActionResult<SimpleModel> SimpleModelResult() =>
        new SimpleModel { Id = 10, Name = "Test", StreetName = "Some street" };

    [HttpGet]
    public ActionResult<IEnumerable<SimpleModel>> CollectionModelResult() =>
        new[]
        {
                new SimpleModel { Id = 10, Name = "TestName" },
                new SimpleModel { Id = 11, Name = "TestName1", StreetName = "Some street" },
        };

    [HttpGet]
    public ActionResult<Dictionary<string, string>> DictionaryResult() =>
        new Dictionary<string, string>
        {
            ["SomeKey"] = "Value0",
            ["DifferentKey"] = "Value1",
            ["Key3"] = null,
        };

    [HttpGet]
    public ActionResult<SimpleModel> LargeObjectResult() =>
        new SimpleModel
        {
            Id = 10,
            Name = "This is long so we can test large objects " + new string('a', 1024 * 65),
        };

    [HttpGet]
    public ActionResult<SimpleModel> PolymorphicResult() => new DerivedModel
    {
        Id = 10,
        Name = "test",
        Address = "Some address",
    };

    [HttpGet]
    public ActionResult<ProblemDetails> ProblemDetailsResult() => NotFound();

    public class SimpleModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string StreetName { get; set; }
    }

    public class DerivedModel : SimpleModel
    {
        public string Address { get; set; }
    }
}
