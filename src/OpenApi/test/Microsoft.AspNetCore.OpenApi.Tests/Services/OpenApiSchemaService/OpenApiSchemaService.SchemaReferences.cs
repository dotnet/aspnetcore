// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task SchemaReferences_HandlesCircularReferencesRegardlessOfPropertyOrder_SelfFirst()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectCircularModelSelfFirst dto) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Components.Schemas["DirectCircularModelSelfFirst"];
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("self", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("#/components/schemas/DirectCircularModelSelfFirst", reference.Reference.ReferenceV3);
                },
                property =>
                {
                    Assert.Equal("referenced", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                });

            // Verify that it does not result in an empty schema for a referenced schema
            var referencedSchema = document.Components.Schemas["ReferencedModel"];
            Assert.NotEmpty(referencedSchema.Properties);
            var idProperty = Assert.Single(referencedSchema.Properties);
            Assert.Equal("id", idProperty.Key);
            var idPropertySchema = Assert.IsType<OpenApiSchema>(idProperty.Value);
            Assert.Equal(JsonSchemaType.Integer, idPropertySchema.Type);
        });
    }

    [Fact]
    public async Task SchemaReferences_HandlesCircularReferencesRegardlessOfPropertyOrder_SelfLast()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectCircularModelSelfLast dto) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Components.Schemas["DirectCircularModelSelfLast"];
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("referenced", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                },
                property =>
                {
                    Assert.Equal("self", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("#/components/schemas/DirectCircularModelSelfLast", reference.Reference.ReferenceV3);
                });

            // Verify that it does not result in an empty schema for a referenced schema
            var referencedSchema = document.Components.Schemas["ReferencedModel"];
            Assert.NotEmpty(referencedSchema.Properties);
            var idProperty = Assert.Single(referencedSchema.Properties);
            Assert.Equal("id", idProperty.Key);
            var idPropertySchema = Assert.IsType<OpenApiSchema>(idProperty.Value);
            Assert.Equal(JsonSchemaType.Integer, idPropertySchema.Type);
        });
    }

    [Fact]
    public async Task SchemaReferences_HandlesCircularReferencesRegardlessOfPropertyOrder_MultipleSelf()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectCircularModelMultiple dto) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Components.Schemas["DirectCircularModelMultiple"];
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("selfFirst", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("#/components/schemas/DirectCircularModelMultiple", reference.Reference.ReferenceV3);
                },
                property =>
                {
                    Assert.Equal("referenced", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                },
                property =>
                {
                    Assert.Equal("selfLast", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("#/components/schemas/DirectCircularModelMultiple", reference.Reference.ReferenceV3);
                });

            // Verify that it does not result in an empty schema for a referenced schema
            var referencedSchema = document.Components.Schemas["ReferencedModel"];
            Assert.NotEmpty(referencedSchema.Properties);
            var idProperty = Assert.Single(referencedSchema.Properties);
            Assert.Equal("id", idProperty.Key);
            var idPropertySchema = Assert.IsType<OpenApiSchema>(idProperty.Value);
            Assert.Equal(JsonSchemaType.Integer, idPropertySchema.Type);
        });
    }

    private class DirectCircularModelSelfFirst
    {
        public DirectCircularModelSelfFirst Self { get; set; } = null!;
        public ReferencedModel Referenced { get; set; } = null!;
    }

    private class DirectCircularModelSelfLast
    {
        public ReferencedModel Referenced { get; set; } = null!;
        public DirectCircularModelSelfLast Self { get; set; } = null!;
    }

    private class DirectCircularModelMultiple
    {
        public DirectCircularModelMultiple SelfFirst { get; set; } = null!;
        public ReferencedModel Referenced { get; set; } = null!;
        public DirectCircularModelMultiple SelfLast { get; set; } = null!;
    }

    private class ReferencedModel
    {
        public int Id { get; set; }
    }
}
