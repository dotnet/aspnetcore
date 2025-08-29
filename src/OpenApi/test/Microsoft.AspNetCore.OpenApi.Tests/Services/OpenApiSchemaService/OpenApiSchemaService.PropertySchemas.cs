// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetOpenApiSchema_HandlesNullablePropertiesWithNullInType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (NullablePropertiesTestModel model) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            var schema = content.Value.Schema;

            Assert.Equal(JsonSchemaType.Object, schema.Type);

            // Check nullable int property has null in type directly or uses oneOf
            var nullableIntProperty = schema.Properties["nullableInt"];
            if (nullableIntProperty.OneOf != null)
            {
                // If still uses oneOf, verify structure
                Assert.Equal(2, nullableIntProperty.OneOf.Count);
                Assert.Collection(nullableIntProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.Integer, item.Type);
                        Assert.Equal("int32", item.Format);
                    });
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableIntProperty.Type?.HasFlag(JsonSchemaType.Integer));
                Assert.True(nullableIntProperty.Type?.HasFlag(JsonSchemaType.Null));
                Assert.Equal("int32", nullableIntProperty.Format);
            }

            // Check nullable string property has null in type directly or uses oneOf
            var nullableStringProperty = schema.Properties["nullableString"];
            if (nullableStringProperty.OneOf != null)
            {
                // If still uses oneOf, verify structure
                Assert.Equal(2, nullableStringProperty.OneOf.Count);
                Assert.Collection(nullableStringProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item => Assert.Equal(JsonSchemaType.String, item.Type));
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableStringProperty.Type?.HasFlag(JsonSchemaType.String));
                Assert.True(nullableStringProperty.Type?.HasFlag(JsonSchemaType.Null));
            }

            // Check nullable bool property has null in type directly or uses oneOf
            var nullableBoolProperty = schema.Properties["nullableBool"];
            if (nullableBoolProperty.OneOf != null)
            {
                // If still uses oneOf, verify structure
                Assert.Equal(2, nullableBoolProperty.OneOf.Count);
                Assert.Collection(nullableBoolProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item => Assert.Equal(JsonSchemaType.Boolean, item.Type));
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableBoolProperty.Type?.HasFlag(JsonSchemaType.Boolean));
                Assert.True(nullableBoolProperty.Type?.HasFlag(JsonSchemaType.Null));
            }

            // Check nullable DateTime property has null in type directly or uses oneOf
            var nullableDateTimeProperty = schema.Properties["nullableDateTime"];
            if (nullableDateTimeProperty.OneOf != null)
            {
                // If still uses oneOf, verify structure
                Assert.Equal(2, nullableDateTimeProperty.OneOf.Count);
                Assert.Collection(nullableDateTimeProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.String, item.Type);
                        Assert.Equal("date-time", item.Format);
                    });
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableDateTimeProperty.Type?.HasFlag(JsonSchemaType.String));
                Assert.True(nullableDateTimeProperty.Type?.HasFlag(JsonSchemaType.Null));
                Assert.Equal("date-time", nullableDateTimeProperty.Format);
            }

            // Check nullable Guid property has null in type directly or uses oneOf
            var nullableGuidProperty = schema.Properties["nullableGuid"];
            if (nullableGuidProperty.OneOf != null)
            {
                // If still uses oneOf, verify structure
                Assert.Equal(2, nullableGuidProperty.OneOf.Count);
                Assert.Collection(nullableGuidProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.String, item.Type);
                        Assert.Equal("uuid", item.Format);
                    });
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableGuidProperty.Type?.HasFlag(JsonSchemaType.String));
                Assert.True(nullableGuidProperty.Type?.HasFlag(JsonSchemaType.Null));
                Assert.Equal("uuid", nullableGuidProperty.Format);
            }

            // Check nullable Uri property has null in type directly or uses oneOf
            var nullableUriProperty = schema.Properties["nullableUri"];
            if (nullableUriProperty.OneOf != null)
            {
                // If still uses oneOf, verify structure
                Assert.Equal(2, nullableUriProperty.OneOf.Count);
                Assert.Collection(nullableUriProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.String, item.Type);
                        Assert.Equal("uri", item.Format);
                    });
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableUriProperty.Type?.HasFlag(JsonSchemaType.String));
                Assert.True(nullableUriProperty.Type?.HasFlag(JsonSchemaType.Null));
                Assert.Equal("uri", nullableUriProperty.Format);
            }
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_HandlesNullableComplexTypesInPropertiesWithOneOf()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (ComplexNullablePropertiesModel model) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            var schema = content.Value.Schema;

            Assert.Equal(JsonSchemaType.Object, schema.Type);

            // Check nullable Todo property uses oneOf with reference
            var nullableTodoProperty = schema.Properties["nullableTodo"];
            Assert.NotNull(nullableTodoProperty.OneOf);
            Assert.Equal(2, nullableTodoProperty.OneOf.Count);
            Assert.Collection(nullableTodoProperty.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item => Assert.Equal("Todo", ((OpenApiSchemaReference)item).Reference.Id));

            // Check nullable Account property uses oneOf with reference
            var nullableAccountProperty = schema.Properties["nullableAccount"];
            Assert.NotNull(nullableAccountProperty.OneOf);
            Assert.Equal(2, nullableAccountProperty.OneOf.Count);
            Assert.Collection(nullableAccountProperty.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item => Assert.Equal("Account", ((OpenApiSchemaReference)item).Reference.Id));

            // Verify component schemas are created
            Assert.Contains("Todo", document.Components.Schemas.Keys);
            Assert.Contains("Account", document.Components.Schemas.Keys);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_HandlesNullableCollectionPropertiesWithNullInType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (NullableCollectionPropertiesModel model) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            var schema = content.Value.Schema;

            Assert.Equal(JsonSchemaType.Object, schema.Type);

            // Check nullable List<Todo> property has null in type or uses oneOf
            var nullableTodoListProperty = schema.Properties["nullableTodoList"];
            if (nullableTodoListProperty.OneOf != null)
            {
                // If still uses oneOf, verify structure
                Assert.Equal(2, nullableTodoListProperty.OneOf.Count);
                Assert.Collection(nullableTodoListProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.Array, item.Type);
                        Assert.NotNull(item.Items);
                        Assert.Equal("Todo", ((OpenApiSchemaReference)item.Items).Reference.Id);
                    });
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableTodoListProperty.Type?.HasFlag(JsonSchemaType.Array));
                Assert.True(nullableTodoListProperty.Type?.HasFlag(JsonSchemaType.Null));
            }

            // Check nullable Todo[] property has null in type or uses oneOf
            var nullableTodoArrayProperty = schema.Properties["nullableTodoArray"];
            if (nullableTodoArrayProperty.OneOf != null)
            {
                // If still uses oneOf, verify structure
                Assert.Equal(2, nullableTodoArrayProperty.OneOf.Count);
                Assert.Collection(nullableTodoArrayProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.Array, item.Type);
                        Assert.NotNull(item.Items);
                        Assert.Equal("Todo", ((OpenApiSchemaReference)item.Items).Reference.Id);
                    });
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableTodoArrayProperty.Type?.HasFlag(JsonSchemaType.Array));
                Assert.True(nullableTodoArrayProperty.Type?.HasFlag(JsonSchemaType.Null));
            }

            // Check nullable Dictionary<string, Todo> property has null in type or uses oneOf
            var nullableDictionaryProperty = schema.Properties["nullableDictionary"];
            if (nullableDictionaryProperty.OneOf != null)
            {
                // If still uses oneOf, verify structure
                Assert.Equal(2, nullableDictionaryProperty.OneOf.Count);
                Assert.Collection(nullableDictionaryProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.Object, item.Type);
                        Assert.NotNull(item.AdditionalProperties);
                        Assert.Equal("Todo", ((OpenApiSchemaReference)item.AdditionalProperties).Reference.Id);
                    });
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableDictionaryProperty.Type?.HasFlag(JsonSchemaType.Object));
                Assert.True(nullableDictionaryProperty.Type?.HasFlag(JsonSchemaType.Null));
            }
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_HandlesNullableEnumPropertiesWithOneOf()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (NullableEnumPropertiesModel model) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            var schema = content.Value.Schema;

            Assert.Equal(JsonSchemaType.Object, schema.Type);

            // Check nullable Status (with string converter) property uses oneOf with reference
            var nullableStatusProperty = schema.Properties["nullableStatus"];
            Assert.NotNull(nullableStatusProperty.OneOf);
            Assert.Equal(2, nullableStatusProperty.OneOf.Count);
            Assert.Collection(nullableStatusProperty.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item => Assert.Equal("Status", ((OpenApiSchemaReference)item).Reference.Id));

            // Check nullable TaskStatus (without converter) property uses oneOf
            var nullableTaskStatusProperty = schema.Properties["nullableTaskStatus"];
            Assert.NotNull(nullableTaskStatusProperty.OneOf);
            Assert.Equal(2, nullableTaskStatusProperty.OneOf.Count);
            Assert.Collection(nullableTaskStatusProperty.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item => Assert.Equal(JsonSchemaType.Integer, item.Type));
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_HandlesNullablePropertiesWithValidationAttributesAndNullInType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (NullablePropertiesWithValidationModel model) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            var schema = content.Value.Schema;

            Assert.Equal(JsonSchemaType.Object, schema.Type);

            // Check nullable string with validation attributes has null in type or uses oneOf
            var nullableNameProperty = schema.Properties["nullableName"];
            if (nullableNameProperty.OneOf != null)
            {
                // If still uses oneOf for properties with validation, verify structure
                Assert.Equal(2, nullableNameProperty.OneOf.Count);
                Assert.Collection(nullableNameProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.String, item.Type);
                        Assert.Equal(3, item.MinLength);
                        Assert.Equal(50, item.MaxLength);
                    });
            }
            else
            {
                // If uses direct type, verify null is included and validation attributes
                Assert.True(nullableNameProperty.Type?.HasFlag(JsonSchemaType.String));
                Assert.True(nullableNameProperty.Type?.HasFlag(JsonSchemaType.Null));
                Assert.Equal(3, nullableNameProperty.MinLength);
                Assert.Equal(50, nullableNameProperty.MaxLength);
            }

            // Check nullable int with range validation has null in type or uses oneOf
            var nullableAgeProperty = schema.Properties["nullableAge"];
            if (nullableAgeProperty.OneOf != null)
            {
                // If still uses oneOf for properties with validation, verify structure
                Assert.Equal(2, nullableAgeProperty.OneOf.Count);
                Assert.Collection(nullableAgeProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.Integer, item.Type);
                        Assert.Equal("int32", item.Format);
                        Assert.Equal("18", item.Minimum);
                        Assert.Equal("120", item.Maximum);
                    });
            }
            else
            {
                // If uses direct type, verify null is included and validation attributes
                Assert.True(nullableAgeProperty.Type?.HasFlag(JsonSchemaType.Integer));
                Assert.True(nullableAgeProperty.Type?.HasFlag(JsonSchemaType.Null));
                Assert.Equal("int32", nullableAgeProperty.Format);
                Assert.Equal("18", nullableAgeProperty.Minimum);
                Assert.Equal("120", nullableAgeProperty.Maximum);
            }

            // Check nullable string with description has null in type or uses oneOf
            var nullableDescriptionProperty = schema.Properties["nullableDescription"];
            if (nullableDescriptionProperty.OneOf != null)
            {
                // If still uses oneOf for properties with description, verify structure
                Assert.Equal(2, nullableDescriptionProperty.OneOf.Count);
                Assert.Collection(nullableDescriptionProperty.OneOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item =>
                    {
                        Assert.Equal(JsonSchemaType.String, item.Type);
                        Assert.Equal("A description field", item.Description);
                    });
            }
            else
            {
                // If uses direct type, verify null is included and description
                Assert.True(nullableDescriptionProperty.Type?.HasFlag(JsonSchemaType.String));
                Assert.True(nullableDescriptionProperty.Type?.HasFlag(JsonSchemaType.Null));
                Assert.Equal("A description field", nullableDescriptionProperty.Description);
            }
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_Base64StringAttribute_StringProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["base64StringValue"];
            var nullable = schema.Properties["nullableBase64StringValue"];
            Assert.Equal(JsonSchemaType.String, nonNullable.Type);
            Assert.Equal("byte", nonNullable.Format);
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, nullable.Type);
            Assert.Equal("byte", nullable.Format);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_RangeAttribute_IntProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["rangeIntValue"];
            var nullable = schema.Properties["nullableRangeIntValue"];
            Assert.Equal(JsonSchemaType.Integer, nonNullable.Type);
            Assert.Equal("int32", nonNullable.Format);
            Assert.Equal("1", nonNullable.Minimum);
            Assert.Equal("100", nonNullable.Maximum);
            Assert.Equal(JsonSchemaType.Integer | JsonSchemaType.Null, nullable.Type);
            Assert.Equal("int32", nullable.Format);
            Assert.Equal("1", nullable.Minimum);
            Assert.Equal("100", nullable.Maximum);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_RangeAttribute_DoubleProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["rangeDoubleValue"];
            var nullable = schema.Properties["nullableRangeDoubleValue"];
            Assert.Equal(JsonSchemaType.Number, nonNullable.Type);
            Assert.Equal("double", nonNullable.Format);
            Assert.Equal("0.1", nonNullable.Minimum as string);
            Assert.Equal("99.9", nonNullable.Maximum as string);
            Assert.Equal(JsonSchemaType.Number | JsonSchemaType.Null, nullable.Type);
            Assert.Equal("double", nullable.Format);
            Assert.Equal("0.1", nullable.Minimum as string);
            Assert.Equal("99.9", nullable.Maximum as string);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_RegularExpressionAttribute_StringProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["regexStringValue"];
            var nullable = schema.Properties["nullableRegexStringValue"];
            Assert.Equal(JsonSchemaType.String, nonNullable.Type);
            Assert.Equal("^[A-Z]{3}$", nonNullable.Pattern);
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, nullable.Type);
            Assert.Equal("^[A-Z]{3}$", nullable.Pattern);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_MaxLengthAttribute_StringProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["maxLengthStringValue"];
            var nullable = schema.Properties["nullableMaxLengthStringValue"];
            Assert.Equal(JsonSchemaType.String, nonNullable.Type);
            Assert.Equal(10, nonNullable.MaxLength);
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, nullable.Type);
            Assert.Equal(10, nullable.MaxLength);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_MaxLengthAttribute_ArrayProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["maxLengthArrayValue"];
            var nullable = schema.Properties["nullableMaxLengthArrayValue"];
            Assert.Equal(JsonSchemaType.Array, nonNullable.Type);
            Assert.Equal(5, nonNullable.MaxItems);
            Assert.Equal(JsonSchemaType.Array | JsonSchemaType.Null, nullable.Type);
            Assert.Equal(5, nullable.MaxItems);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_MinLengthAttribute_StringProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["minLengthStringValue"];
            var nullable = schema.Properties["nullableMinLengthStringValue"];
            Assert.Equal(JsonSchemaType.String, nonNullable.Type);
            Assert.Equal(3, nonNullable.MinLength);
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, nullable.Type);
            Assert.Equal(3, nullable.MinLength);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_MinLengthAttribute_ArrayProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["minLengthArrayValue"];
            var nullable = schema.Properties["nullableMinLengthArrayValue"];
            Assert.Equal(JsonSchemaType.Array, nonNullable.Type);
            Assert.Equal(2, nonNullable.MinItems);
            Assert.Equal(JsonSchemaType.Array | JsonSchemaType.Null, nullable.Type);
            Assert.Equal(2, nullable.MinItems);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_LengthAttribute_StringProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["lengthStringValue"];
            var nullable = schema.Properties["nullableLengthStringValue"];
            Assert.Equal(JsonSchemaType.String, nonNullable.Type);
            Assert.Equal(2, nonNullable.MinLength);
            Assert.Equal(8, nonNullable.MaxLength);
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, nullable.Type);
            Assert.Equal(2, nullable.MinLength);
            Assert.Equal(8, nullable.MaxLength);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_LengthAttribute_ArrayProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["lengthArrayValue"];
            var nullable = schema.Properties["nullableLengthArrayValue"];
            Assert.Equal(JsonSchemaType.Array, nonNullable.Type);
            Assert.Equal(1, nonNullable.MinItems);
            Assert.Equal(4, nonNullable.MaxItems);
            Assert.Equal(JsonSchemaType.Array | JsonSchemaType.Null, nullable.Type);
            Assert.Equal(1, nullable.MinItems);
            Assert.Equal(4, nullable.MaxItems);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_UrlAttribute_StringProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["urlStringValue"];
            var nullable = schema.Properties["nullableUrlStringValue"];
            Assert.Equal(JsonSchemaType.String, nonNullable.Type);
            Assert.Equal("uri", nonNullable.Format);
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, nullable.Type);
            Assert.Equal("uri", nullable.Format);
        });
    }

    [Fact]
    public async Task GetOpenApiSchema_StringLengthAttribute_StringProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (PropertiesWithDataAnnotations model) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content.First().Value.Schema;
            var nonNullable = schema.Properties["stringLengthValue"];
            var nullable = schema.Properties["nullableStringLengthValue"];
            Assert.Equal(JsonSchemaType.String, nonNullable.Type);
            Assert.Equal(5, nonNullable.MinLength);
            Assert.Equal(20, nonNullable.MaxLength);
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, nullable.Type);
            Assert.Equal(5, nullable.MinLength);
            Assert.Equal(20, nullable.MaxLength);
        });
    }

