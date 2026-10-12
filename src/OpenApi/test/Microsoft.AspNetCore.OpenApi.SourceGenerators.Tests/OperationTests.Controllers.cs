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
    public string Get(string name)
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
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var path = document.Paths["/Test"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of the action.", path.Summary);
            Assert.Equal("A description of the action.", path.Description);

            var path2 = document.Paths["/Test2"].Operations[HttpMethod.Get];
            Assert.Equal("The name of the person.", path2.Parameters[0].Description);
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

    [Fact]
    public async Task SupportsParametersWithCustomNamesFromControllers()
    {
        var source =
"""
using System.Collections.Generic;
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
    [HttpGet("{user_id}")]
    public string Get([FromRoute(Name = "user_id")] int userId)
    {
        return "Hello, World!";
    }

    [HttpGet]
    public IEnumerable<Person> Search(Query query)
    {
        return [];
    }
}

public partial class Program {}

public record Person(int Id, string Name);

public class Query
{
    /// <summary>
    /// The full name of the person.
    /// </summary>
    [FromQuery(Name = "full_name")]
    public string? Name { get; init; }
}
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var getOperation = document.Paths["/Test/{user_id}"].Operations[HttpMethod.Get];
            Assert.Equal("user_id", getOperation.Parameters[0].Name);
            Assert.Equal("The id of the user.", getOperation.Parameters[0].Description);

            var searchOperation = document.Paths["/Test"].Operations[HttpMethod.Get];
            Assert.Equal("full_name", searchOperation.Parameters[0].Name);
            Assert.Equal("The full name of the person.", searchOperation.Parameters[0].Description);
        });
    }

    [Fact]
    public async Task DoesNotMisattributeDocumentationWhenMemberNameCollidesWithAnotherBindingName()
    {
        var source =
"""
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
    /// <param name="sortColumn">The column to sort by.</param>
    /// <param name="order">The sort direction.</param>
    [HttpGet]
    public string Get(
        [FromQuery(Name = "order")] string sortColumn,
        [FromQuery(Name = "sortBy")] string order)
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
            var operation = document.Paths["/Test"].Operations[HttpMethod.Get];

            var orderParameter = operation.Parameters.Single(parameter => parameter.Name == "order");
            var sortByParameter = operation.Parameters.Single(parameter => parameter.Name == "sortBy");

            // The OpenAPI parameter named "order" is bound from the C# `sortColumn` parameter.
            Assert.Equal("The column to sort by.", orderParameter.Description);
            // The OpenAPI parameter named "sortBy" is bound from the C# `order` parameter.
            Assert.Equal("The sort direction.", sortByParameter.Description);
        });
    }

    [Fact]
    public async Task ShouldNotApplyCancellationTokenDocumentationToRequestBody()
    {
        var source =
"""
using System.Collections.Generic;
using System.Threading;
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
    /// <param name="cancellationToken">The cancellation token.</param>
    [HttpGet]
    public ActionResult Create(Person person, CancellationToken cancellationToken)
    {
        return Created();
    }
}

public partial class Program {}

public record Person(int Id, string Name);
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var getOperation = document.Paths["/Test"].Operations[HttpMethod.Get];
            Assert.Null(getOperation.RequestBody.Description);
        });
    }

    [Fact]
    public async Task ShouldNotApplyFromServicesDocumentationToRequestBody()
    {
        var source =
"""
using System.Collections.Generic;
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
    /// <param name="service">The service used to create the resource.</param>
    [HttpGet]
    public ActionResult Create(Person person, [FromServices] ITestService service)
    {
        return Created();
    }
}

public interface ITestService {}

public partial class Program {}

public record Person(int Id, string Name);
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var getOperation = document.Paths["/Test"].Operations[HttpMethod.Get];
            // The `service` parameter is bound from DI, not the request body, so its
            // documentation must not be applied to the request body description.
            Assert.Null(getOperation.RequestBody.Description);
        });
    }

    [Fact]
    public async Task ShouldApplyFormParameterDocumentationToRequestBody()
    {
        var source =
"""
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
    /// <param name="name">The name of the resource to create.</param>
    [HttpPost]
    public ActionResult Create([FromForm] string name)
    {
        return Created();
    }
}

public partial class Program {}
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var postOperation = document.Paths["/Test"].Operations[HttpMethod.Post];
            // The `name` parameter is bound from the form, which maps to the request
            // body, so its documentation should be applied to the request body description.
            Assert.Equal("The name of the resource to create.", postOperation.RequestBody.Description);
        });
    }

    [Fact]
    public async Task SupportsXmlCommentsOnInheritedPropertiesFromControllers()
    {
        var source =
"""
using System.Collections.Generic;
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
    [HttpGet]
    public IEnumerable<Person> Search([FromQuery] DerivedQuery query)
    {
        return [];
    }
}

public partial class Program {}

public record Person(int Id, string Name);

public class BaseQuery
{
    /// <summary>
    /// The full name of the person.
    /// </summary>
    public string? Name { get; init; }
}

public class DerivedQuery : BaseQuery
{
    /// <summary>
    /// The maximum number of results to return.
    /// </summary>
    public int Limit { get; init; }
}
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var searchOperation = document.Paths["/Test"].Operations[HttpMethod.Get];
            // `Name` is declared on `BaseQuery` but bound through the derived model, so its
            // documentation must be resolved by walking up to the inherited property. This
            // would regress if the property lookup only considered members declared directly
            // on the container (derived) type.
            var nameParameter = Assert.Single(searchOperation.Parameters, parameter => parameter.Name == "Name");
            Assert.Equal("The full name of the person.", nameParameter.Description);
            var limitParameter = Assert.Single(searchOperation.Parameters, parameter => parameter.Name == "Limit");
            Assert.Equal("The maximum number of results to return.", limitParameter.Description);
        });
    }

    [Fact]
    public async Task SupportsXmlCommentsOnShadowedPropertiesFromControllers()
    {
        var source =
"""
using System.Collections.Generic;
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
    [HttpGet]
    public IEnumerable<Person> Search([FromQuery] DerivedQuery query)
    {
        return [];
    }
}

public partial class Program {}

public record Person(int Id, string Name);

public class BaseQuery
{
    /// <summary>
    /// The base filter value.
    /// </summary>
    public object? Filter { get; init; }
}

public class DerivedQuery : BaseQuery
{
    /// <summary>
    /// The filter to apply to the search.
    /// </summary>
    public new string? Filter { get; init; }
}
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var searchOperation = document.Paths["/Test"].Operations[HttpMethod.Get];
            // `Filter` is redeclared on the derived type with `new` and a different type,
            // so a plain `Type.GetProperty(name)` would throw an `AmbiguousMatchException`.
            // The lookup must resolve the most-derived declaration and apply its documentation.
            var filterParameter = Assert.Single(searchOperation.Parameters, parameter => parameter.Name == "Filter");
            Assert.Equal("The filter to apply to the search.", filterParameter.Description);
        });
    }
}
