// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.IO.Pipelines;
using System.Net.Http;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public partial class OpenApiDocumentServiceTests : OpenApiDocumentServiceTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetRequestBody_HandlesIFormFile(bool withAttribute)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (withAttribute)
        {
            builder.MapPost("/", ([FromForm] IFormFile formFile) => { });
        }
        else
        {
            builder.MapPost("/", (IFormFile formFile) => { });
        }

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.True(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("multipart/form-data", content.Key);
            Assert.Equal(JsonSchemaType.Object, content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.Properties);
            Assert.Contains("formFile", content.Value.Schema.Properties);
            var formFileProperty = content.Value.Schema.Properties["formFile"];
            Assert.Equal(JsonSchemaType.String, formFileProperty.Type);
            Assert.Equal("binary", formFileProperty.Format);
        });
    }

#nullable enable
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetRequestBody_HandlesIFormFileOptionality(bool isOptional)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (isOptional)
        {
            builder.MapPost("/", (IFormFile? formFile) => { });
        }
        else
        {
            builder.MapPost("/", (IFormFile formFile) => { });
        }

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations![HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.True(operation.RequestBody.Required);
            var schema = operation.RequestBody.Content!["multipart/form-data"].Schema;
            Assert.NotNull(schema);
            if (!isOptional)
            {
                Assert.Contains("formFile", schema.Required!);
            }
            else
            {
                Assert.Null(schema.Required);
            }
        });
    }
#nullable restore

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetRequestBody_HandlesIFormFileCollection(bool withAttribute)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (withAttribute)
        {
            builder.MapPost("/", ([FromForm] IFormFileCollection formFileCollection) => { });
        }
        else
        {
            builder.MapPost("/", (IFormFileCollection formFileCollection) => { });
        }

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.True(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("multipart/form-data", content.Key);
            Assert.Equal(JsonSchemaType.Object, content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.Properties);
            Assert.Contains("formFileCollection", content.Value.Schema.Properties);
            var formFileProperty = content.Value.Schema.Properties["formFileCollection"];
            Assert.Equal(JsonSchemaType.Array, formFileProperty.Type);
            Assert.Equal(JsonSchemaType.String, formFileProperty.Items.Type);
            Assert.Equal("binary", formFileProperty.Items.Format);
        });
    }

#nullable enable
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetRequestBody_HandlesIFormFileCollectionOptionality(bool isOptional)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (isOptional)
        {
            builder.MapPost("/", (IFormFileCollection? formFile) => { });
        }
        else
        {
            builder.MapPost("/", (IFormFileCollection formFile) => { });
        }

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations![HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.True(operation.RequestBody.Required);
            var schema = operation.RequestBody.Content!["multipart/form-data"].Schema;
            Assert.NotNull(schema);
            if (!isOptional)
            {
                Assert.Contains("formFile", schema.Required!);
            }
            else
            {
                Assert.Null(schema.Required);
            }
        });
    }
#nullable restore

    [Fact]
    public async Task GetRequestBody_MultipleFormFileParameters()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (IFormFile formFile1, IFormFile formFile2) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("multipart/form-data", content.Key);
            Assert.Equal(JsonSchemaType.Object, content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.AllOf);
            Assert.Collection(content.Value.Schema.AllOf,
                allOfItem =>
                {
                    Assert.NotNull(allOfItem.Properties);
                    Assert.Contains("formFile1", allOfItem.Properties);
                    var formFile1Property = allOfItem.Properties["formFile1"];
                    Assert.Equal(JsonSchemaType.String, formFile1Property.Type);
                    Assert.Equal("binary", formFile1Property.Format);
                },
                allOfItem =>
                {
                    Assert.NotNull(allOfItem.Properties);
                    Assert.Contains("formFile2", allOfItem.Properties);
                    var formFile2Property = allOfItem.Properties["formFile2"];
                    Assert.Equal(JsonSchemaType.String, formFile2Property.Type);
                    Assert.Equal("binary", formFile2Property.Format);
                });
        });
    }

    [Fact]
    public async Task GetRequestBody_IFormFileHandlesAcceptsMetadata()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (IFormFile formFile) => { }).Accepts(typeof(IFormFile), "application/magic-foo-content-type");

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/magic-foo-content-type", content.Key);
            Assert.Equal(JsonSchemaType.Object, content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.Properties);
            Assert.Contains("formFile", content.Value.Schema.Properties);
            var formFileProperty = content.Value.Schema.Properties["formFile"];
            Assert.Equal(JsonSchemaType.String, formFileProperty.Type);
            Assert.Equal("binary", formFileProperty.Format);
        });
    }

    [Fact]
    public async Task GetRequestBody_IFormFileHandlesConsumesAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", [Consumes(typeof(IFormFile), "application/magic-foo-content-type")] (IFormFile formFile) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/magic-foo-content-type", content.Key);
            Assert.Equal(JsonSchemaType.Object, content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.Properties);
            Assert.Contains("formFile", content.Value.Schema.Properties);
            var formFileProperty = content.Value.Schema.Properties["formFile"];
            Assert.Equal(JsonSchemaType.String, formFileProperty.Type);
            Assert.Equal("binary", formFileProperty.Format);
        });
    }

    [Fact]
    public async Task GetRequestBody_HandlesJsonBody()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (TodoWithDueDate name) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json", content.Key);
        });
    }

#nullable enable
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetRequestBody_HandlesJsonBodyOptionality(bool isOptional)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (isOptional)
        {
            builder.MapPost("/", (TodoWithDueDate? name) => { });
        }
        else
        {
            builder.MapPost("/", (TodoWithDueDate name) => { });
        }

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations![HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.Equal(!isOptional, operation.RequestBody.Required);
        });

    }
