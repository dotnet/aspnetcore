// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Tests;

[UsesVerify]
public partial class OperationTests
{
    [Fact]
    public async Task SupportsXmlCommentsOnOperationsFromMinimalApis()
    {
        var source = """
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/1", RouteHandlerExtensionMethods.Get);
app.MapGet("/2", RouteHandlerExtensionMethods.Get2);
app.MapGet("/3", RouteHandlerExtensionMethods.Get3);
app.MapGet("/4", RouteHandlerExtensionMethods.Get4);
app.MapGet("/5", RouteHandlerExtensionMethods.Get5);
app.MapPost("/6", RouteHandlerExtensionMethods.Post6);
app.MapPut("/7", RouteHandlerExtensionMethods.Put7);
app.MapGet("/8", RouteHandlerExtensionMethods.Get8);
app.MapGet("/9", RouteHandlerExtensionMethods.Get9);
app.MapGet("/10", RouteHandlerExtensionMethods.Get10);
app.MapGet("/11", RouteHandlerExtensionMethods.Get11);
app.MapGet("/12", RouteHandlerExtensionMethods.Get12);
app.MapGet("/13", RouteHandlerExtensionMethods.Get13);
app.MapGet("/14", RouteHandlerExtensionMethods.Get14);
app.MapGet("/15", RouteHandlerExtensionMethods.Get15);
app.MapPost("/16", RouteHandlerExtensionMethods.Post16);
app.MapGet("/17", RouteHandlerExtensionMethods.Get17);
app.MapPost("/18", RouteHandlerExtensionMethods.Post18);
app.MapPost("/19", RouteHandlerExtensionMethods.Post19);
app.MapGet("/20", RouteHandlerExtensionMethods.Get20);
app.MapGet("/21", RouteHandlerExtensionMethods.Get21);
app.MapGet("/22", RouteHandlerExtensionMethods.Get22);

app.Run();

public static class RouteHandlerExtensionMethods
{
    /// <summary>
    /// A summary of the action.
    /// </summary>
    /// <description>
    /// A description of the action.
    /// </description>
    /// <returns>Returns the greeting.</returns>
    public static string Get()
    {
        return "Hello, World!";
    }

    /// <param name="name">The name of the person.</param>
    /// <response code="200">Returns the greeting.</response>
    public static string Get2(string name)
    {
        return $"Hello, {name}!";
    }

    /// <param name="name" example="Testy McTester">The name of the person.</param>
    /// <returns>Returns the greeting.</returns>
    /// <returns>Returns a different greeting.</returns>
    public static string Get3(string name)
    {
        return $"Hello, {name}!";
    }

    /// <returns>Indicates that the value was not found.</returns>
    public static NotFound<string> Get4()
    {
        return TypedResults.NotFound("Not found!");
    }

    /// <returns>This gets ignored.</returns>
    /// <response code="200">Indicates that the value is even.</response>
    /// <response code="201">Indicates that the value is less than 50.</response>
    /// <response code="404">Indicates that the value was not found.</response>
    public static Results<NotFound<string>, Ok<string?>, Created> Get5()
    {
        var randomNumber = Random.Shared.Next();
        if (randomNumber % 2 == 0)
        {
            return TypedResults.Ok("is even");
        }
        else if (randomNumber < 50)
        {
            return TypedResults.Created("is less than 50");
        }
        return TypedResults.NotFound("Not found!");
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///     POST /6
    ///     {
    ///         "username": "johndoe",
    ///         "email": "john@example.com"
    ///     }
    /// </remarks>
    /// <param name="user" example='{"username": "johndoe", "email": "john@example.com"}'>The user information.</param>
    /// <response code="201">Successfully created the user.</response>
    /// <response code="400">If the user data is invalid.</response>
    public static IResult Post6(User user)
    {
        return TypedResults.Created($"/users/{user.Username}", user);
    }

    /// <summary>
    /// Updates an existing record.
    /// </summary>
    /// <param name="id" deprecated="true">Legacy ID parameter - use uuid instead.</param>
    /// <param name="uuid">Unique identifier for the record.</param>
    /// <response code="204">Update successful.</response>
    /// <response code="404" deprecated="true">Legacy response - will be removed.</response>
    public static IResult Put7(int? id, string uuid)
    {
        return TypedResults.NoContent();
    }

    /// <summary>
    /// A summary of Get8.
    /// </summary>
    public static async Task Get8()
    {
        await Task.Delay(1000);
        return;
    }
    /// <summary>
    /// A summary of Get9.
    /// </summary>
    public static async ValueTask Get9()
    {
        await Task.Delay(1000);
        return;
    }
    /// <summary>
    /// A summary of Get10.
    /// </summary>
    public static Task Get10()
    {
        return Task.CompletedTask;
    }
    /// <summary>
    /// A summary of Get11.
    /// </summary>
    public static ValueTask Get11()
    {
        return ValueTask.CompletedTask;
    }
    /// <summary>
    /// A summary of Get12.
    /// </summary>
    public static Task<string> Get12()
    {
        return Task.FromResult("Hello, World!");
    }
    /// <summary>
    /// A summary of Get13.
    /// </summary>
    public static ValueTask<string> Get13()
    {
        return new ValueTask<string>("Hello, World!");
    }
    /// <summary>
    /// A summary of Get14.
    /// </summary>
    /// <returns>Returns the greeting.</returns>
    public static async Task<Holder<string>> Get14()
    {
        await Task.Delay(1000);
        return new Holder<string> { Value = "Hello, World!" };
    }
    /// <summary>
    /// A summary of Get15.
    /// </summary>
    /// <response code="200">Returns the greeting.</response>
    public static Task<Holder<string>> Get15()
    {
        return Task.FromResult(new Holder<string> { Value = "Hello, World!" });
    }

    /// <summary>
    /// A summary of Post16.
    /// </summary>
    public static void Post16(Example example)
    {
        return;
    }

    /// <summary>
    /// A summary of Get17.
    /// </summary>
    public static int[][] Get17(int[] args)
    {
        return [[1, 2, 3], [4, 5, 6], [7, 8, 9], args];
    }

    /// <summary>
    /// A summary of Post18.
    /// </summary>
    public static int Post18([AsParameters] FirstParameters queryParameters, [AsParameters] SecondParameters bodyParameters)
    {
        return 0;
    }

    /// <summary>
    /// Tests mixed regular and AsParameters with examples.
    /// </summary>
    /// <param name="regularParam">A regular parameter with documentation.</param>
    /// <param name="mixedParams">Mixed parameter class with various types.</param>
    public static IResult Post19(string regularParam, [AsParameters] MixedParametersClass mixedParams)
    {
        return TypedResults.Ok($"Regular: {regularParam}, Email: {mixedParams.Email}");
    }

    /// <summary>
    /// Tests AsParameters with different binding sources.
    /// </summary>
    /// <param name="bindingParams">Parameters from different sources.</param>
    public static IResult Get20([AsParameters] BindingSourceParametersClass bindingParams)
    {
        return TypedResults.Ok($"Query: {bindingParams.QueryParam}, Header: {bindingParams.HeaderParam}");
    }

    /// <summary>
    /// Tests XML documentation priority order (value > returns > summary).
    /// </summary>
    /// <param name="priorityParams">Parameters demonstrating XML doc priority.</param>
    public static IResult Get21([AsParameters] XmlDocPriorityParametersClass priorityParams)
    {
        return TypedResults.Ok($"Processed parameters");
    }

    /// <summary>
    /// Tests summary and value documentation priority on AsParameters properties.
    /// </summary>
    /// <param name="summaryValueParams">Parameters testing summary vs value priority.</param>
    public static IResult Get22([AsParameters] SummaryValueParametersClass summaryValueParams)
    {
        return TypedResults.Ok($"Summary: {summaryValueParams.SummaryProperty}, Value: {summaryValueParams.ValueProperty}");
    }
}

public class FirstParameters
{
    /// <summary>
    /// The name of the person.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// The age of the person.
    /// </summary>
    /// <example>30</example>
    public int? Age { get; set; }
    /// <summary>
    /// The user information.
    /// </summary>
    /// <example>
    /// {
    ///   "username": "johndoe",
    ///   "email": "johndoe@example.com"
    /// }
    /// </example>
    public User? User { get; set; }
}

public class SecondParameters
{
    /// <summary>
    /// The description of the project.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// The service used for testing.
    /// </summary>
    [FromServices]
    public Example Service { get; set; }
}

public class User
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Holder<T>
{
    public T Value { get; set; } = default!;
}

public class Example : Task<int>
{
    public Example(Func<int> function) : base(function)
    {
    }

    public Example(Func<object?, int> function, object? state) : base(function, state)
    {
    }
}

public class MixedParametersClass
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    /// <example>"user@example.com"</example>
    public string? Email { get; set; }

    /// <summary>
    /// The user's age in years.
    /// </summary>
    /// <example>25</example>
    public int Age { get; set; }

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    /// <example>true</example>
    public bool IsActive { get; set; }
}

public class BindingSourceParametersClass
{
    /// <summary>
    /// Query parameter from URL.
    /// </summary>
    [FromQuery]
    public string? QueryParam { get; set; }

    /// <summary>
    /// Header value from request.
    /// </summary>
    [FromHeader]
    public string? HeaderParam { get; set; }
}

public class XmlDocPriorityParametersClass
{
    /// <summary>
    /// Property with only summary documentation.
    /// </summary>
    public string? SummaryOnlyProperty { get; set; }

    /// <summary>
    /// Property with summary documentation that should be overridden.
    /// </summary>
    /// <returns>Returns-based description that should take precedence over summary.</returns>
    public string? SummaryAndReturnsProperty { get; set; }

    /// <summary>
    /// Property with all three types of documentation.
    /// </summary>
    /// <returns>Returns-based description that should be overridden by value.</returns>
    /// <value>Value-based description that should take highest precedence.</value>
    public string? AllThreeProperty { get; set; }

    /// <returns>Returns-only description.</returns>
    public string? ReturnsOnlyProperty { get; set; }

    /// <value>Value-only description.</value>
    public string? ValueOnlyProperty { get; set; }
}

public class SummaryValueParametersClass
{
    /// <summary>
    /// Property with only summary documentation.
    /// </summary>
    public string? SummaryProperty { get; set; }

    /// <summary>
    /// Property with summary that should be overridden by value.
    /// </summary>
    /// <value>Value description that should take precedence over summary.</value>
    public string? ValueProperty { get; set; }

    /// <value>Property with only value documentation.</value>
    public string? ValueOnlyProperty { get; set; }
}
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var path = document.Paths["/1"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of the action.", path.Summary);
            Assert.Equal("A description of the action.", path.Description);
            Assert.Equal("Returns the greeting.", path.Responses["200"].Description);

            var path2 = document.Paths["/2"].Operations[HttpMethod.Get];
            Assert.Equal("The name of the person.", path2.Parameters[0].Description);
            Assert.Equal("Returns the greeting.", path2.Responses["200"].Description);

            var path3 = document.Paths["/3"].Operations[HttpMethod.Get];
            Assert.Equal("The name of the person.", path3.Parameters[0].Description);
            var example = Assert.IsAssignableFrom<JsonNode>(path3.Parameters[0].Example);
            Assert.Equal("\"Testy McTester\"", example.ToJsonString());
            Assert.Equal("Returns the greeting.", path3.Responses["200"].Description);

            var path4 = document.Paths["/4"].Operations[HttpMethod.Get];
            var response = path4.Responses["404"];
            Assert.Equal("Indicates that the value was not found.", response.Description);

            var path5 = document.Paths["/5"].Operations[HttpMethod.Get];
            Assert.Equal("Indicates that the value was not found.", path5.Responses["404"].Description);
            Assert.Equal("Indicates that the value is even.", path5.Responses["200"].Description);
            Assert.Equal("Indicates that the value is less than 50.", path5.Responses["201"].Description);

            var path6 = document.Paths["/6"].Operations[HttpMethod.Post];
            Assert.Equal("Creates a new user.", path6.Summary);
            Assert.Contains("Sample request:", path6.Description);
            var userParam = path6.RequestBody.Content["application/json"];
            var userExample = Assert.IsAssignableFrom<JsonNode>(userParam.Example);
            Assert.Equal("johndoe", userExample["username"].GetValue<string>());

            var path7 = document.Paths["/7"].Operations[HttpMethod.Put];
            var idParam = path7.Parameters.First(p => p.Name == "id");
            Assert.True(idParam.Deprecated);
            Assert.Equal("Legacy ID parameter - use uuid instead.", idParam.Description);

            var path8 = document.Paths["/8"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of Get8.", path8.Summary);

            var path9 = document.Paths["/9"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of Get9.", path9.Summary);

            var path10 = document.Paths["/10"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of Get10.", path10.Summary);

            var path11 = document.Paths["/11"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of Get11.", path11.Summary);

            var path12 = document.Paths["/12"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of Get12.", path12.Summary);

            var path13 = document.Paths["/13"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of Get13.", path13.Summary);

            var path14 = document.Paths["/14"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of Get14.", path14.Summary);
            Assert.Equal("Returns the greeting.", path14.Responses["200"].Description);

            var path15 = document.Paths["/15"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of Get15.", path15.Summary);
            Assert.Equal("Returns the greeting.", path15.Responses["200"].Description);

            var path16 = document.Paths["/16"].Operations[HttpMethod.Post];
            Assert.Equal("A summary of Post16.", path16.Summary);

            var path17 = document.Paths["/17"].Operations[HttpMethod.Get];
            Assert.Equal("A summary of Get17.", path17.Summary);

            var path18 = document.Paths["/18"].Operations[HttpMethod.Post];
            Assert.Equal("A summary of Post18.", path18.Summary);
            Assert.Equal("The name of the person.", path18.Parameters[0].Description);
            Assert.Equal("The age of the person.", path18.Parameters[1].Description);
            Assert.Equal(30, path18.Parameters[1].Example.GetValue<int>());
            Assert.Equal("The description of the project.", path18.Parameters[2].Description);
            Assert.Equal("The user information.", path18.RequestBody.Description);
            var path18RequestBody = path18.RequestBody.Content["application/json"];
            var path18Example = Assert.IsAssignableFrom<JsonNode>(path18RequestBody.Example);
            Assert.Equal("johndoe", path18Example["username"].GetValue<string>());
            Assert.Equal("johndoe@example.com", path18Example["email"].GetValue<string>());

            var path19 = document.Paths["/19"].Operations[HttpMethod.Post];
            Assert.Equal("Tests mixed regular and AsParameters with examples.", path19.Summary);
            Assert.Equal("A regular parameter with documentation.", path19.Parameters[0].Description);
            Assert.Equal("The user's email address.", path19.Parameters[1].Description);
            Assert.Equal("user@example.com", path19.Parameters[1].Example.GetValue<string>());
            Assert.Equal("The user's age in years.", path19.Parameters[2].Description);
            Assert.Equal(25, path19.Parameters[2].Example.GetValue<int>());
            Assert.Equal("Whether the user is active.", path19.Parameters[3].Description);
            Assert.True(path19.Parameters[3].Example.GetValue<bool>());

            var path20 = document.Paths["/20"].Operations[HttpMethod.Get];
            Assert.Equal("Tests AsParameters with different binding sources.", path20.Summary);
            Assert.Equal("Query parameter from URL.", path20.Parameters[0].Description);
            Assert.Equal("Header value from request.", path20.Parameters[1].Description);

            // Test XML documentation priority order: value > returns > summary
            var path22 = document.Paths["/21"].Operations[HttpMethod.Get];
            // Find parameters by name for clearer assertions
            var summaryOnlyParam = path22.Parameters.First(p => p.Name == "SummaryOnlyProperty");
            Assert.Equal("Property with only summary documentation.", summaryOnlyParam.Description);

            var summaryAndReturnsParam = path22.Parameters.First(p => p.Name == "SummaryAndReturnsProperty");
            Assert.Equal("Property with summary documentation that should be overridden.", summaryAndReturnsParam.Description);

            var allThreeParam = path22.Parameters.First(p => p.Name == "AllThreeProperty");
            Assert.Equal($"Property with all three types of documentation.\nValue-based description that should take highest precedence.", allThreeParam.Description);

            var returnsOnlyParam = path22.Parameters.First(p => p.Name == "ReturnsOnlyProperty");
            Assert.Null(returnsOnlyParam.Description);

            var valueOnlyParam = path22.Parameters.First(p => p.Name == "ValueOnlyProperty");
            Assert.Equal("Value-only description.", valueOnlyParam.Description);

            // Test summary and value documentation priority for AsParameters
            var path23 = document.Paths["/22"].Operations[HttpMethod.Get];
            Assert.Equal("Tests summary and value documentation priority on AsParameters properties.", path23.Summary);

            var summaryParam = path23.Parameters.First(p => p.Name == "SummaryProperty");
            Assert.Equal("Property with only summary documentation.", summaryParam.Description);

            var valueParam = path23.Parameters.First(p => p.Name == "ValueProperty");
            Assert.Equal($"Property with summary that should be overridden by value.\nValue description that should take precedence over summary.", valueParam.Description);

            var valueOnlyParam2 = path23.Parameters.First(p => p.Name == "ValueOnlyProperty");
            Assert.Equal("Property with only value documentation.", valueOnlyParam2.Description);
        });
    }
}
