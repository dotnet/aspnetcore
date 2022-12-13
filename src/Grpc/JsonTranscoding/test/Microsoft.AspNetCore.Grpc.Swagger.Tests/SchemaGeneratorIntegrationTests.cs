// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Messages;
using Microsoft.AspNetCore.Grpc.Swagger.Internal;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests;

public class SchemaGeneratorIntegrationTests
{
    private (OpenApiSchema Schema, SchemaRepository SchemaRepository) GenerateSchema(System.Type type)
    {
        var dataContractResolver = new GrpcDataContractResolver(new JsonSerializerDataContractResolver(new JsonSerializerOptions()));
        var schemaGenerator = new SchemaGenerator(new SchemaGeneratorOptions(), dataContractResolver);
        var schemaRepository = new SchemaRepository();

        var schema = schemaGenerator.GenerateSchema(type, schemaRepository);

        return (schema, schemaRepository);
    }

    [Fact]
    public void GenerateSchema_EnumValue_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(EnumMessage));

        // Assert
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.Equal(1, schema.Properties.Count);

        var enumSchema = repository.Schemas[schema.Properties["enumValue"].Reference.Id];
        Assert.Equal("string", enumSchema.Type);
        Assert.Equal(5, enumSchema.Enum.Count);

        var enumValues = enumSchema.Enum.Select(e => ((OpenApiString)e).Value).ToList();
        Assert.Contains("NEG", enumValues);
        Assert.Contains("NESTED_ENUM_UNSPECIFIED", enumValues);
        Assert.Contains("FOO", enumValues);
        Assert.Contains("BAR", enumValues);
        Assert.Contains("BAZ", enumValues);
    }

    [Fact]
    public void GenerateSchema_BasicMessage_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(HelloReply));

        // Assert
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.Equal(2, schema.Properties.Count);
        Assert.Equal("string", schema.Properties["message"].Type);
        var valuesSchema = schema.Properties["values"];
        Assert.Equal("array", valuesSchema.Type);
        Assert.NotNull(valuesSchema.Items);
        Assert.Equal("string", valuesSchema.Items.Type);
    }

    [Fact]
    public void GenerateSchema_RecursiveMessage_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(RecursiveMessage));

        // Assert
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.Equal(1, schema.Properties.Count);
        Assert.Equal("RecursiveMessage", schema.Properties["child"].Reference.Id);
    }

    [Fact]
    public void GenerateSchema_BytesMessage_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(BytesMessage));

        // Assert
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.Equal(2, schema.Properties.Count);
        Assert.Equal("string", schema.Properties["bytesValue"].Type);
        Assert.Equal("string", schema.Properties["bytesNullableValue"].Type);
    }

    [Fact]
    public void GenerateSchema_ListValues_ReturnSchema()
    {
        // Arrange & Act
        var (schema, _) = GenerateSchema(typeof(ListValue));

        // Assert
        Assert.Equal("array", schema.Type);
        Assert.NotNull(schema.Items);
        Assert.Null(schema.Items.Type);
    }

    [Fact]
    public void GenerateSchema_Struct_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(Struct));

        _ = repository.Schemas.Count;

        // Assert
        Assert.Equal("Struct", schema.Reference.Id);

        var resolvedSchema = repository.Schemas[schema.Reference.Id];

        Assert.Equal("object", resolvedSchema.Type);
        Assert.Equal(0, resolvedSchema.Properties.Count);
        Assert.NotNull(resolvedSchema.AdditionalProperties);
        Assert.Null(resolvedSchema.AdditionalProperties.Type);
    }

    [Fact]
    public void GenerateSchema_Any_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(Any));

        // Assert
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Null(schema.AdditionalProperties.Type);
        Assert.Equal(1, schema.Properties.Count);
        Assert.Equal("string", schema.Properties["@type"].Type);
    }

    [Fact]
    public void GenerateSchema_OneOf_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(OneOfMessage));

        // Assert
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.Equal(4, schema.Properties.Count);
        Assert.Equal("string", schema.Properties["firstOne"].Type);
        Assert.Equal("string", schema.Properties["firstTwo"].Type);
        Assert.Equal("string", schema.Properties["secondOne"].Type);
        Assert.Equal("string", schema.Properties["secondTwo"].Type);
        Assert.Null(schema.AdditionalProperties);
    }

    [Fact]
    public void GenerateSchema_Map_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(MapMessage));

        // Assert
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.Equal(1, schema.Properties.Count);
        Assert.Equal("object", schema.Properties["mapValue"].Type);
        Assert.Equal("number", schema.Properties["mapValue"].AdditionalProperties.Type);
        Assert.Equal("double", schema.Properties["mapValue"].AdditionalProperties.Format);
    }
}
