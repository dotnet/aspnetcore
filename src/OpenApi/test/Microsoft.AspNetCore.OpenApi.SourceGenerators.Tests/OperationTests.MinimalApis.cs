// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Microsoft.OpenApi.Models;

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

app.Run();

public static class RouteHandlerExtensionMethods
{
    /// <summary>
    /// A summary of the action.
    /// </summary>
    /// <description>
    /// A description of the action.
    /// </description>
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
    public static string Get3(string name)
    {
        return $"Hello, {name}!";
    }

    /// <response code="404">Indicates that the value was not found.</response>
    public static NotFound<string> Get4()
    {
        return TypedResults.NotFound("Not found!");
    }

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
    public static async Task<Holder<string>> Get14()
    {
        await Task.Delay(1000);
        return new Holder<string> { Value = "Hello, World!" };
    }
    /// <summary>
    /// A summary of Get15.
    /// </summary>
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
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var path = document.Paths["/1"].Operations[OperationType.Get];
            Assert.Equal("A summary of the action.", path.Summary);
            Assert.Equal("A description of the action.", path.Description);

            var path2 = document.Paths["/2"].Operations[OperationType.Get];
            Assert.Equal("The name of the person.", path2.Parameters[0].Description);
            Assert.Equal("Returns the greeting.", path2.Responses["200"].Description);

            var path3 = document.Paths["/3"].Operations[OperationType.Get];
            Assert.Equal("The name of the person.", path3.Parameters[0].Description);
            var example = Assert.IsAssignableFrom<JsonNode>(path3.Parameters[0].Example);
            Assert.Equal("\"Testy McTester\"", example.ToJsonString());

            var path4 = document.Paths["/4"].Operations[OperationType.Get];
            var response = path4.Responses["404"];
            Assert.Equal("Indicates that the value was not found.", response.Description);

            var path5 = document.Paths["/5"].Operations[OperationType.Get];
            Assert.Equal("Indicates that the value was not found.", path5.Responses["404"].Description);
            Assert.Equal("Indicates that the value is even.", path5.Responses["200"].Description);
            Assert.Equal("Indicates that the value is less than 50.", path5.Responses["201"].Description);

            var path6 = document.Paths["/6"].Operations[OperationType.Post];
            Assert.Equal("Creates a new user.", path6.Summary);
            Assert.Contains("Sample request:", path6.Description);
            var userParam = path6.RequestBody.Content["application/json"];
            var userExample = Assert.IsAssignableFrom<JsonNode>(userParam.Example);
            Assert.Equal("johndoe", userExample["username"].GetValue<string>());

            var path7 = document.Paths["/7"].Operations[OperationType.Put];
            var idParam = path7.Parameters.First(p => p.Name == "id");
            Assert.True(idParam.Deprecated);
            Assert.Equal("Legacy ID parameter - use uuid instead.", idParam.Description);

            var path8 = document.Paths["/8"].Operations[OperationType.Get];
            Assert.Equal("A summary of Get8.", path8.Summary);

            var path9 = document.Paths["/9"].Operations[OperationType.Get];
            Assert.Equal("A summary of Get9.", path9.Summary);

            var path10 = document.Paths["/10"].Operations[OperationType.Get];
            Assert.Equal("A summary of Get10.", path10.Summary);

            var path11 = document.Paths["/11"].Operations[OperationType.Get];
            Assert.Equal("A summary of Get11.", path11.Summary);

            var path12 = document.Paths["/12"].Operations[OperationType.Get];
            Assert.Equal("A summary of Get12.", path12.Summary);

            var path13 = document.Paths["/13"].Operations[OperationType.Get];
            Assert.Equal("A summary of Get13.", path13.Summary);

            var path14 = document.Paths["/14"].Operations[OperationType.Get];
            Assert.Equal("A summary of Get14.", path14.Summary);

            var path15 = document.Paths["/15"].Operations[OperationType.Get];
            Assert.Equal("A summary of Get15.", path15.Summary);

            var path16 = document.Paths["/16"].Operations[OperationType.Post];
            Assert.Equal("A summary of Post16.", path16.Summary);

            var path17 = document.Paths["/17"].Operations[OperationType.Get];
            Assert.Equal("A summary of Get17.", path17.Summary);
        });
    }
}