#nullable restore

    [Fact]
    public async Task GetRequestBody_HandlesJsonBodyWithAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", ([FromBody] string name) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json", content.Key);
        });
    }

    [Fact]
    public async Task GetRequestBody_HandlesJsonBodyWithAcceptsMetadata()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (string name) => { }).Accepts(typeof(string), "application/magic-foo-content-type");

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/magic-foo-content-type", content.Key);
        });
    }

    [Fact]
    public async Task GetRequestBody_HandlesJsonBodyWithConsumesAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", [Consumes(typeof(string), "application/magic-foo-content-type")] (string name) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/magic-foo-content-type", content.Key);
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_SetsNullRequestBodyWithNoParameters()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (string name) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.Null(operation.RequestBody);
        });
    }

    // Test coverage for https://github.com/dotnet/aspnetcore/issues/52284
    [Fact]
    public async Task GetOpenApiRequestBody_HandlesFromFormWithPoco()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/form", ([FromForm] Todo todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            Assert.Contains("multipart/form-data", content.Keys);
            // Same schema should be produced for both content-types
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.NotNull(item.Schema.Properties);
                Assert.Contains("id", item.Schema.Required);
                Assert.Contains("title", item.Schema.Required);
                Assert.Contains("completed", item.Schema.Required);
                Assert.Contains("createdAt", item.Schema.Required);
                Assert.Collection(item.Schema.Properties,
                    property =>
                    {
                        Assert.Equal("id", property.Key);
                        Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("title", property.Key);
                        Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("completed", property.Key);
                        Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("createdAt", property.Key);
                        Assert.Equal(JsonSchemaType.String, property.Value.Type);
                        Assert.Equal("date-time", property.Value.Format);
                    });
            }
        });
    }

    // Test coverage for https://github.com/dotnet/aspnetcore/issues/57112
    [Fact]
    public async Task GetOpenApiRequestBody_HandlesFromFormWithRequiredPrimitive()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/form", ([FromForm] int id, [FromForm] DateTime date, [FromForm] short? value) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            // Same schema should be produced for both content-types
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.NotNull(item.Schema.Properties);
                // Assert that requiredness has been set for primitives
                Assert.Contains("id", item.Schema.Required);
                Assert.Contains("date", item.Schema.Required);
                Assert.DoesNotContain("value", item.Schema.Required);
                Assert.Collection(item.Schema.AllOf,
                    subSchema =>
                    {
                        Assert.Contains("id", subSchema.Properties);
                        Assert.Equal(JsonSchemaType.Integer, subSchema.Properties["id"].Type);
                    },
                    subSchema =>
                    {
                        Assert.Contains("date", subSchema.Properties);
                        Assert.Equal(JsonSchemaType.String, subSchema.Properties["date"].Type);
                        Assert.Equal("date-time", subSchema.Properties["date"].Format);
                    },
                    subSchema =>
                    {
                        Assert.Contains("value", subSchema.Properties);
                        Assert.Equal(JsonSchemaType.Integer, subSchema.Properties["value"].Type);
                    });
            }
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesFromFormWithPoco_MvcAction()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ActionWithFormModel));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            // Same schema should be produced for both content-types
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.NotNull(item.Schema.Properties);
                Assert.Collection(item.Schema.Properties,
                    property =>
                    {
                        Assert.Equal("Id", property.Key);
                        Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("Title", property.Key);
                        Assert.Equal(JsonSchemaType.String, property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("Completed", property.Key);
                        Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("CreatedAt", property.Key);
                        Assert.Equal(JsonSchemaType.String, property.Value.Type);
                        Assert.Equal("date-time", property.Value.Format);
                    });
            }
        });
    }

    [Route("/form-model")]
    private void ActionWithFormModel([FromForm] Todo todo) { }

    // Test coverage for https://github.com/dotnet/aspnetcore/issues/53831
    [Fact]
    public async Task GetOpenApiRequestBody_HandlesMultipleFormWithPoco()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/form", ([FromForm] Todo todo, [FromForm] Error error) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            Assert.Contains("multipart/form-data", content.Keys);
            // Same schema should be produced for both content-types
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.NotNull(item.Schema.AllOf);
                Assert.Collection(item.Schema.AllOf,
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                            {
                                Assert.Equal("id", property.Key);
                                Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("title", property.Key);
                                Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("completed", property.Key);
                                Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("createdAt", property.Key);
                                Assert.Equal(JsonSchemaType.String, property.Value.Type);
                                Assert.Equal("date-time", property.Value.Format);
                            });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties,
                            property =>
                            {
                                Assert.Equal("code", property.Key);
                                Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("message", property.Key);
                                Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                            });
                    });
            }
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesMultipleFormWithPoco_MvcAction()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ActionWithMultipleFormModel));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            // Same schema should be produced for both content-types
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.NotNull(item.Schema.AllOf);
                Assert.Collection(item.Schema.AllOf,
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                            {
                                Assert.Equal("Id", property.Key);
                                Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("Title", property.Key);
                                Assert.Equal(JsonSchemaType.String, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("Completed", property.Key);
                                Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("CreatedAt", property.Key);
                                Assert.Equal(JsonSchemaType.String, property.Value.Type);
                                Assert.Equal("date-time", property.Value.Format);
                            });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties,
                            property =>
                            {
                                Assert.Equal("Code", property.Key);
                                Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("Message", property.Key);
                                Assert.Equal(JsonSchemaType.String, property.Value.Type);
                            });
                    });
            }
        });
    }

    [Route("/form-model")]
    private void ActionWithMultipleFormModel([FromForm] Todo todo, [FromForm] Error error) { }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesFromFormWithPocoSingleProp_MvcAction()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ActionWithFormModelSingleProp));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            // Same schema should be produced for both content-types
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.NotNull(item.Schema.Properties);
                Assert.Collection(item.Schema.Properties,
                    property =>
                    {
                        Assert.Equal("Name", property.Key);
                        Assert.Equal(JsonSchemaType.String, property.Value.Type);
                    });
            }
        });
    }

    [Route("/form-model-single-prop")]
    private void ActionWithFormModelSingleProp([FromForm] ModelWithSingleProperty model) { }

    [Theory]
    [InlineData(false, nameof(FormModelWithRequiredProperties.BindingRequired))]
    [InlineData(false, nameof(FormModelWithRequiredProperties.ValidationRequired))]
    [InlineData(true, nameof(FormModelWithRequiredProperties.BindingRequired))]
    [InlineData(true, nameof(FormModelWithRequiredProperties.ValidationRequired))]
    [InlineData(false, "upload")]
    [InlineData(true, "upload")]
    public async Task GetOpenApiRequestBody_HandlesFromFormWithRequiredProperties_MvcAction(bool withAdditionalParameter, string requiredProperty)
    {
        var action = CreateActionDescriptor(withAdditionalParameter
            ? nameof(ActionWithRequiredFormModelAndAdditionalParameter)
            : nameof(ActionWithRequiredFormModel));

        await VerifyOpenApiDocument(action, document =>
        {
            var operation = Assert.Single(document.Paths.Values).Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.True(operation.RequestBody.Required);
            var schema = operation.RequestBody.Content["application/x-www-form-urlencoded"].Schema;
            Assert.NotNull(schema);
            var modelSchema = withAdditionalParameter
                ? Assert.Single(schema.AllOf, item => item.Properties.ContainsKey(nameof(FormModelWithRequiredProperties.BindingRequired)))
                : schema;

            Assert.Equal(JsonSchemaType.Object, modelSchema.Type);
            Assert.Equal(7, modelSchema.Properties.Count);
            Assert.Contains("upload", modelSchema.Properties.Keys);
            Assert.DoesNotContain(nameof(FormModelWithRequiredProperties.File), modelSchema.Properties.Keys);
            Assert.Contains(nameof(FormModelWithRequiredProperties.BindingRequired), modelSchema.Properties.Keys);
            Assert.Contains(nameof(FormModelWithRequiredProperties.ValidationRequired), modelSchema.Properties.Keys);
            Assert.Contains(nameof(FormModelWithRequiredProperties.OptionalText), modelSchema.Properties.Keys);
            Assert.Contains(nameof(FormModelWithRequiredProperties.OptionalNumber), modelSchema.Properties.Keys);
            Assert.DoesNotContain(nameof(FormModelWithRequiredProperties.OptionalText), modelSchema.Required ?? new HashSet<string>());
            Assert.DoesNotContain(nameof(FormModelWithRequiredProperties.OptionalNumber), modelSchema.Required ?? new HashSet<string>());
            Assert.DoesNotContain(nameof(FormModelWithRequiredProperties.OptionalCount), modelSchema.Required ?? new HashSet<string>());
            Assert.DoesNotContain(nameof(FormModelWithRequiredProperties.OptionalFlag), modelSchema.Required ?? new HashSet<string>());
            Assert.Contains(requiredProperty, modelSchema.Required ?? new HashSet<string>());
            Assert.Equal(new[] { "BindingRequired", "ValidationRequired", "upload" }, modelSchema.Required.Order(StringComparer.Ordinal));
            Assert.Same(schema, operation.RequestBody.Content["multipart/form-data"].Schema);
        });
    }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/required-form-model")]
    private void ActionWithRequiredFormModel([FromForm, Required] FormModelWithRequiredProperties model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/required-form-model-additional")]
    private void ActionWithRequiredFormModelAndAdditionalParameter([FromForm, Required] FormModelWithRequiredProperties model, [FromForm] string additional) { }

#nullable enable
    private class FormModelWithRequiredProperties
    {
        [BindRequired]
        public string? BindingRequired { get; set; }

        [Required]
        public string? ValidationRequired { get; set; }

        [FromForm(Name = "upload"), Required]
        public IFormFile? File { get; set; }

        public string? OptionalText { get; set; }

        public int? OptionalNumber { get; set; }

        public int OptionalCount { get; set; }

        public bool OptionalFlag { get; set; }
    }
#nullable restore

    [Theory]
    [InlineData(nameof(ActionWithConditionalFormModel), "Count", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithConditionalFormModel), "Count", "multipart/form-data")]
    [InlineData(nameof(ActionWithConditionalFormModel), "Child.Name", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithConditionalFormModel), "Child.Name", "multipart/form-data")]
    [InlineData(nameof(ActionWithConditionalFormModel), "Child.Bound", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithConditionalFormModel), "Child.Bound", "multipart/form-data")]
    [InlineData(nameof(ActionWithConditionalFormModelAndAdditionalParameter), "Count", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithConditionalFormModelAndAdditionalParameter), "Count", "multipart/form-data")]
    [InlineData(nameof(ActionWithConditionalFormModelAndAdditionalParameter), "Child.Name", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithConditionalFormModelAndAdditionalParameter), "Child.Name", "multipart/form-data")]
    [InlineData(nameof(ActionWithConditionalFormModelAndAdditionalParameter), "Child.Bound", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithConditionalFormModelAndAdditionalParameter), "Child.Bound", "multipart/form-data")]
    [InlineData(nameof(ActionWithPrefixedConditionalFormModel), "input.Count", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithPrefixedConditionalFormModel), "input.Count", "multipart/form-data")]
    [InlineData(nameof(ActionWithPrefixedConditionalFormModel), "input.Child.Name", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithPrefixedConditionalFormModel), "input.Child.Name", "multipart/form-data")]
    [InlineData(nameof(ActionWithPrefixedConditionalFormModel), "input.Child.Bound", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithPrefixedConditionalFormModel), "input.Child.Bound", "multipart/form-data")]
    [InlineData(nameof(ActionWithRecursiveFormModel), "Child.Name", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithRecursiveFormModel), "Child.Name", "multipart/form-data")]
    [InlineData(nameof(ActionWithRequiredParentFormModel), "Child.Name", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithRequiredParentFormModel), "Child.Name", "multipart/form-data")]
    [InlineData(nameof(ActionWithRequiredParentFormModel), "Child.Bound", "application/x-www-form-urlencoded")]
    [InlineData(nameof(ActionWithRequiredParentFormModel), "Child.Bound", "multipart/form-data")]
    public async Task GetOpenApiRequestBody_DoesNotRequireConditionalFormProperties_MvcAction(string actionName, string optionalProperty, string contentType)
    {
        var action = CreateActionDescriptor(actionName);
        await VerifyOpenApiDocument(action, document =>
        {
            var operation = Assert.Single(document.Paths.Values).Operations[HttpMethod.Post];
            var prefix = actionName == nameof(ActionWithPrefixedConditionalFormModel) ? "input." : "";
            var schema = operation.RequestBody.Content[contentType].Schema;
            var modelSchema = actionName == nameof(ActionWithConditionalFormModelAndAdditionalParameter)
                ? Assert.Single(schema.AllOf, item => item.Properties.ContainsKey("Count"))
                : schema;
            Assert.Contains(optionalProperty, modelSchema.Properties.Keys);
            Assert.DoesNotContain(optionalProperty, modelSchema.Required ?? new HashSet<string>());
            var expected = actionName == nameof(ActionWithRequiredParentFormModel)
                ? Array.Empty<string>()
                : actionName == nameof(ActionWithRecursiveFormModel)
                    ? new[] { "Name" }
                    : new[] { prefix + "BoundCount", prefix + "NullableCount", prefix + "direct.name" };
            Assert.Equal(expected, (modelSchema.Required ?? new HashSet<string>()).Order(StringComparer.Ordinal));
        });
    }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/conditional-form")]
    private void ActionWithConditionalFormModel([FromForm] ConditionalFormModel model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/conditional-form-additional")]
    private void ActionWithConditionalFormModelAndAdditionalParameter([FromForm] ConditionalFormModel model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/conditional-form-prefixed")]
    private void ActionWithPrefixedConditionalFormModel([FromForm(Name = "input")] ConditionalFormModel model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/conditional-form-parent")]
    private void ActionWithRequiredParentFormModel([FromForm] RequiredParentFormModel model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/conditional-form-recursive")]
    private void ActionWithRecursiveFormModel([FromForm] RecursiveFormModel model) { }

#nullable enable
    private class RecursiveFormModel
    {
        public IFormFile? File { get; set; }

        [Required]
        public string? Name { get; set; }

        public RecursiveFormModel? Child { get; set; }
    }

    private class ConditionalFormModel
    {
        public IFormFile? File { get; set; }

        [Required]
        public int Count { get; set; }

        [Required]
        public int? NullableCount { get; set; }

        [BindRequired]
        public int BoundCount { get; set; }

        [FromForm(Name = "direct.name"), Required]
        public string? Name { get; set; }

        public ConditionalFormChild? Child { get; set; }
    }

    private class ConditionalFormChild
    {
        [Required]
        public string? Name { get; set; }

        [BindRequired]
        public string? Bound { get; set; }

        public string? Note { get; set; }
    }

    private class RequiredParentFormModel
    {
        public IFormFile? File { get; set; }

        [Required]
        public ConditionalFormChild? Child { get; set; }
    }
#nullable restore

    [Theory]
    [InlineData(nameof(ActionWithSuppressedValidation), false)]
    [InlineData(nameof(ActionWithSuppressedValidation), true)]
    [InlineData(nameof(ActionWithSuppressedContainer), false)]
    [InlineData(nameof(ActionWithExcludedBinding), false)]
    [InlineData(nameof(ActionWithExcludedBinding), true)]
    [InlineData(nameof(ActionWithReadOnlyBinding), false)]
    [InlineData(nameof(ActionWithReadOnlyBinding), true)]
    [InlineData(nameof(ActionWithTypeExcludedBinding), false)]
    [InlineData(nameof(ActionWithConditionalValidationFilter), false)]
    public async Task GetOpenApiRequestBody_DoesNotRequireSuppressedFormProperties_MvcAction(string actionName, bool withAdditionalParameter)
    {
        await VerifyEffectiveFormRequirements(actionName, withAdditionalParameter, expected: "");
    }

    [Theory]
    [InlineData(nameof(ActionWithIncludedBinding), "Bound")]
    [InlineData(nameof(ActionWithBindingAndSuppressedValidation), "Bound")]
    [InlineData(nameof(ActionWithMutableReadOnlyBinding), "Values")]
    [InlineData(nameof(ActionWithParameterValidationFilter), "Name")]
    public async Task GetOpenApiRequestBody_PreservesEffectiveFormRequirements_MvcAction(string actionName, string expected)
    {
        await VerifyEffectiveFormRequirements(actionName, withAdditionalParameter: false, expected);
    }

    [Theory]
    [InlineData(false, false, false, "application/x-www-form-urlencoded")]
    [InlineData(false, false, false, "multipart/form-data")]
    [InlineData(false, false, true, "application/x-www-form-urlencoded")]
    [InlineData(false, false, true, "multipart/form-data")]
    [InlineData(false, true, false, "application/x-www-form-urlencoded")]
    [InlineData(false, true, false, "multipart/form-data")]
    [InlineData(false, true, true, "application/x-www-form-urlencoded")]
    [InlineData(false, true, true, "multipart/form-data")]
    [InlineData(true, false, false, "application/x-www-form-urlencoded")]
    [InlineData(true, false, false, "multipart/form-data")]
    [InlineData(true, false, true, "application/x-www-form-urlencoded")]
    [InlineData(true, false, true, "multipart/form-data")]
    [InlineData(true, true, false, "application/x-www-form-urlencoded")]
    [InlineData(true, true, false, "multipart/form-data")]
    [InlineData(true, true, true, "application/x-www-form-urlencoded")]
    [InlineData(true, true, true, "multipart/form-data")]
    public async Task GetOpenApiRequestBody_RespectsSuppressChildValidationMetadataProvider_MvcAction(
        bool suppressChildren, bool bindingRequired, bool withAdditionalParameter, string contentType)
    {
        var action = CreateActionDescriptor(bindingRequired
            ? nameof(ActionWithChildValidationAndBinding)
            : nameof(ActionWithChildValidation));
        if (!withAdditionalParameter)
        {
            action.Parameters = action.Parameters.Take(1).ToArray();
        }
        await VerifyOpenApiDocument(action, document =>
        {
            var operation = Assert.Single(document.Paths.Values).Operations[HttpMethod.Post];
            var schema = operation.RequestBody.Content[contentType].Schema;
            var modelSchema = withAdditionalParameter
                ? Assert.Single(schema.AllOf, item => item.Properties.ContainsKey("File"))
                : schema;
            Assert.Equal(action.Parameters[0].ParameterType.GetProperties().Select(property => property.Name).Order(StringComparer.Ordinal),
                modelSchema.Properties.Keys.Order(StringComparer.Ordinal));
            if (suppressChildren)
            {
                Assert.DoesNotContain("Name", modelSchema.Required ?? new HashSet<string>());
            }
            var expected = bindingRequired ? new List<string> { "Bound" } : [];
            if (!suppressChildren)
            {
                expected.Add("Name");
            }
            Assert.Equal(expected, (modelSchema.Required ?? new HashSet<string>()).Order(StringComparer.Ordinal));
        }, configureMvcOptions: options =>
        {
            if (suppressChildren)
            {
                options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(action.Parameters[0].ParameterType));
            }
        });
    }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/child-validation")]
    private void ActionWithChildValidation([FromForm] ChildValidationForm model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/child-validation-binding")]
    private void ActionWithChildValidationAndBinding([FromForm] ChildValidationBindingForm model, [FromForm] string additional) { }

#nullable enable
    private class ChildValidationForm
    {
        public IFormFile? File { get; set; }
        [Required]
        public string? Name { get; set; }
    }

    private class ChildValidationBindingForm : ChildValidationForm
    {
        [BindRequired]
        public string? Bound { get; set; }
    }
#nullable restore

    [Theory]
    [InlineData(nameof(ActionWithDefaultRecord), "")]
    [InlineData(nameof(ActionWithRequiredRecord), "Id,Name")]
    [InlineData(nameof(ActionWithExplicitDefaultRecord), "Name")]
    [InlineData(nameof(ActionWithInitializedUnbindableForm), "")]
    [InlineData(nameof(ActionWithNullReadOnlyForm), "")]
    [InlineData(nameof(ActionWithInitForm), "Name")]
    [InlineData(nameof(ActionWithFilteredRecord), "")]
    [InlineData(nameof(ActionWithGetOnlyRecord), "Name")]
    [InlineData(nameof(ActionWithNeverRecord), "")]
    public async Task GetOpenApiRequestBody_UsesBindableEffectiveMetadata_MvcAction(string actionName, string expected)
    {
        // Exercise both the direct object and allOf layouts, and both advertised form media types.
        foreach (var withAdditionalParameter in new[] { false, true })
        {
            await VerifyEffectiveFormRequirements(actionName, withAdditionalParameter, expected);
        }
    }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/record-default")]
    private void ActionWithDefaultRecord([FromForm] DefaultRecord model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/record-required")]
    private void ActionWithRequiredRecord([FromForm] RequiredRecord model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/record-explicit-default")]
    private void ActionWithExplicitDefaultRecord([FromForm] ExplicitDefaultRecord model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/initialized-unbindable")]
    private void ActionWithInitializedUnbindableForm([FromForm] InitializedUnbindableForm model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/null-readonly")]
    private void ActionWithNullReadOnlyForm([FromForm] NullReadOnlyForm model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/init-form")]
    private void ActionWithInitForm([FromForm] InitForm model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/filtered-record")]
    private void ActionWithFilteredRecord([FromForm, Bind("File")] RequiredRecord model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/getonly-record")]
    private void ActionWithGetOnlyRecord([FromForm] GetOnlyRecord model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/never-record")]
    private void ActionWithNeverRecord([FromForm] NeverRecord model, [FromForm] string additional) { }

#nullable enable
    private record GetOnlyRecord([Required] string? Name)
    {
        public string? Name { get; } = Name;
        public IFormFile? File { get; init; }
    }

    private record NeverRecord([BindNever] string? Name = "default")
    {
        public IFormFile? File { get; init; }
    }

    private record DefaultRecord(string Name = "default")
    {
        public IFormFile? File { get; init; }
    }

    private record RequiredRecord([BindRequired] int Id, [Required] string? Name)
    {
        public IFormFile? File { get; init; }
    }

    private record ExplicitDefaultRecord([Required] string Name = "default")
    {
        public IFormFile? File { get; init; }
    }

    private class InitializedUnbindableForm
    {
        public IFormFile? File { get; set; }
        [Required]
        public string ReadOnly { get; } = "default";
        [BindNever, Required]
        public string Name { get; set; } = "default";
    }

    private class NullReadOnlyForm
    {
        public IFormFile? File { get; set; }
        [Required]
        public string? Name { get; }
    }

    private class InitForm
    {
        public IFormFile? File { get; init; }
        [Required]
        public string? Name { get; init; }
    }
#nullable restore

    [Theory]
    [InlineData(nameof(ActionWithAliasedRecord), "File,display_name", "display_name")]
    [InlineData(nameof(ActionWithOptionalAliasedRecord), "File,display_name", "")]
    [InlineData(nameof(ActionWithPrefixedAliasedRecord), "form.File,form.display_name", "form.display_name")]
    [InlineData(nameof(ActionWithDottedAliasedRecord), "File,display.name", "display.name")]
    public async Task GetOpenApiRequestBody_UsesConstructorFormAlias_MvcAction(string actionName, string properties, string required)
    {
        foreach (var withAdditionalParameter in new[] { false, true })
        {
            var action = CreateActionDescriptor(actionName);
            if (!withAdditionalParameter)
            {
                action.Parameters = action.Parameters.Take(1).ToArray();
            }
            await VerifyOpenApiDocument(action, document =>
            {
                var operation = Assert.Single(document.Paths.Values).Operations[HttpMethod.Post];
                foreach (var contentType in new[] { "application/x-www-form-urlencoded", "multipart/form-data" })
                {
                    var schema = operation.RequestBody.Content[contentType].Schema;
                    var modelSchema = withAdditionalParameter ? Assert.Single(schema.AllOf, item => item.Properties.ContainsKey(properties.Split(',')[0])) : schema;
                    Assert.Equal(properties.Split(','), modelSchema.Properties.Keys.Order(StringComparer.Ordinal));
                    Assert.Equal(required.Length == 0 ? Array.Empty<string>() : required.Split(','),
                        (modelSchema.Required ?? new HashSet<string>()).Order(StringComparer.Ordinal));
                }
            });
        }
    }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/record-alias")]
    private void ActionWithAliasedRecord([FromForm] AliasedRecord model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/record-optional-alias")]
    private void ActionWithOptionalAliasedRecord([FromForm] OptionalAliasedRecord model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/record-prefixed-alias")]
    private void ActionWithPrefixedAliasedRecord([FromForm(Name = "form")] AliasedRecord model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/record-dotted-alias")]
    private void ActionWithDottedAliasedRecord([FromForm] DottedAliasedRecord model, [FromForm] string additional) { }

#nullable enable
    private record AliasedRecord([FromForm(Name = "display_name"), BindRequired] string Name)
    {
        public IFormFile? File { get; init; }
    }

    private record OptionalAliasedRecord([FromForm(Name = "display_name")] string? Name)
    {
        public IFormFile? File { get; init; }
    }

    private record DottedAliasedRecord([FromForm(Name = "display.name"), BindRequired] string Name)
    {
        public IFormFile? File { get; init; }
    }
#nullable restore

    private async Task VerifyEffectiveFormRequirements(string actionName, bool withAdditionalParameter, string expected)
    {
        var action = CreateActionDescriptor(actionName);
        if (!withAdditionalParameter)
        {
            action.Parameters = action.Parameters.Take(1).ToArray();
        }
        await VerifyOpenApiDocument(action, document =>
        {
            var operation = Assert.Single(document.Paths.Values).Operations[HttpMethod.Post];
            foreach (var contentType in new[] { "application/x-www-form-urlencoded", "multipart/form-data" })
            {
                var schema = operation.RequestBody.Content[contentType].Schema;
                var modelSchema = withAdditionalParameter
                    ? Assert.Single(schema.AllOf, item => item.Properties.ContainsKey("File"))
                    : schema;
                Assert.Equal(action.Parameters[0].ParameterType.GetProperties().Select(property => property.Name).Order(StringComparer.Ordinal),
                    modelSchema.Properties.Keys.Order(StringComparer.Ordinal));
                Assert.Same(operation.RequestBody.Content["application/x-www-form-urlencoded"].Schema,
                    operation.RequestBody.Content["multipart/form-data"].Schema);
                Assert.Equal(expected.Length == 0 ? Array.Empty<string>() : expected.Split(','),
                    (modelSchema.Required ?? new HashSet<string>()).Order(StringComparer.Ordinal));
            }
        });
    }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/suppressed-validation")]
    private void ActionWithSuppressedValidation([FromForm] SuppressedValidationForm model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/suppressed-container")]
    private void ActionWithSuppressedContainer([FromForm] SuppressedContainerForm model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/excluded-binding")]
    private void ActionWithExcludedBinding([FromForm, Bind("Name")] BindingFilterForm model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/included-binding")]
    private void ActionWithIncludedBinding([FromForm, Bind("Name,Bound")] BindingFilterForm model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/binding-suppressed-validation")]
    private void ActionWithBindingAndSuppressedValidation([FromForm] BindingFilterForm model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/readonly-binding")]
    private void ActionWithReadOnlyBinding([FromForm] ReadOnlyBindingForm model, [FromForm] string additional) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/mutable-readonly-binding")]
    private void ActionWithMutableReadOnlyBinding([FromForm] MutableReadOnlyBindingForm model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/parameter-validation-filter")]
    private void ActionWithParameterValidationFilter([FromForm, ValidateNever] ParameterValidationForm model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/type-excluded-binding")]
    private void ActionWithTypeExcludedBinding([FromForm] TypeBindingFilterForm model) { }

    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [HttpPost("/conditional-validation-filter")]
    private void ActionWithConditionalValidationFilter([FromForm] ConditionalValidationForm model) { }

    private sealed class ConditionalValidationAttribute : Attribute, IPropertyValidationFilter
    {
        public bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry)
            => ((ConditionalValidationForm)parentEntry.Model).Validate;
    }

#nullable enable
    private class ConditionalValidationForm
    {
        public IFormFile? File { get; set; }
        public bool Validate { get; set; }
        [Required, ConditionalValidation]
        public string? Name { get; set; }
    }

    private class SuppressedValidationForm
    {
        public IFormFile? File { get; set; }
        [Required, ValidateNever]
        public string? Explicit { get; set; }
        [ValidateNever]
        public string Implicit { get; set; } = null!;
    }

    [ValidateNever]
    private class SuppressedContainerForm
    {
        public IFormFile? File { get; set; }
        [Required]
        public string? Name { get; set; }
    }

    private class BindingFilterForm
    {
        public IFormFile? File { get; set; }
        public string? Name { get; set; }
        [BindRequired, ValidateNever]
        public string? Bound { get; set; }
    }

    [Bind("Name")]
    private class TypeBindingFilterForm
    {
        public IFormFile? File { get; set; }
        public string? Name { get; set; }
        [BindRequired]
        public string? Bound { get; set; }
    }

    private class ReadOnlyBindingForm
    {
        public IFormFile? File { get; set; }
        [BindRequired]
        public int Count { get; }
        [BindRequired]
        public string? Text { get; }
        [BindRequired]
        public string[]? Array { get; }
    }

    private class MutableReadOnlyBindingForm
    {
        public IFormFile? File { get; set; }
        [BindRequired]
        public List<string>? Values { get; } = [];
    }

    private class ParameterValidationForm
    {
        public IFormFile? File { get; set; }
        [Required]
        public string? Name { get; set; }
    }
#nullable restore

    private class ModelWithSingleProperty
    {
        public string Name { get; set; }
    }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesFromFormWithNullableProperties_MvcAction()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ActionWithFormModelNullableProps));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            // Assert that all properties within the form schema are not marked as nullable
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.NotNull(item.Schema.Properties);
                Assert.All(item.Schema.Properties,
                    property =>
                    {
                        Assert.False(property.Value.Type?.HasFlag(JsonSchemaType.Null));
                    });
            }
        });
    }

    [Route("/form-model-nullable")]
    private void ActionWithFormModelNullableProps([FromForm] ModelWithNullableProperties model) { }

#nullable enable
    private class ModelWithNullableProperties
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
    }
#nullable restore

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesFormModelWithFile_MvcAction()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ActionWithFormModelWithFile));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            var item = Assert.Single(content.Values);
            Assert.NotNull(item.Schema);
            Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
            Assert.Collection(item.Schema.Properties,
                property =>
                {
                    Assert.Equal("Name", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("Description", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("Resume", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                    Assert.Equal("binary", property.Value.Format);
                });
        });
    }

    [Route("/resume")]
    private void ActionWithFormModelWithFile([FromForm] ResumeUpload model) { }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesFormModelWithFile()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/resume", ([FromForm] ResumeUpload model) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.Collection(item.Schema.Properties,
                    property =>
                    {
                        Assert.Equal("name", property.Key);
                        Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("description", property.Key);
                        Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("resume", property.Key);
                        Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                        Assert.Equal("binary", property.Value.Format);
                    });
            }
        });
    }

    [Theory]
    [InlineData(nameof(ActionWithDateTimeForm), JsonSchemaType.String, "date-time")]
    [InlineData(nameof(ActionWithGuidForm), JsonSchemaType.String, "uuid")]
    [InlineData(nameof(ActionWithIntForm), JsonSchemaType.Integer, "int32")]
    public async Task GetOpenApiRequestBody_HandlesFormWithPrimitives_MvcAction(string actionMethodName, JsonSchemaType type, string format)
    {
        // Arrange
        var action = CreateActionDescriptor(actionMethodName);

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            var item = Assert.Single(content.Values);
            Assert.NotNull(item.Schema);
            Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
            Assert.Collection(item.Schema.Properties,
                property =>
                {
                    Assert.Equal("model", property.Key);
                    Assert.Equal(type, property.Value.Type);
                    Assert.Equal(format, property.Value.Format);
                });
        });
    }

    [Route("/form-int")]
    private void ActionWithIntForm([FromForm] int model) { }

    [Route("/form-guid")]
    private void ActionWithGuidForm([FromForm] Guid model) { }

    [Route("/form-datetime")]
    private void ActionWithDateTimeForm([FromForm] DateTime model) { }

    public static object[][] FromFormWithPrimitives =>
    [
        [([FromForm] int id) => {}, JsonSchemaType.Integer, "int32"],
        [([FromForm] long id) => {}, JsonSchemaType.Integer, "int64"],
        [([FromForm] float id) => {}, JsonSchemaType.Number, "float"],
        [([FromForm] double id) => {}, JsonSchemaType.Number, "double"],
        [([FromForm] decimal id) => {}, JsonSchemaType.Number, "double"],
        [([FromForm] bool id) => {}, JsonSchemaType.Boolean, null],
        [([FromForm] string id) => {}, JsonSchemaType.String, null],
        [([FromForm] char id) => {}, JsonSchemaType.String, "char"],
        [([FromForm] byte id) => {}, JsonSchemaType.Integer, "uint8"],
        [([FromForm] short id) => {}, JsonSchemaType.Integer, "int16"],
        [([FromForm] ushort id) => {}, JsonSchemaType.Integer, "uint16"],
        [([FromForm] uint id) => {}, JsonSchemaType.Integer, "uint32"],
        [([FromForm] ulong id) => {}, JsonSchemaType.Integer, "uint64"],
        [([FromForm] Uri id) => {}, JsonSchemaType.String, "uri"],
        [([FromForm] TimeOnly id) => {}, JsonSchemaType.String, "time"],
        [([FromForm] DateOnly id) => {}, JsonSchemaType.String, "date"]
    ];

    [Theory]
    [MemberData(nameof(FromFormWithPrimitives))]
    public async Task GetOpenApiRequestBody_HandlesFormWithPrimitives(Delegate requestHandler, JsonSchemaType schemaType, string schemaFormat)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.Collection(item.Schema.Properties,
                    property =>
                    {
                        Assert.Equal("id", property.Key);
                        Assert.Equal(schemaType, property.Value.Type);
                        Assert.Equal(schemaFormat, property.Value.Format);
                    });
            }
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesFormWithMultipleMixedTypes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/", ([FromForm] Todo todo, IFormFile formFile, [FromForm] Guid guid) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.Collection(item.Schema.AllOf,
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                            {
                                Assert.Equal("id", property.Key);
                                Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("title", property.Key);
                                Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("completed", property.Key);
                                Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("createdAt", property.Key);
                                Assert.Equal(JsonSchemaType.String, property.Value.Type);
                                Assert.Equal("date-time", property.Value.Format);
                            });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                        {
                            Assert.Equal("formFile", property.Key);
                            Assert.Equal(JsonSchemaType.String, property.Value.Type);
                            Assert.Equal("binary", property.Value.Format);
                        });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                        {
                            Assert.Equal("guid", property.Key);
                            Assert.Equal(JsonSchemaType.String, property.Value.Type);
                            Assert.Equal("uuid", property.Value.Format);
                        });
                    });
            }
        });
    }

    [ConditionalFact(Skip = "https://github.com/dotnet/aspnetcore/issues/55349")]
    public async Task GetOpenApiRequestBody_HandlesFormWithMultipleMixedTypes_MvcAction()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ActionWithMixedFormTypes));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal(JsonSchemaType.Object, item.Schema.Type);
                Assert.Collection(item.Schema.AllOf,
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                            {
                                Assert.Equal("id", property.Key);
                                Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("title", property.Key);
                                Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("completed", property.Key);
                                Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("createdAt", property.Key);
                                Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                                Assert.Equal("date-time", property.Value.Format);
                            });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                        {
                            Assert.Equal("formFile", property.Key);
                            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                            Assert.Equal("binary", property.Value.Format);
                        });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                        {
                            Assert.Equal("guid", property.Key);
                            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                            Assert.Equal("uuid", property.Value.Format);
                        });
                    });
            }
        });
    }

    [Route("/form-mixed-types")]
    private void ActionWithMixedFormTypes([FromForm] Todo todo, IFormFile formFile, [FromForm] Guid guid) { }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesStreamAndPipeReader()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/stream", (Stream stream) => { });
        builder.MapGet("/pipereader", (PipeReader pipeReader) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            foreach (var path in document.Paths)
            {
                var operation = path.Value.Operations[HttpMethod.Get];
                Assert.NotNull(operation.RequestBody.Content);
                var content = Assert.Single(operation.RequestBody.Content);
                Assert.Equal("application/octet-stream", content.Key);
                Assert.NotNull(content.Value.Schema);
                Assert.Equal(JsonSchemaType.String, content.Value.Schema.Type);
                Assert.Equal("binary", content.Value.Schema.Format);
            }
        });
    }

    [ConditionalFact(Skip = "https://github.com/dotnet/aspnetcore/issues/55349")]
    public async Task GetOpenApiRequestBody_HandlesStreamAndPipeReader_MvcAction()
    {
        // Arrange
        var streamAction = CreateActionDescriptor(nameof(ActionWithStream));
        var pipeReaderAction = CreateActionDescriptor(nameof(ActionWithPipeReader));

        // Assert
        await VerifyOpenApiDocument(streamAction, VerifyDocument);
        await VerifyOpenApiDocument(pipeReaderAction, VerifyDocument);

        static void VerifyDocument(OpenApiDocument document)
        {
            var path = Assert.Single(document.Paths);
            var operation = path.Value.Operations[HttpMethod.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/octet-stream", content.Key);
            Assert.NotNull(content.Value.Schema);
            Assert.Equal(JsonSchemaType.String, content.Value.Schema.Type);
            Assert.Equal("binary", content.Value.Schema.Format);
        }
    }

    [Route("/stream")]
    private void ActionWithStream(Stream stream) { }
    [Route("/pipereader")]
    private void ActionWithPipeReader(PipeReader pipeReader) { }

    [Fact]
    public async Task GetRequestBody_HandlesJsonPatchBody()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", (JsonPatchDocument patch) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json-patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

#nullable enable
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetRequestBody_HandlesJsonPatchBodyOptionality(bool isOptional)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (isOptional)
        {
            builder.MapPatch("/", (JsonPatchDocument? patch) => { });
        }
        else
        {
            builder.MapPatch("/", (JsonPatchDocument patch) => { });
        }

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations![HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.Equal(!isOptional, operation.RequestBody.Required);
        });

    }
