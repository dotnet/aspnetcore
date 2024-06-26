// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

public class OpenApiSchemaComparerTests
{
    public static object[][] SinglePropertyData => [
        [new OpenApiSchema { Title = "Title" }, new OpenApiSchema { Title = "Title" }, true],
        [new OpenApiSchema { Title = "Title" }, new OpenApiSchema { Title = "Another Title" }, false],
        [new OpenApiSchema { Type = "string" }, new OpenApiSchema { Type = "string" }, true],
        [new OpenApiSchema { Type = "string" }, new OpenApiSchema { Type = "integer" }, false],
        [new OpenApiSchema { Format = "int32" }, new OpenApiSchema { Format = "int32" }, true],
        [new OpenApiSchema { Format = "int32" }, new OpenApiSchema { Format = "int64" }, false],
        [new OpenApiSchema { Maximum = 10 }, new OpenApiSchema { Maximum = 10 }, true],
        [new OpenApiSchema { Maximum = 10 }, new OpenApiSchema { Maximum = 20 }, false],
        [new OpenApiSchema { Minimum = 10 }, new OpenApiSchema { Minimum = 10 }, true],
        [new OpenApiSchema { Minimum = 10 }, new OpenApiSchema { Minimum = 20 }, false],
        [new OpenApiSchema { ExclusiveMaximum = true }, new OpenApiSchema { ExclusiveMaximum = true }, true],
        [new OpenApiSchema { ExclusiveMaximum = true }, new OpenApiSchema { ExclusiveMaximum = false }, false],
        [new OpenApiSchema { ExclusiveMinimum = true }, new OpenApiSchema { ExclusiveMinimum = true }, true],
        [new OpenApiSchema { ExclusiveMinimum = true }, new OpenApiSchema { ExclusiveMinimum = false }, false],
        [new OpenApiSchema { MaxLength = 10 }, new OpenApiSchema { MaxLength = 10 }, true],
        [new OpenApiSchema { MaxLength = 10 }, new OpenApiSchema { MaxLength = 20 }, false],
        [new OpenApiSchema { MinLength = 10 }, new OpenApiSchema { MinLength = 10 }, true],
        [new OpenApiSchema { MinLength = 10 }, new OpenApiSchema { MinLength = 20 }, false],
        [new OpenApiSchema { Pattern = "pattern" }, new OpenApiSchema { Pattern = "pattern" }, true],
        [new OpenApiSchema { Pattern = "pattern" }, new OpenApiSchema { Pattern = "another pattern" }, false],
        [new OpenApiSchema { MaxItems = 10 }, new OpenApiSchema { MaxItems = 10 }, true],
        [new OpenApiSchema { MaxItems = 10 }, new OpenApiSchema { MaxItems = 20 }, false],
        [new OpenApiSchema { MinItems = 10 }, new OpenApiSchema { MinItems = 10 }, true],
        [new OpenApiSchema { MinItems = 10 }, new OpenApiSchema { MinItems = 20 }, false],
        [new OpenApiSchema { UniqueItems = true }, new OpenApiSchema { UniqueItems = true }, true],
        [new OpenApiSchema { UniqueItems = true }, new OpenApiSchema { UniqueItems = false }, false],
        [new OpenApiSchema { MaxProperties = 10 }, new OpenApiSchema { MaxProperties = 10 }, true],
        [new OpenApiSchema { MaxProperties = 10 }, new OpenApiSchema { MaxProperties = 20 }, false],
        [new OpenApiSchema { MinProperties = 10 }, new OpenApiSchema { MinProperties = 10 }, true],
        [new OpenApiSchema { MinProperties = 10 }, new OpenApiSchema { MinProperties = 20 }, false],
        [new OpenApiSchema { Required = new HashSet<string>() { "required" } }, new OpenApiSchema { Required = new HashSet<string> { "required" } }, true],
        [new OpenApiSchema { Required = new HashSet<string>() { "name", "age" } }, new OpenApiSchema { Required = new HashSet<string> { "age", "name" } }, true],
        [new OpenApiSchema { Required = new HashSet<string>() { "required" } }, new OpenApiSchema { Required = new HashSet<string> { "another required" } }, false],
        [new OpenApiSchema { Enum = [new OpenApiString("value")] }, new OpenApiSchema { Enum = [new OpenApiString("value")] }, true],
        [new OpenApiSchema { Enum = [new OpenApiString("value")] }, new OpenApiSchema { Enum = [new OpenApiString("value2" )] }, false],
        [new OpenApiSchema { Enum = [new OpenApiString("value"), new OpenApiString("value2")] }, new OpenApiSchema { Enum = [new OpenApiString("value2" ), new OpenApiString("value" )] }, false],
        [new OpenApiSchema { Items = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { Items = new OpenApiSchema { Type = "string" } }, true],
        [new OpenApiSchema { Items = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { Items = new OpenApiSchema { Type = "integer" } }, false],
        [new OpenApiSchema { Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Type = "string" } } }, new OpenApiSchema { Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Type = "string" } } }, true],
        [new OpenApiSchema { Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Type = "string" } } }, new OpenApiSchema { Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Type = "integer" } } }, false],
        [new OpenApiSchema { AdditionalProperties = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { AdditionalProperties = new OpenApiSchema { Type = "string" } }, true],
        [new OpenApiSchema { AdditionalProperties = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { AdditionalProperties = new OpenApiSchema { Type = "integer" } }, false],
        [new OpenApiSchema { Description = "Description" }, new OpenApiSchema { Description = "Description" }, true],
        [new OpenApiSchema { Description = "Description" }, new OpenApiSchema { Description = "Another Description" }, false],
        [new OpenApiSchema { Deprecated = true }, new OpenApiSchema { Deprecated = true }, true],
        [new OpenApiSchema { Deprecated = true }, new OpenApiSchema { Deprecated = false }, false],
        [new OpenApiSchema { ExternalDocs = new OpenApiExternalDocs { Description = "Description" } }, new OpenApiSchema { ExternalDocs = new OpenApiExternalDocs { Description = "Description" } }, true],
        [new OpenApiSchema { ExternalDocs = new OpenApiExternalDocs { Description = "Description" } }, new OpenApiSchema { ExternalDocs = new OpenApiExternalDocs { Description = "Another Description" } }, false],
        [new OpenApiSchema { UnresolvedReference = true }, new OpenApiSchema { UnresolvedReference = true }, true],
        [new OpenApiSchema { UnresolvedReference = true }, new OpenApiSchema { UnresolvedReference = false }, false],
        [new OpenApiSchema { Reference = new OpenApiReference { Id = "Id", Type = ReferenceType.Schema } }, new OpenApiSchema { Reference = new OpenApiReference { Id = "Id", Type = ReferenceType.Schema } }, true],
        [new OpenApiSchema { Reference = new OpenApiReference { Id = "Id", Type = ReferenceType.Schema } }, new OpenApiSchema { Reference = new OpenApiReference { Id = "Another Id", Type = ReferenceType.Schema } }, false],
        [new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("value") } }, new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("value") } }, true],
        [new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("value") } }, new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("another value") } }, false],
        [new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("value") } }, new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key2"] = new OpenApiString("value") } }, false],
        [new OpenApiSchema { Xml = new OpenApiXml { Name = "Name" } }, new OpenApiSchema { Xml = new OpenApiXml { Name = "Name" } }, true],
        [new OpenApiSchema { Xml = new OpenApiXml { Name = "Name" } }, new OpenApiSchema { Xml = new OpenApiXml { Name = "Another Name" } }, false],
        [new OpenApiSchema { Nullable = true }, new OpenApiSchema { Nullable = true }, true],
        [new OpenApiSchema { Nullable = true }, new OpenApiSchema { Nullable = false }, false],
        [new OpenApiSchema { ReadOnly = true }, new OpenApiSchema { ReadOnly = true }, true],
        [new OpenApiSchema { ReadOnly = true }, new OpenApiSchema { ReadOnly = false }, false],
        [new OpenApiSchema { WriteOnly = true }, new OpenApiSchema { WriteOnly = true }, true],
        [new OpenApiSchema { WriteOnly = true }, new OpenApiSchema { WriteOnly = false }, false],
        [new OpenApiSchema { Discriminator = new OpenApiDiscriminator { PropertyName = "PropertyName" } }, new OpenApiSchema { Discriminator = new OpenApiDiscriminator { PropertyName = "PropertyName" } }, true],
        [new OpenApiSchema { Discriminator = new OpenApiDiscriminator { PropertyName = "PropertyName" } }, new OpenApiSchema { Discriminator = new OpenApiDiscriminator { PropertyName = "AnotherPropertyName" } }, false],
        [new OpenApiSchema { Example = new OpenApiString("example") }, new OpenApiSchema { Example = new OpenApiString("example") }, true],
        [new OpenApiSchema { Example = new OpenApiString("example") }, new OpenApiSchema { Example = new OpenApiString("another example") }, false],
        [new OpenApiSchema { Example = new OpenApiInteger(2) }, new OpenApiSchema { Example = new OpenApiString("another example") }, false],
        [new OpenApiSchema { AdditionalPropertiesAllowed = true }, new OpenApiSchema { AdditionalPropertiesAllowed = true }, true],
        [new OpenApiSchema { AdditionalPropertiesAllowed = true }, new OpenApiSchema { AdditionalPropertiesAllowed = false }, false],
        [new OpenApiSchema { Not = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { Not = new OpenApiSchema { Type = "string" } }, true],
        [new OpenApiSchema { Not = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { Not = new OpenApiSchema { Type = "integer" } }, false],
        [new OpenApiSchema { AnyOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { AnyOf = [new OpenApiSchema { Type = "string" }] }, true],
        [new OpenApiSchema { AnyOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { AnyOf = [new OpenApiSchema { Type = "integer" }] }, false],
        [new OpenApiSchema { AllOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { AllOf = [new OpenApiSchema { Type = "string" }] }, true],
        [new OpenApiSchema { AllOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { AllOf = [new OpenApiSchema { Type = "integer" }] }, false],
        [new OpenApiSchema { OneOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { OneOf = [new OpenApiSchema { Type = "string" }] }, true],
        [new OpenApiSchema { OneOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { OneOf = [new OpenApiSchema { Type = "integer" }] }, false],
        [new OpenApiSchema { MultipleOf = 10 }, new OpenApiSchema { MultipleOf = 10 }, true],
        [new OpenApiSchema { MultipleOf = 10 }, new OpenApiSchema { MultipleOf = 20 }, false],
        [new OpenApiSchema { Default = new OpenApiString("default") }, new OpenApiSchema { Default = new OpenApiString("default") }, true],
        [new OpenApiSchema { Default = new OpenApiString("default") }, new OpenApiSchema { Default = new OpenApiString("another default") }, false],
    ];

    [Theory]
    [MemberData(nameof(SinglePropertyData))]
    public void ProducesCorrectEqualityForOpenApiSchema(OpenApiSchema schema, OpenApiSchema anotherSchema, bool isEqual)
        => Assert.Equal(isEqual, OpenApiSchemaComparer.Instance.Equals(schema, anotherSchema));

    [Fact]
    public void ValidatePropertiesOnOpenApiSchema()
    {
        var propertyNames = typeof(OpenApiSchema).GetProperties().Select(property => property.Name).ToList();
        var originalSchema = new OpenApiSchema
        {
            AdditionalProperties = new OpenApiSchema(),
            AdditionalPropertiesAllowed = true,
            AllOf = [new OpenApiSchema()],
            AnyOf = [new OpenApiSchema()],
            Deprecated = true,
            Default = new OpenApiString("default"),
            Description = "description",
            Discriminator = new OpenApiDiscriminator(),
            Example = new OpenApiString("example"),
            ExclusiveMaximum = true,
            ExclusiveMinimum = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["key"] = new OpenApiString("value")
            },
            ExternalDocs = new OpenApiExternalDocs(),
            Enum = [new OpenApiString("test")],
            Format = "object",
            Items = new OpenApiSchema(),
            Maximum = 10,
            MaxItems = 10,
            MaxLength = 10,
            MaxProperties = 10,
            Minimum = 10,
            MinItems = 10,
            MinLength = 10,
            MinProperties = 10,
            MultipleOf = 10,
            OneOf = [new OpenApiSchema()],
            Not = new OpenApiSchema(),
            Nullable = false,
            Pattern = "pattern",
            Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema() },
            ReadOnly = true,
            Required = new HashSet<string> { "required" },
            Reference = new OpenApiReference { Id = "Id", Type = ReferenceType.Schema },
            UniqueItems = false,
            UnresolvedReference = true,
            WriteOnly = true,
            Xml = new OpenApiXml { Name = "Name" },
        };

        OpenApiSchema modifiedSchema = new(originalSchema) { AdditionalProperties = new OpenApiSchema { Type = "string" } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.AdditionalProperties)));

        modifiedSchema = new(originalSchema) { AdditionalPropertiesAllowed = false };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.AdditionalPropertiesAllowed)));

        modifiedSchema = new(originalSchema) { AllOf = [new OpenApiSchema { Type = "string" }] };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.AllOf)));

        modifiedSchema = new(originalSchema) { AnyOf = [new OpenApiSchema { Type = "string" }] };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.AnyOf)));

        modifiedSchema = new(originalSchema) { Deprecated = false };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Deprecated)));

        modifiedSchema = new(originalSchema) { Default = new OpenApiString("another default") };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Default)));

        modifiedSchema = new(originalSchema) { Description = "another description" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Description)));

        modifiedSchema = new(originalSchema) { Discriminator = new OpenApiDiscriminator { PropertyName = "PropertyName" } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Discriminator)));

        modifiedSchema = new(originalSchema) { Example = new OpenApiString("another example") };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Example)));

        modifiedSchema = new(originalSchema) { ExclusiveMaximum = false };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.ExclusiveMaximum)));

        modifiedSchema = new(originalSchema) { ExclusiveMinimum = false };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.ExclusiveMinimum)));

        modifiedSchema = new(originalSchema) { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("another value") } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Extensions)));

        modifiedSchema = new(originalSchema) { ExternalDocs = new OpenApiExternalDocs { Description = "another description" } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.ExternalDocs)));

        modifiedSchema = new(originalSchema) { Enum = [new OpenApiString("another test")] };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Enum)));

        modifiedSchema = new(originalSchema) { Format = "string" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Format)));

        modifiedSchema = new(originalSchema) { Items = new OpenApiSchema { Type = "string" } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Items)));

        modifiedSchema = new(originalSchema) { Maximum = 20 };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Maximum)));

        modifiedSchema = new(originalSchema) { MaxItems = 20 };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MaxItems)));

        modifiedSchema = new(originalSchema) { MaxLength = 20 };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MaxLength)));

        modifiedSchema = new(originalSchema) { MaxProperties = 20 };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MaxProperties)));

        modifiedSchema = new(originalSchema) { Minimum = 20 };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Minimum)));

        modifiedSchema = new(originalSchema) { MinItems = 20 };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MinItems)));

        modifiedSchema = new(originalSchema) { MinLength = 20 };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MinLength)));

        modifiedSchema = new(originalSchema) { MinProperties = 20 };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MinProperties)));

        modifiedSchema = new(originalSchema) { MultipleOf = 20 };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MultipleOf)));

        modifiedSchema = new(originalSchema) { OneOf = [new OpenApiSchema { Type = "string" }] };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.OneOf)));

        modifiedSchema = new(originalSchema) { Not = new OpenApiSchema { Type = "string" } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Not)));

        modifiedSchema = new(originalSchema) { Nullable = true };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Nullable)));

        modifiedSchema = new(originalSchema) { Pattern = "another pattern" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Pattern)));

        modifiedSchema = new(originalSchema) { Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Type = "integer" } } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Properties)));

        modifiedSchema = new(originalSchema) { ReadOnly = false };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.ReadOnly)));

        modifiedSchema = new(originalSchema) { Required = new HashSet<string> { "another required" } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Required)));

        modifiedSchema = new(originalSchema) { Reference = new OpenApiReference { Id = "Another Id", Type = ReferenceType.Schema } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Reference)));

        modifiedSchema = new(originalSchema) { Title = "Another Title" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Title)));

        modifiedSchema = new(originalSchema) { Type = "integer" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Type)));

        modifiedSchema = new(originalSchema) { UniqueItems = true };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.UniqueItems)));

        modifiedSchema = new(originalSchema) { UnresolvedReference = false };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.UnresolvedReference)));

        modifiedSchema = new(originalSchema) { WriteOnly = false };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.WriteOnly)));

        modifiedSchema = new(originalSchema) { Xml = new OpenApiXml { Name = "Another Name" } };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Xml)));

        Assert.Empty(propertyNames);
    }
}
