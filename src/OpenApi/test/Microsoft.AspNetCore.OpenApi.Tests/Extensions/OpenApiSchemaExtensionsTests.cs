// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

public class OpenApiSchemaExtensionsTests
{
    [Fact]
    public void ValidateCopyOnAllProperties()
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
            Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Items = new OpenApiSchema() }, },
            ReadOnly = true,
            Required = new HashSet<string> { "required" },
            Reference = new OpenApiReference { Id = "Id", Type = ReferenceType.Schema },
            UniqueItems = false,
            UnresolvedReference = true,
            WriteOnly = true,
            Xml = new OpenApiXml { Name = "Name" },
            Annotations = new Dictionary<string, object> { ["x-schema-id"] = "value" }
        };

        var modifiedSchema = originalSchema.Clone();
        modifiedSchema.AdditionalProperties = new OpenApiSchema { Type = "string" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.AdditionalProperties)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.AdditionalPropertiesAllowed = false;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.AdditionalPropertiesAllowed)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.AllOf = [new OpenApiSchema { Type = "string" }];
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.AllOf)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.AnyOf = [new OpenApiSchema { Type = "string" }];
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.AnyOf)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Deprecated = false;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Deprecated)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Default = new OpenApiString("another default");
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Default)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Description = "another description";
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Description)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Discriminator = new OpenApiDiscriminator { PropertyName = "PropertyName" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Discriminator)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Example = new OpenApiString("another example");
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Example)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.ExclusiveMaximum = false;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.ExclusiveMaximum)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.ExclusiveMinimum = false;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.ExclusiveMinimum)));

        modifiedSchema = originalSchema.Clone();
        originalSchema.Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("another value") };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Extensions)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.ExternalDocs.Description = "another description";
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.ExternalDocs)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Enum = [new OpenApiString("another test")];
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Enum)));

        modifiedSchema = originalSchema.Clone();
        originalSchema.Format = "string";
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Format)));

        modifiedSchema = originalSchema.Clone();
        originalSchema.Type = "string";
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Items)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Maximum = 20;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Maximum)));

        modifiedSchema = originalSchema.Clone();
        originalSchema.MaxItems = 20;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MaxItems)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.MaxLength = 20;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MaxLength)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.MaxProperties = 20;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MaxProperties)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Minimum = 20;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Minimum)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.MinItems = 20;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MinItems)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.MinLength = 20;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MinLength)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.MinProperties = 20;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MinProperties)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.MultipleOf = 20;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.MultipleOf)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.OneOf = [new OpenApiSchema { Type = "string" }];
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.OneOf)));

        modifiedSchema = originalSchema.Clone();
        originalSchema.Not = new OpenApiSchema { Type = "string" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Not)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Nullable = true;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Nullable)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Pattern = "another pattern";
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Pattern)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Properties["name"].Items = new OpenApiSchema { Type = "string" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Properties)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.ReadOnly = false;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.ReadOnly)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Required = new HashSet<string> { "another required" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Required)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Reference = new OpenApiReference { Id = "Another Id", Type = ReferenceType.Schema };
        modifiedSchema.Annotations = new Dictionary<string, object> { ["x-schema-id"] = "another value" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Reference)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Title = "Another Title";
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Title)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Type = "integer";
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Type)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.UniqueItems = true;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.UniqueItems)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.UnresolvedReference = false;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.UnresolvedReference)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.WriteOnly = false;
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.WriteOnly)));

        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Xml = new OpenApiXml { Name = "Another Name" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Xml)));

        // Although annotations are not part of the OpenAPI schema, we care specifically about
        // x-schema-id annotation which is used to identify schemas in the document.
        modifiedSchema = originalSchema.Clone();
        modifiedSchema.Annotations["x-schema-id"] = "another value";
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
        Assert.True(propertyNames.Remove(nameof(OpenApiSchema.Annotations)));

        Assert.Empty(propertyNames);
    }

    [Fact]
    public void ValidateDeepCopyOnNestedSchemas()
    {
        var originalSchema = new OpenApiSchema
        {
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = new OpenApiSchema
                {
                    Items = new OpenApiSchema
                    {
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["nested"] = new OpenApiSchema
                            {
                                AnyOf = [new OpenApiSchema { Type = "string" }]
                            }
                        }
                    }
                }
            }
        };

        var modifiedSchema = originalSchema.Clone();
        modifiedSchema.Properties["name"].Items.Properties["nested"].AnyOf = [new OpenApiSchema { Type = "integer" }];
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
    }

    [Fact]
    public void ValidateDeepCopyOnSchemasWithReference()
    {
        var originalSchema = new OpenApiSchema
        {
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = new OpenApiSchema
                {
                    Reference = new OpenApiReference { Id = "Id", Type = ReferenceType.Schema }
                }
            }
        };

        var modifiedSchema = originalSchema.Clone();
        modifiedSchema.Properties["name"].Reference = new OpenApiReference { Id = "Another Id", Type = ReferenceType.Schema };
        modifiedSchema.Annotations = new Dictionary<string, object> { ["x-schema-id"] = "another value" };
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
    }

    [Fact]
    public void ValidateDeepCopyOnSchemasWithOpenApiAny()
    {
        var originalSchema = new OpenApiSchema
        {
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = new OpenApiSchema
                {
                    Default = new OpenApiString("default"),
                    Example = new OpenApiString("example"),
                    Enum = [new OpenApiString("enum")],
                    ExternalDocs = new OpenApiExternalDocs(),
                    Xml = new OpenApiXml { Name = "Name" }
                }
            }
        };

        var modifiedSchema = originalSchema.Clone();
        modifiedSchema.Properties["name"].Default = new OpenApiString("another default");
        Assert.False(OpenApiSchemaComparer.Instance.Equals(originalSchema, modifiedSchema));
    }
}