#nullable restore

    [Fact]
    public async Task GetRequestBody_HandlesJsonPatchBodyWithAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", ([FromBody] JsonPatchDocument patch) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json-patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

    [Fact]
    public async Task GetRequestBody_HandlesJsonPatchBodyWithAcceptsMetadata()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", (JsonPatchDocument name) => { }).Accepts(typeof(JsonPatchDocument), "application/vnd.github.patch+json");

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.DoesNotContain("application/json", operation.RequestBody.Content.Keys);
            Assert.DoesNotContain("application/json-patch+json", operation.RequestBody.Content.Keys);
            Assert.Equal("application/vnd.github.patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

    [Fact]
    public async Task GetRequestBody_HandlesJsonPatchBodyWithConsumesAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", [Consumes(typeof(JsonPatchDocument), "application/vnd.github.patch+json")] (JsonPatchDocument patch) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.DoesNotContain("application/json", operation.RequestBody.Content.Keys);
            Assert.DoesNotContain("application/json-patch+json", operation.RequestBody.Content.Keys);
            Assert.Equal("application/vnd.github.patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

    [Fact]
    public async Task GetRequestBody_HandlesGenericJsonPatchBody()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", (JsonPatchDocument<JsonPatchModel> patch) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json-patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

#nullable enable
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetRequestBody_HandlesGenericJsonPatchBodyOptionality(bool isOptional)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (isOptional)
        {
            builder.MapPatch("/", (JsonPatchDocument<JsonPatchModel>? patch) => { });
        }
        else
        {
            builder.MapPatch("/", (JsonPatchDocument<JsonPatchModel> patch) => { });
        }

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations![HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.Equal(!isOptional, operation.RequestBody.Required);
        });

    }
