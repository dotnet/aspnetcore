// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Tests;

[UsesVerify]
public class SchemaTests
{
    [Fact]
    public async Task SupportsXmlCommentsOnSchemas()
    {
        var source = """
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapPost("/todo", (Todo todo) => { });
app.MapPost("/project", (Project project) => { });
app.MapPost("/board", (ProjectBoard.BoardItem boardItem) => { });
app.MapPost("/protected-internal-element", (ProjectBoard.ProtectedInternalElement element) => { });
app.MapPost("/project-record", (ProjectRecord project) => { });
app.MapPost("/todo-with-description", (TodoWithDescription todo) => { });
app.MapPost("/type-with-examples", (TypeWithExamples typeWithExamples) => { });
app.MapPost("/user", (User user) => { });

app.Run();

/// <summary>
/// This is a todo item.
/// </summary>
public class Todo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// The project that contains <see cref="Todo"/> items.
/// </summary>
public record Project(string Name, string Description);

public class ProjectBoard
{
    /// <summary>
    /// An item on the board.
    /// </summary>
    public class BoardItem
    {
        public string Name { get; set; }
    }

    /// <summary>
    /// No XML comment processed here.
    /// </summary>
    private class Element
    {
        /// <summary>
        /// The unique identifier for the element.
        /// </summary>
        /// <remarks>
        /// This won't be emitted since it is a public
        /// property on a private class.
        /// </remarks>
        public string Name { get; set; }
    }

    /// <summary>
    /// Can find this XML comment.
    /// </summary>
    protected internal class ProtectedInternalElement
    {
        /// <summary>
        /// The unique identifier for the element.
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// No XML comment processed here.
    /// </summary>
    protected class ProtectedElement
    {
        /// <summary>
        /// The unique identifier for the element.
        /// </summary>
        public string Name { get; set; }
    }
}

/// <summary>
/// The project that contains <see cref="Todo"/> items.
/// </summary>
/// <param name="Name">The name of the project.</param>
/// <param name="Description">The description of the project.</param>
public record ProjectRecord(string Name, string Description);

public class TodoWithDescription
{
    /// <summary>
    /// The identifier of the todo.
    /// </summary>
    public int Id { get; set; }
    /// <value>
    /// The name of the todo.
    /// </value>
    public string Name { get; set; }
    /// <summary>
    /// A description of the the todo.
    /// </summary>
    /// <value>Another description of the todo.</value>
    public string Description { get; set; }
}

public class TypeWithExamples
{
    /// <example>true</example>
    public bool BooleanType { get; set; }
    /// <example>42</example>
    public int IntegerType { get; set; }
    /// <example>1234567890123456789</example>
    public long LongType { get; set; }
    /// <example>3.14</example>
    public double DoubleType { get; set; }
    /// <example>3.14</example>
    public float FloatType { get; set; }
    /// <example>2022-01-01T00:00:00Z</example>
    public DateTime DateTimeType { get; set; }
    /// <example>2022-01-01</example>
    public DateOnly DateOnlyType { get; set; }
    /// <example>Hello, World!</example>
    public string StringType { get; set; }
    /// <example>2d8f1eac-b5c6-4e29-8c62-4d9d75ef3d3d</example>
    public Guid GuidType { get; set; }
    /// <example>12:30:45</example>
    public TimeOnly TimeOnlyType { get; set; }
    /// <example>P3DT4H5M</example>
    public TimeSpan TimeSpanType { get; set; }
    /// <example>255</example>
    public byte ByteType { get; set; }
    /// <example>3.14159265359</example>
    public decimal DecimalType { get; set; }
    /// <example>https://example.com</example>
    public Uri UriType { get; set; }
}

public interface IUser
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// The user's display name.
    /// </summary>
    string Name { get; set; }
}

/// <inheritdoc/>
internal class User : IUser
{
    /// <inheritdoc/>
    public int Id { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; }
}
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var path = document.Paths["/todo"].Operations[OperationType.Post];
            var todo = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("This is a todo item.", todo.Description);

            path = document.Paths["/project"].Operations[OperationType.Post];
            var project = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("The project that contains Todo items.", project.Description);

            path = document.Paths["/board"].Operations[OperationType.Post];
            var board = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("An item on the board.", board.Description);

            path = document.Paths["/protected-internal-element"].Operations[OperationType.Post];
            var element = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("The unique identifier for the element.", element.Properties["name"].Description);
            Assert.Equal("Can find this XML comment.", element.Description);

            path = document.Paths["/project-record"].Operations[OperationType.Post];
            project = path.RequestBody.Content["application/json"].Schema;

            Assert.Equal("The name of the project.", project.Properties["name"].Description);
            Assert.Equal("The description of the project.", project.Properties["description"].Description);

            path = document.Paths["/todo-with-description"].Operations[OperationType.Post];
            todo = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("The identifier of the todo.", todo.Properties["id"].Description);
            Assert.Equal("The name of the todo.", todo.Properties["name"].Description);
            Assert.Equal("Another description of the todo.", todo.Properties["description"].Description);

            path = document.Paths["/type-with-examples"].Operations[OperationType.Post];
            var typeWithExamples = path.RequestBody.Content["application/json"].Schema;

            var booleanTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["booleanType"].Example);
            Assert.True(booleanTypeExample.GetValue<bool>());

            var integerTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["integerType"].Example);
            Assert.Equal(42, integerTypeExample.GetValue<int>());

            var longTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["longType"].Example);
            Assert.Equal(1234567890123456789, longTypeExample.GetValue<long>());

            var doubleTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["doubleType"].Example);
            Assert.Equal(3.14, doubleTypeExample.GetValue<double>());

            var floatTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["floatType"].Example);
            Assert.Equal(3.14f, floatTypeExample.GetValue<float>());

            var dateTimeTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["dateTimeType"].Example);
            Assert.Equal(new DateTime(2022, 01, 01), dateTimeTypeExample.GetValue<DateTime>());

            var dateOnlyTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["dateOnlyType"].Example);
            Assert.Equal("2022-01-01", dateOnlyTypeExample.GetValue<string>());

            var stringTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["stringType"].Example);
            Assert.Equal("Hello, World!", stringTypeExample.GetValue<string>());

            var guidTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["guidType"].Example);
            Assert.Equal("2d8f1eac-b5c6-4e29-8c62-4d9d75ef3d3d", guidTypeExample.GetValue<string>());

            var byteTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["byteType"].Example);
            Assert.Equal(255, byteTypeExample.GetValue<int>());

            var timeOnlyTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["timeOnlyType"].Example);
            Assert.Equal("12:30:45", timeOnlyTypeExample.GetValue<string>());

            var timeSpanTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["timeSpanType"].Example);
            Assert.Equal("P3DT4H5M", timeSpanTypeExample.GetValue<string>());

            var decimalTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["decimalType"].Example);
            Assert.Equal(3.14159265359m, decimalTypeExample.GetValue<decimal>());

            var uriTypeExample = Assert.IsAssignableFrom<JsonNode>(typeWithExamples.Properties["uriType"].Example);
            Assert.Equal("https://example.com", uriTypeExample.GetValue<string>());

            path = document.Paths["/user"].Operations[OperationType.Post];
            var user = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("The unique identifier for the user.", user.Properties["id"].Description);
            Assert.Equal("The user's display name.", user.Properties["name"].Description);
        });
    }
}