#nullable enable
    private class NullablePropertiesTestModel
    {
        public int? NullableInt { get; set; }
        public string? NullableString { get; set; }
        public bool? NullableBool { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public Guid? NullableGuid { get; set; }
        public Uri? NullableUri { get; set; }
    }

    private class ComplexNullablePropertiesModel
    {
        public Todo? NullableTodo { get; set; }
        public Account? NullableAccount { get; set; }
    }

    private class NullableCollectionPropertiesModel
    {
        public List<Todo>? NullableTodoList { get; set; }
        public Todo[]? NullableTodoArray { get; set; }
        public Dictionary<string, Todo>? NullableDictionary { get; set; }
    }

    private class NullableEnumPropertiesModel
    {
        public Status? NullableStatus { get; set; }
        public TaskStatus? NullableTaskStatus { get; set; }
    }

    private class NullablePropertiesWithValidationModel
    {
        [StringLength(50, MinimumLength = 3)]
        public string? NullableName { get; set; }

        [Range(18, 120)]
        public int? NullableAge { get; set; }

        [Description("A description field")]
        public string? NullableDescription { get; set; }
    }

    private class PropertiesWithDataAnnotations
    {
        // Base64StringAttribute
        [Base64String]
        public string Base64StringValue { get; set; } = string.Empty;
        [Base64String]
        public string? NullableBase64StringValue { get; set; }

        // RangeAttribute
        [Range(1, 100)]
        public int RangeIntValue { get; set; } = 0;
        [Range(1, 100)]
        public int? NullableRangeIntValue { get; set; }
        [Range(0.1, 99.9)]
        public double RangeDoubleValue { get; set; } = 0.0;
        [Range(0.1, 99.9)]
        public double? NullableRangeDoubleValue { get; set; }

        // RegularExpressionAttribute
        [RegularExpression(@"^[A-Z]{3}$")]
        public string RegexStringValue { get; set; } = string.Empty;
        [RegularExpression(@"^[A-Z]{3}$")]
        public string? NullableRegexStringValue { get; set; }

        // MaxLengthAttribute
        [MaxLength(10)]
        public string MaxLengthStringValue { get; set; } = string.Empty;
        [MaxLength(10)]
        public string? NullableMaxLengthStringValue { get; set; }
        [MaxLength(5)]
        public int[] MaxLengthArrayValue { get; set; } = [];
        [MaxLength(5)]
        public int[]? NullableMaxLengthArrayValue { get; set; }

        // MinLengthAttribute
        [MinLength(3)]
        public string MinLengthStringValue { get; set; } = string.Empty;
        [MinLength(3)]
        public string? NullableMinLengthStringValue { get; set; }
        [MinLength(2)]
        public int[] MinLengthArrayValue { get; set; } = [];
        [MinLength(2)]
        public int[]? NullableMinLengthArrayValue { get; set; }

        // LengthAttribute (custom, if available)
        [Length(2, 8)]
        public string LengthStringValue { get; set; } = string.Empty;
        [Length(2, 8)]
        public string? NullableLengthStringValue { get; set; }
        [Length(1, 4)]
        public int[] LengthArrayValue { get; set; } = [];
        [Length(1, 4)]
        public int[]? NullableLengthArrayValue { get; set; }

        // UrlAttribute
        [Url]
        public string UrlStringValue { get; set; } = string.Empty;
        [Url]
        public string? NullableUrlStringValue { get; set; }

        // StringLengthAttribute
        [StringLength(20, MinimumLength = 5)]
        public string StringLengthValue { get; set; } = string.Empty;
        [StringLength(20, MinimumLength = 5)]
        public string? NullableStringLengthValue { get; set; }
    }
#nullable restore
}
