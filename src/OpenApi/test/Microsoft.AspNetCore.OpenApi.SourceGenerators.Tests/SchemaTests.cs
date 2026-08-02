// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.Json.Nodes;

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
            var path = document.Paths["/todo"].Operations[HttpMethod.Post];
            var todo = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("This is a todo item.", todo.Description);

            path = document.Paths["/project"].Operations[HttpMethod.Post];
            var project = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("The project that contains Todo items.", project.Description);

            path = document.Paths["/board"].Operations[HttpMethod.Post];
            var board = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("An item on the board.", board.Description);

            path = document.Paths["/protected-internal-element"].Operations[HttpMethod.Post];
            var element = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("The unique identifier for the element.", element.Properties["name"].Description);
            Assert.Equal("Can find this XML comment.", element.Description);

            path = document.Paths["/project-record"].Operations[HttpMethod.Post];
            project = path.RequestBody.Content["application/json"].Schema;

            Assert.Equal("The name of the project.", project.Properties["name"].Description);
            Assert.Equal("The description of the project.", project.Properties["description"].Description);

            path = document.Paths["/todo-with-description"].Operations[HttpMethod.Post];
            todo = path.RequestBody.Content["application/json"].Schema;
            // Test different XML comment scenarios for properties:
            // Id: only <summary> tag -> uses summary directly
            Assert.Equal("The identifier of the todo.", todo.Properties["id"].Description);
            // Name: only <value> tag -> uses value directly
            Assert.Equal("The name of the todo.", todo.Properties["name"].Description);
            // Description: both <summary> and <value> tags -> combines with newline separator
            Assert.Equal($"A description of the the todo.\nAnother description of the todo.", todo.Properties["description"].Description);

            path = document.Paths["/type-with-examples"].Operations[HttpMethod.Post];
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

            path = document.Paths["/user"].Operations[HttpMethod.Post];
            var user = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("The unique identifier for the user.", user.Properties["id"].Description);
            Assert.Equal("The user's display name.", user.Properties["name"].Description);
        });
    }

    [Fact]
    public async Task XmlCommentsOnPropertiesShouldApplyToSchemaReferences()
    {
        var source = """
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddOpenApi(options => {
    var prevCreateSchemaReferenceId = options.CreateSchemaReferenceId;
    options.CreateSchemaReferenceId = (x) => x.Type == typeof(ModelInline) ? null : prevCreateSchemaReferenceId(x);
});

var app = builder.Build();

app.MapPost("/example", (RootModel model) => { });

app.Run();

/// <summary>
/// Comment on class ModelWithSummary.
/// </summary>
/// <example>
/// { "street": "ModelWithSummaryClass" }
/// </example>
public class ModelWithSummary
{
    public string Street { get; set; }
}

public class ModelWithoutSummary
{
    public string Street { get; set; }
}

/// <summary>
/// Comment on class ModelInline.
/// </summary>
/// <example>
/// { "street": "ModelInlineClass" }
/// </example>
public class ModelInline
{
    public string Street { get; set; }
}

/// <summary>
/// Comment on class RootModel.
/// </summary>
/// <example>
/// { }
/// </example>
public class RootModel
{
    public ModelWithSummary NoPropertyComment { get; set; }

    /// <summary>
    /// Comment on property ModelWithSummary1.
    /// </summary>
    /// <example>
    /// { "street": "ModelWithSummary1Prop" }
    /// </example>
    public ModelWithSummary ModelWithSummary1 { get; set; }

    /// <summary>
    /// Comment on property ModelWithSummary2.
    /// </summary>
    /// <example>
    /// { "street": "ModelWithSummary2Prop" }
    /// </example>
    public ModelWithSummary ModelWithSummary2 { get; set; }

    /// <summary>
    /// Comment on property ModelWithoutSummary1.
    /// </summary>
    /// <example>
    /// { "street": "ModelWithoutSummary1Prop" }
    /// </example>
    public ModelWithoutSummary ModelWithoutSummary1 { get; set; }

    /// <summary>
    /// Comment on property ModelWithoutSummary2.
    /// </summary>
    /// <example>
    /// { "street": "ModelWithoutSummary2Prop" }
    /// </example>
    public ModelWithoutSummary ModelWithoutSummary2 { get; set; }

    /// <summary>
    /// Comment on property ModelInline1.
    /// </summary>
    /// <example>
    /// { "street": "ModelInline1Prop" }
    /// </example>
    public ModelInline ModelInline1 { get; set; }

    /// <summary>
    /// Comment on property ModelInline2.
    /// </summary>
    /// <example>
    /// { "street": "ModelInline2Prop" }
    /// </example>
    public ModelInline ModelInline2 { get; set; }
}
""";
        var generator = new XmlCommentGenerator();
        await SnapshotTestHelper.Verify(source, generator, out var compilation);
        await SnapshotTestHelper.VerifyOpenApi(compilation, document =>
        {
            var path = document.Paths["/example"].Operations[HttpMethod.Post];
            var exampleOperationBodySchema = path.RequestBody.Content["application/json"].Schema;
            Assert.Equal("Comment on class RootModel.", exampleOperationBodySchema.Description);

            var rootModelSchema = document.Components.Schemas["RootModel"];
            Assert.Equal("Comment on class RootModel.", rootModelSchema.Description);

            var modelWithSummary = document.Components.Schemas["ModelWithSummary"];
            Assert.Equal("Comment on class ModelWithSummary.", modelWithSummary.Description);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "street": "ModelWithSummaryClass" }"""), modelWithSummary.Example));

            var modelWithoutSummary = document.Components.Schemas["ModelWithoutSummary"];
            Assert.Null(modelWithoutSummary.Description);

            Assert.DoesNotContain("ModelInline", document.Components.Schemas.Keys);

            // Check RootModel properties
            var noPropertyCommentProp = Assert.IsType<OpenApiSchemaReference>(rootModelSchema.Properties["noPropertyComment"]);
            Assert.Null(noPropertyCommentProp.Reference.Description);

            var modelWithSummary1Prop = Assert.IsType<OpenApiSchemaReference>(rootModelSchema.Properties["modelWithSummary1"]);
            Assert.Equal("Comment on property ModelWithSummary1.", modelWithSummary1Prop.Description);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "street": "ModelWithSummary1Prop" }"""), modelWithSummary1Prop.Examples[0]));

            var modelWithSummary2Prop = Assert.IsType<OpenApiSchemaReference>(rootModelSchema.Properties["modelWithSummary2"]);
            Assert.Equal("Comment on property ModelWithSummary2.", modelWithSummary2Prop.Description);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "street": "ModelWithSummary2Prop" }"""), modelWithSummary2Prop.Examples[0]));

            var modelWithoutSummary1Prop = Assert.IsType<OpenApiSchemaReference>(rootModelSchema.Properties["modelWithoutSummary1"]);
            Assert.Equal("Comment on property ModelWithoutSummary1.", modelWithoutSummary1Prop.Description);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "street": "ModelWithoutSummary1Prop" }"""), modelWithoutSummary1Prop.Examples[0]));

            var modelWithoutSummary2Prop = Assert.IsType<OpenApiSchemaReference>(rootModelSchema.Properties["modelWithoutSummary2"]);
            Assert.Equal("Comment on property ModelWithoutSummary2.", modelWithoutSummary2Prop.Description);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "street": "ModelWithoutSummary2Prop" }"""), modelWithoutSummary2Prop.Examples[0]));

            var modelInline1Prop = Assert.IsType<OpenApiSchema>(rootModelSchema.Properties["modelInline1"]);
            Assert.Equal("Comment on property ModelInline1.", modelInline1Prop.Description);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "street": "ModelInline1Prop" }"""), modelInline1Prop.Example));

            var modelInline2Prop = Assert.IsType<OpenApiSchema>(rootModelSchema.Properties["modelInline2"]);
            Assert.Equal("Comment on property ModelInline2.", modelInline2Prop.Description);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "street": "ModelInline2Prop" }"""), modelInline2Prop.Example));
        });
    }
}
