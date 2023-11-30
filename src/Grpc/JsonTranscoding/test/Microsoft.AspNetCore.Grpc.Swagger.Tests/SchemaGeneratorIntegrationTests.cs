// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Messages;
using Microsoft.AspNetCore.Grpc.Swagger.Internal;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests;

public class SchemaGeneratorIntegrationTests
{
    private (OpenApiSchema Schema, SchemaRepository SchemaRepository) GenerateSchema(System.Type type, IDescriptor descriptor)
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
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.Equal(1, schema.Properties.Count);

        var enumSchema = repository.Schemas[schema.Properties["enumValue"].Reference.Id];
        Assert.Equal("string", enumSchema.Type);
        Assert.Equal(5, enumSchema.Enum.Count);

        var enumValues = enumSchema.Enum.Select(e => ((OpenApiString)e).Value).OrderBy(s => s).ToList();
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
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("string", schema.Type);
        Assert.Equal(5, schema.Enum.Count);

        var enumValues = schema.Enum.Select(e => ((OpenApiString)e).Value).OrderBy(s => s).ToList();
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
        var (schema, repository) = GenerateSchema(typeof(RecursiveMessage), RecursiveMessage.Descriptor);

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
        var (schema, repository) = GenerateSchema(typeof(BytesMessage), BytesMessage.Descriptor);

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
        var (schema, _) = GenerateSchema(typeof(ListValue), ListValue.Descriptor);

        // Assert
        Assert.Equal("array", schema.Type);
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
        var (schema, repository) = GenerateSchema(typeof(Any), Any.Descriptor);

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
        var (schema, repository) = GenerateSchema(typeof(OneOfMessage), OneOfMessage.Descriptor);

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
        var (schema, repository) = GenerateSchema(typeof(MapMessage), MapMessage.Descriptor);

        // Assert
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.Equal(1, schema.Properties.Count);
        Assert.Equal("object", schema.Properties["mapValue"].Type);
        Assert.Equal("number", schema.Properties["mapValue"].AdditionalProperties.Type);
        Assert.Equal("double", schema.Properties["mapValue"].AdditionalProperties.Format);
    }

    [Fact]
    public void GenerateSchema_FieldMask_ReturnSchema()
    {
        // Arrange & Act
        var (schema, repository) = GenerateSchema(typeof(FieldMaskMessage), FieldMaskMessage.Descriptor);

        // Assert
        schema = repository.Schemas[schema.Reference.Id];
        Assert.Equal("object", schema.Type);
        Assert.Equal(1, schema.Properties.Count);
        Assert.Equal("string", schema.Properties["fieldMaskValue"].Type);
    }
}
