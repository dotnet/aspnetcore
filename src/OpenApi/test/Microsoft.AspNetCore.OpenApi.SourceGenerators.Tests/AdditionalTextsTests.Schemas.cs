// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Tests;

[UsesVerify]
public partial class AdditionalTextsTests
{
    [Fact]
    public async Task CanHandleXmlForSchemasInAdditionalTexts()
    {
        var source = """
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ClassLibrary;

var builder = WebApplication.CreateBuilder();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapPost("/todo", (Todo todo) => { });
app.MapPost("/project", (Project project) => { });
app.MapPost("/board", (ProjectBoard.BoardItem boardItem) => { });
app.MapPost("/project-record", (ProjectRecord project) => { });
app.MapPost("/todo-with-description", (TodoWithDescription todo) => { });
app.MapPost("/type-with-examples", (TypeWithExamples typeWithExamples) => { });
app.MapPost("/external-method", ClassLibrary.Endpoints.ExternalMethod);

app.Run();
""";

        var librarySource = """
using System;

namespace ClassLibrary;

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
        /// <summary>
        /// The identifier of the board item. Defaults to "name".
        /// </summary>
        public string Name { get; set; }
    }

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

    protected internal class ProtectedInternalElement
    {
        /// <summary>
        /// The unique identifier for the element.
        /// </summary>
        public string Name { get; set; }
    }

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
    /// A description of the todo.
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

public class Holder<T>
{
    /// <summary>
    /// The value to hold.
    /// </summary>
    public T Value { get; set; }

    public Holder(T value)
    {
        Value = value;
    }
}

public static class Endpoints
{
    /// <summary>
    /// An external method.
    /// </summary>
    /// <param name="name">The name of the tester. Defaults to "Tester".</param>
    public static void ExternalMethod(string name = "Tester") { }

    /// <summary>
    /// Creates a holder for the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to hold.</param>
    /// <returns>A holder for the specified value.</returns>
    /// <example>{ value: 42 }</example>
    public static Holder<T> CreateHolder<T>(T value) => new(value);
}
""";
        var references = new Dictionary<string, List<string>>
        {
            { "ClassLibrary", [librarySource] }
        };

        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, references, out var compilation, out var additionalAssemblies);
        await SnapshotTestHelper.VerifyOpenApi(compilation, additionalAssemblies, document =>
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
        });
    }
}
