// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task SchemaDescriptions_HandlesSchemaReferences()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (DescribedReferencesDto dto) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody;

            Assert.NotNull(requestBody);
            var content = Assert.Single(requestBody.Content);
            Assert.Equal("application/json", content.Key);
            Assert.NotNull(content.Value.Schema);
            var schema = content.Value.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("child1", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("Property: DescribedReferencesDto.Child1", reference.Reference.Description);
                },
                property =>
                {
                    Assert.Equal("child2", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("Property: DescribedReferencesDto.Child2", reference.Reference.Description);
                },
                property =>
                {
                    Assert.Equal("childNoDescription", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Null(reference.Reference.Description);
                });

            var referencedSchema = document.Components.Schemas["DescribedChildDto"];
            Assert.Equal("Class: DescribedChildDto", referencedSchema.Description);
        });

    }

    [Description("Class: DescribedReferencesDto")]
    public class DescribedReferencesDto
    {
        [Description("Property: DescribedReferencesDto.Child1")]
        public DescribedChildDto Child1 { get; set; }

        [Description("Property: DescribedReferencesDto.Child2")]
        public DescribedChildDto Child2 { get; set; }

        public DescribedChildDto ChildNoDescription { get; set; }
    }

    [Description("Class: DescribedChildDto")]
    public class DescribedChildDto
    {
        [Description("Property: DescribedChildDto.ChildValue")]
        public string ChildValue { get; set; }
    }

    [Fact]
    public async Task SchemaDescriptions_HandlesInlinedSchemas()
    {
        // Arrange
        var builder = CreateBuilder();

        var options = new OpenApiOptions();
        var originalCreateSchemaReferenceId = options.CreateSchemaReferenceId;
        options.CreateSchemaReferenceId = (x) => x.Type == typeof(DescribedInlinedDto) ? null : originalCreateSchemaReferenceId(x);

        // Act
        builder.MapPost("/", (DescribedInlinedSchemasDto dto) => { });

        // Assert
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody;

            Assert.NotNull(requestBody);
            var content = Assert.Single(requestBody.Content);
            Assert.Equal("application/json", content.Key);
            Assert.NotNull(content.Value.Schema);
            var schema = content.Value.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("inlined1", property.Key);
                    var inlinedSchema = Assert.IsType<OpenApiSchema>(property.Value);
                    Assert.Equal("Property: DescribedInlinedSchemasDto.Inlined1", inlinedSchema.Description);
                },
                property =>
                {
                    Assert.Equal("inlined2", property.Key);
                    var inlinedSchema = Assert.IsType<OpenApiSchema>(property.Value);
                    Assert.Equal("Property: DescribedInlinedSchemasDto.Inlined2", inlinedSchema.Description);
                },
                property =>
                {
                    Assert.Equal("inlinedNoDescription", property.Key);
                    var inlinedSchema = Assert.IsType<OpenApiSchema>(property.Value);
                    Assert.Equal("Class: DescribedInlinedDto", inlinedSchema.Description);
                });
        });
    }

    [Description("Class: DescribedInlinedSchemasDto")]
    public class DescribedInlinedSchemasDto
    {
        [Description("Property: DescribedInlinedSchemasDto.Inlined1")]
        public DescribedInlinedDto Inlined1 { get; set; }

        [Description("Property: DescribedInlinedSchemasDto.Inlined2")]
        public DescribedInlinedDto Inlined2 { get; set; }

        public DescribedInlinedDto InlinedNoDescription { get; set; }
    }

    [Description("Class: DescribedInlinedDto")]
    public class DescribedInlinedDto
    {
        [Description("Property: DescribedInlinedDto.ChildValue")]
        public string ChildValue { get; set; }
    }
}
