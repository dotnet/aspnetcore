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

            // Check nullable int property has null in type directly or uses allOf
            var nullableIntProperty = schema.Properties["nullableInt"];
            if (nullableIntProperty.AllOf != null)
            {
                // If still uses allOf, verify structure
                Assert.Equal(2, nullableIntProperty.AllOf.Count);
                Assert.Collection(nullableIntProperty.AllOf,
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

            // Check nullable string property has null in type directly or uses allOf
            var nullableStringProperty = schema.Properties["nullableString"];
            if (nullableStringProperty.AllOf != null)
            {
                // If still uses allOf, verify structure
                Assert.Equal(2, nullableStringProperty.AllOf.Count);
                Assert.Collection(nullableStringProperty.AllOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item => Assert.Equal(JsonSchemaType.String, item.Type));
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableStringProperty.Type?.HasFlag(JsonSchemaType.String));
                Assert.True(nullableStringProperty.Type?.HasFlag(JsonSchemaType.Null));
            }

            // Check nullable bool property has null in type directly or uses allOf
            var nullableBoolProperty = schema.Properties["nullableBool"];
            if (nullableBoolProperty.AllOf != null)
            {
                // If still uses allOf, verify structure
                Assert.Equal(2, nullableBoolProperty.AllOf.Count);
                Assert.Collection(nullableBoolProperty.AllOf,
                    item => Assert.Equal(JsonSchemaType.Null, item.Type),
                    item => Assert.Equal(JsonSchemaType.Boolean, item.Type));
            }
            else
            {
                // If uses direct type, verify null is included
                Assert.True(nullableBoolProperty.Type?.HasFlag(JsonSchemaType.Boolean));
                Assert.True(nullableBoolProperty.Type?.HasFlag(JsonSchemaType.Null));
            }

            // Check nullable DateTime property has null in type directly or uses allOf
            var nullableDateTimeProperty = schema.Properties["nullableDateTime"];
            if (nullableDateTimeProperty.AllOf != null)
            {
                // If still uses allOf, verify structure
                Assert.Equal(2, nullableDateTimeProperty.AllOf.Count);
                Assert.Collection(nullableDateTimeProperty.AllOf,
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

            // Check nullable Guid property has null in type directly or uses allOf
            var nullableGuidProperty = schema.Properties["nullableGuid"];
            if (nullableGuidProperty.AllOf != null)
            {
                // If still uses allOf, verify structure
                Assert.Equal(2, nullableGuidProperty.AllOf.Count);
                Assert.Collection(nullableGuidProperty.AllOf,
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

            // Check nullable Uri property has null in type directly or uses allOf
            var nullableUriProperty = schema.Properties["nullableUri"];
            if (nullableUriProperty.AllOf != null)
            {
                // If still uses allOf, verify structure
                Assert.Equal(2, nullableUriProperty.AllOf.Count);
                Assert.Collection(nullableUriProperty.AllOf,
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
    public async Task GetOpenApiSchema_HandlesNullableComplexTypesInPropertiesWithAllOf()
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

            // Check nullable Todo property uses allOf with reference
            var nullableTodoProperty = schema.Properties["nullableTodo"];
            Assert.NotNull(nullableTodoProperty.AllOf);
            Assert.Equal(2, nullableTodoProperty.AllOf.Count);
            Assert.Collection(nullableTodoProperty.AllOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item => Assert.Equal("Todo", ((OpenApiSchemaReference)item).Reference.Id));

            // Check nullable Account property uses allOf with reference
            var nullableAccountProperty = schema.Properties["nullableAccount"];
            Assert.NotNull(nullableAccountProperty.AllOf);
            Assert.Equal(2, nullableAccountProperty.AllOf.Count);
            Assert.Collection(nullableAccountProperty.AllOf,
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

            // Check nullable List<Todo> property has null in type or uses allOf
            var nullableTodoListProperty = schema.Properties["nullableTodoList"];
            if (nullableTodoListProperty.AllOf != null)
            {
                // If still uses allOf, verify structure
                Assert.Equal(2, nullableTodoListProperty.AllOf.Count);
                Assert.Collection(nullableTodoListProperty.AllOf,
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

            // Check nullable Todo[] property has null in type or uses allOf
            var nullableTodoArrayProperty = schema.Properties["nullableTodoArray"];
            if (nullableTodoArrayProperty.AllOf != null)
            {
                // If still uses allOf, verify structure
                Assert.Equal(2, nullableTodoArrayProperty.AllOf.Count);
                Assert.Collection(nullableTodoArrayProperty.AllOf,
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

            // Check nullable Dictionary<string, Todo> property has null in type or uses allOf
            var nullableDictionaryProperty = schema.Properties["nullableDictionary"];
            if (nullableDictionaryProperty.AllOf != null)
            {
                // If still uses allOf, verify structure
                Assert.Equal(2, nullableDictionaryProperty.AllOf.Count);
                Assert.Collection(nullableDictionaryProperty.AllOf,
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
    public async Task GetOpenApiSchema_HandlesNullableEnumPropertiesWithAllOf()
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

            // Check nullable Status (with string converter) property uses allOf with reference
            var nullableStatusProperty = schema.Properties["nullableStatus"];
            Assert.NotNull(nullableStatusProperty.AllOf);
            Assert.Equal(2, nullableStatusProperty.AllOf.Count);
            Assert.Collection(nullableStatusProperty.AllOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item => Assert.Equal("Status", ((OpenApiSchemaReference)item).Reference.Id));

            // Check nullable TaskStatus (without converter) property uses allOf
            var nullableTaskStatusProperty = schema.Properties["nullableTaskStatus"];
            Assert.NotNull(nullableTaskStatusProperty.AllOf);
            Assert.Equal(2, nullableTaskStatusProperty.AllOf.Count);
            Assert.Collection(nullableTaskStatusProperty.AllOf,
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

            // Check nullable string with validation attributes has null in type or uses allOf
            var nullableNameProperty = schema.Properties["nullableName"];
            if (nullableNameProperty.AllOf != null)
            {
                // If still uses allOf for properties with validation, verify structure
                Assert.Equal(2, nullableNameProperty.AllOf.Count);
                Assert.Collection(nullableNameProperty.AllOf,
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

            // Check nullable int with range validation has null in type or uses allOf
            var nullableAgeProperty = schema.Properties["nullableAge"];
            if (nullableAgeProperty.AllOf != null)
            {
                // If still uses allOf for properties with validation, verify structure
                Assert.Equal(2, nullableAgeProperty.AllOf.Count);
                Assert.Collection(nullableAgeProperty.AllOf,
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

            // Check nullable string with description has null in type or uses allOf
            var nullableDescriptionProperty = schema.Properties["nullableDescription"];
            if (nullableDescriptionProperty.AllOf != null)
            {
                // If still uses allOf for properties with description, verify structure
                Assert.Equal(2, nullableDescriptionProperty.AllOf.Count);
                Assert.Collection(nullableDescriptionProperty.AllOf,
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
#nullable restore
}
