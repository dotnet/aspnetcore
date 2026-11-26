// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Tests;

public partial class OperationTests
{
    [Fact]
    public async Task SupportsXmlCommentsOnOperationsFromControllers()
    {
        var source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(TestController).Assembly)
    .AddApplicationPart(typeof(Test2Controller).Assembly);
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapControllers();

app.Run();

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// A summary of the action.
    /// </summary>
    /// <description>
    /// A description of the action.
    /// </description>
    [HttpGet]
    public string Get()
    {
        return "Hello, World!";
    }
}

[ApiController]
[Route("[controller]")]
public class Test2Controller : ControllerBase
{
    /// <param name="name">The name of the person.</param>
    /// <response code="200">Returns the greeting.</response>
    [HttpGet]
    public string Get(string name, [FromQuery] DerivedQuery query)
    {
        return $"Hello, {name}!";
    }

    /// <param name="id">The id associated with the request.</param>
    [HttpGet("HelloByInt")]
    public string Get(int id)
    {
        return $"Hello, {id}!";
    }

    /// <param name="todo">The todo to insert into the database.</param>
    [HttpPost]
    public string Post(Todo todo)
    {
        return $"Hello, {todo.Title}!";
    }
}

public partial class Program {}

public record Todo(int Id, string Title, bool Completed);

public class BaseQuery
{
    /// <summary>The ID to filter by.</summary>
    public int? Id { get; set; }
}

public class DerivedQuery : BaseQuery
{
    /// <summary>The limit to apply.</summary>
    public int? Limit { get; set; }
}
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var path = document.Paths["/Test"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of the action.", path.Summary);
            Assert.Equal("A description of the action.", path.Description);

            var path2 = document.Paths["/Test2"].Operations[HttpMethod.Get];
            Assert.Equal("The name of the person.", Assert.Single(path2.Parameters, parameter => parameter.Name == "name").Description);
            Assert.Equal("The ID to filter by.", Assert.Single(path2.Parameters, parameter => parameter.Name == "Id").Description);
            Assert.Equal("The limit to apply.", Assert.Single(path2.Parameters, parameter => parameter.Name == "Limit").Description);
            Assert.Equal("Returns the greeting.", path2.Responses["200"].Description);

            var path2again = document.Paths["/Test2/HelloByInt"].Operations[HttpMethod.Get];
            Assert.Equal("The id associated with the request.", path2again.Parameters[0].Description);

            var path3 = document.Paths["/Test2"].Operations[HttpMethod.Post];
            Assert.Equal("The todo to insert into the database.", path3.RequestBody.Description);
        });
    }

    [Fact]
    public async Task SupportsRouteParametersFromControllers()
    {
        var source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(TestController).Assembly);
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapControllers();

app.Run();

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    /// <param name="userId">The id of the user.</param>
    [HttpGet("{userId}")]
    public string Get()
    {
        return "Hello, World!";
    }
}

public partial class Program {}

""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var path = document.Paths["/Test/{userId}"].Operations[HttpMethod.Get];
            Assert.Equal("The id of the user.", path.Parameters[0].Description);
        });
    }
}
