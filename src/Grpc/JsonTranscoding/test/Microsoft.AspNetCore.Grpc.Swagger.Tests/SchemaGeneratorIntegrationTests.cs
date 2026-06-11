// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Messages;
using Microsoft.AspNetCore.Grpc.Swagger.Internal;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Infrastructure;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests;

public class SchemaGeneratorIntegrationTests
{
    private (IOpenApiSchema Schema, SchemaRepository SchemaRepository) GenerateSchema(System.Type type, IDescriptor descriptor)
    {
        var descriptorRegistry = new DescriptorRegistry();
        descriptorRegistry.RegisterFileDescriptor(descriptor.File);

        var dataContractResolver = new GrpcDataContractResolver(new JsonSerializerDataContractResolver(new JsonSerializerOptions()), descriptorRegistry);
        var schemaGenerator = new SchemaGenerator(new SchemaGeneratorOptions(), dataContractResolver);
        var schemaRepository = new SchemaRepository();

        var schema = schemaGenerator.GenerateSchema(type, schemaRepository);

        return (schema, schemaRepository);
    }

    [Fact]
    public void GenerateSchema_EnumValue_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(EnumMessage), EnumMessage.Descriptor);

        // Assert
        schema = OpenApiTestHelpers.ResolveSchema(repository, schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, schema);
        Assert.Single(schema.Properties);

        var enumSchema = OpenApiTestHelpers.ResolveSchema(repository, schema.Properties["enumValue"]);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, enumSchema);
        Assert.Equal(5, enumSchema.Enum.Count);

        var enumValues = enumSchema.Enum.Select(e => e.GetValue<string>()).OrderBy(s => s).ToList();
        Assert.Collection(enumValues,
            v => Assert.Equal("BAR", v),
            v => Assert.Equal("BAZ", v),
            v => Assert.Equal("FOO", v),
            v => Assert.Equal("NEG", v),
            v => Assert.Equal("NESTED_ENUM_UNSPECIFIED", v));
    }

    [Fact]
    public void GenerateSchema_EnumWithoutMessage_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(EnumWithoutMessage), MessagesReflection.Descriptor);

        // Assert
        schema = OpenApiTestHelpers.ResolveSchema(repository, schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema);
        Assert.Equal(5, schema.Enum.Count);

        var enumValues = schema.Enum.Select(e => e.GetValue<string>()).OrderBy(s => s).ToList();
        Assert.Collection(enumValues,
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_BAR", v),
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_BAZ", v),
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_FOO", v),
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_NEG", v),
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_UNSPECIFIED", v));
    }

    [Fact]
    public void GenerateSchema_BasicMessage_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(HelloReply), HelloReply.Descriptor);

        // Assert
        schema = OpenApiTestHelpers.ResolveSchema(repository, schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, schema);
        Assert.Equal(2, schema.Properties.Count);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema.Properties["message"]);
        var valuesSchema = schema.Properties["values"];
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Array, valuesSchema);
        Assert.NotNull(valuesSchema.Items);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, valuesSchema.Items);
    }

    [Fact]
    public void GenerateSchema_RecursiveMessage_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(RecursiveMessage), RecursiveMessage.Descriptor);

        // Assert
        schema = OpenApiTestHelpers.ResolveSchema(repository, schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, schema);
        Assert.Single(schema.Properties);
        Assert.Equal("RecursiveMessage", OpenApiTestHelpers.GetReferenceId(schema.Properties["child"]));
    }

    [Fact]
    public void GenerateSchema_BytesMessage_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(BytesMessage), BytesMessage.Descriptor);

        // Assert
        schema = OpenApiTestHelpers.ResolveSchema(repository, schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, schema);
        Assert.Equal(2, schema.Properties.Count);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema.Properties["bytesValue"]);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema.Properties["bytesNullableValue"]);
    }

    [Fact]
    public void GenerateSchema_ListValues_ReturnSchema()
    {
        // Arrange & Act
        var (schema, _) = GenerateSchema(typeof(ListValue), ListValue.Descriptor);

        // Assert
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Array, schema);
        Assert.NotNull(schema.Items);
        Assert.Null(schema.Items.Type);
    }

    [Fact]
    public void GenerateSchema_Struct_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(Struct), Struct.Descriptor);

        _ = repository.Schemas.Count;

        // Assert
        Assert.Equal("Struct", OpenApiTestHelpers.GetReferenceId(schema));

        var resolvedSchema = OpenApiTestHelpers.ResolveSchema(repository, schema);

        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, resolvedSchema);
        Assert.Empty(resolvedSchema.Properties);
        Assert.NotNull(resolvedSchema.AdditionalProperties);
        Assert.Null(resolvedSchema.AdditionalProperties.Type);
    }

    [Fact]
    public void GenerateSchema_Any_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(Any), Any.Descriptor);

        // Assert
        schema = OpenApiTestHelpers.ResolveSchema(repository, schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, schema);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Null(schema.AdditionalProperties.Type);
        Assert.Single(schema.Properties);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema.Properties["@type"]);
    }

    [Fact]
    public void GenerateSchema_OneOf_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(OneOfMessage), OneOfMessage.Descriptor);

        // Assert
        schema = OpenApiTestHelpers.ResolveSchema(repository, schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, schema);
        Assert.Equal(4, schema.Properties.Count);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema.Properties["firstOne"]);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema.Properties["firstTwo"]);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema.Properties["secondOne"]);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema.Properties["secondTwo"]);
        Assert.Null(schema.AdditionalProperties);
    }

    [Fact]
    public void GenerateSchema_Map_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(MapMessage), MapMessage.Descriptor);

        // Assert
        schema = OpenApiTestHelpers.ResolveSchema(repository, schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, schema);
        Assert.Single(schema.Properties);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, schema.Properties["mapValue"]);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Number, schema.Properties["mapValue"].AdditionalProperties);
        Assert.Equal("double", schema.Properties["mapValue"].AdditionalProperties.Format);
    }

    [Fact]
    public void GenerateSchema_FieldMask_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(FieldMaskMessage), FieldMaskMessage.Descriptor);

        // Assert
        schema = OpenApiTestHelpers.ResolveSchema(repository, schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, schema);
        Assert.Single(schema.Properties);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, schema.Properties["fieldMaskValue"]);
    }
}
