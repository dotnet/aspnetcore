// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Produces("application/json")]
public class SystemTextJsonOutputFormatterController : ControllerBase
{
    [HttpGet]
    public ActionResult<SimpleModel> PolymorphicResult() => new DerivedModel
    {
        Id = 10,
        Name = "test",
        Address = "Some address",
    };

    [HttpGet]
    public async IAsyncEnumerable<int> AsyncEnumerable()
    {
        await Task.Yield();
        HttpContext.Response.Headers["Test"] = "t";
        yield return 1;
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(DerivedModel), nameof(DerivedModel))]
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