#nullable restore

    [Fact]
    public async Task GetRequestBody_HandlesGenericJsonPatchBodyWithAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", ([FromBody] JsonPatchDocument<JsonPatchModel> patch) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json-patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

    [Fact]
    public async Task GetRequestBody_HandlesGenericJsonPatchBodyWithAcceptsMetadata()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", (JsonPatchDocument<JsonPatchModel> name) => { })
               .Accepts(typeof(JsonPatchDocument<JsonPatchModel>), "application/vnd.github.patch+json");

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.DoesNotContain("application/json", operation.RequestBody.Content.Keys);
            Assert.DoesNotContain("application/json-patch+json", operation.RequestBody.Content.Keys);
            Assert.Equal("application/vnd.github.patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

    [Fact]
    public async Task GetRequestBody_HandlesGenericJsonPatchBodyWithConsumesAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", [Consumes(typeof(JsonPatchDocument<JsonPatchModel>), "application/vnd.github.patch+json")] (JsonPatchDocument<JsonPatchModel> patch) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.DoesNotContain("application/json", operation.RequestBody.Content.Keys);
            Assert.DoesNotContain("application/json-patch+json", operation.RequestBody.Content.Keys);
            Assert.Equal("application/vnd.github.patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

#nullable enable
    private sealed class JsonPatchModel
    {
        [JsonPropertyName("first")]
        public string? First { get; set; }

        [JsonPropertyName("second")]
        public string? Second { get; set; }
    }
#nullable restore

    [Fact]
    public async Task GetRequestBody_HandlesCustomJsonPatchBody()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", (CustomJsonPatchDocument patch) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json-patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

    private class CustomJsonPatchDocument : JsonPatchDocument;

    [Fact]
    public async Task GetRequestBody_HandlesGenericCustomJsonPatchBody()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPatch("/", (CustomJsonPatchDocument<JsonPatchModel> patch) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Patch];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json-patch+json", content.Key);
            var schema = Assert.IsType<OpenApiSchemaReference>(content.Value.Schema);
            Assert.Equal("JsonPatchDocument", schema.Reference.Id);
        });
    }

    private class CustomJsonPatchDocument<T> : JsonPatchDocument<T> where T : class;
}
