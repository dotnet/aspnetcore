// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

public partial class OpenApiDocumentServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetRequestBody_VerifyDefaultFormEncoding()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (IFormFile formFile) => { });

        // Assert -- The defaults for form encoding are Explode = true and Style = Form
        // which align with the encoding formats that are used by ASP.NET Core's binding layer.
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("multipart/form-data", content.Key);
            var encoding = content.Value.Encoding["multipart/form-data"];
            Assert.True(encoding.Explode);
            Assert.Equal(ParameterStyle.Form, encoding.Style);
        });
    }

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
            var operation = paths.Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("multipart/form-data", content.Key);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.Properties);
            Assert.Contains("formFile", content.Value.Schema.Properties);
            var formFileProperty = content.Value.Schema.Properties["formFile"];
            Assert.Equal("string", formFileProperty.Type);
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
            var operation = paths.Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.Equal(!isOptional, operation.RequestBody.Required);
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
            var operation = paths.Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.False(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("multipart/form-data", content.Key);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.Properties);
            Assert.Contains("formFileCollection", content.Value.Schema.Properties);
            var formFileProperty = content.Value.Schema.Properties["formFileCollection"];
            Assert.Equal("array", formFileProperty.Type);
            Assert.Equal("string", formFileProperty.Items.Type);
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
            var operation = paths.Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.Equal(!isOptional, operation.RequestBody.Required);
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
            var operation = paths.Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("multipart/form-data", content.Key);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.AllOf);
            Assert.Collection(content.Value.Schema.AllOf,
                allOfItem =>
                {
                    Assert.NotNull(allOfItem.Properties);
                    Assert.Contains("formFile1", allOfItem.Properties);
                    var formFile1Property = allOfItem.Properties["formFile1"].GetEffective(document);
                    Assert.Equal("string", formFile1Property.Type);
                    Assert.Equal("binary", formFile1Property.Format);
                },
                allOfItem =>
                {
                    Assert.NotNull(allOfItem.Properties);
                    Assert.Contains("formFile2", allOfItem.Properties);
                    var formFile2Property = allOfItem.Properties["formFile2"].GetEffective(document);
                    Assert.Equal("string", formFile2Property.Type);
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
            var operation = paths.Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/magic-foo-content-type", content.Key);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.Properties);
            Assert.Contains("formFile", content.Value.Schema.Properties);
            var formFileProperty = content.Value.Schema.Properties["formFile"];
            Assert.Equal("string", formFileProperty.Type);
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
            var operation = paths.Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/magic-foo-content-type", content.Key);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.NotNull(content.Value.Schema.Properties);
            Assert.Contains("formFile", content.Value.Schema.Properties);
            var formFileProperty = content.Value.Schema.Properties["formFile"];
            Assert.Equal("string", formFileProperty.Type);
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
            var operation = paths.Operations[OperationType.Post];
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
            var operation = paths.Operations[OperationType.Post];
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
            var operation = paths.Operations[OperationType.Post];
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
            var operation = paths.Operations[OperationType.Post];
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
            var operation = paths.Operations[OperationType.Post];
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
            var operation = paths.Operations[OperationType.Post];
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
            var operation = paths.Operations[OperationType.Post];
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
                Assert.Equal("object", item.Schema.Type);
                Assert.NotNull(item.Schema.Properties);
                Assert.Collection(item.Schema.Properties,
                    property =>
                    {
                        Assert.Equal("id", property.Key);
                        Assert.Equal("integer", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("title", property.Key);
                        Assert.Equal("string", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("completed", property.Key);
                        Assert.Equal("boolean", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("createdAt", property.Key);
                        Assert.Equal("string", property.Value.Type);
                        Assert.Equal("date-time", property.Value.Format);
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
            var operation = paths.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            // Same schema should be produced for both content-types
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal("object", item.Schema.Type);
                Assert.NotNull(item.Schema.Properties);
                Assert.Collection(item.Schema.Properties,
                    property =>
                    {
                        Assert.Equal("Id", property.Key);
                        Assert.Equal("integer", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("Title", property.Key);
                        Assert.Equal("string", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("Completed", property.Key);
                        Assert.Equal("boolean", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("CreatedAt", property.Key);
                        Assert.Equal("string", property.Value.Type);
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
            var operation = paths.Operations[OperationType.Post];
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
                Assert.Equal("object", item.Schema.Type);
                Assert.NotNull(item.Schema.AllOf);
                Assert.Collection(item.Schema.AllOf,
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                            {
                                Assert.Equal("id", property.Key);
                                Assert.Equal("integer", property.Value.GetEffective(document).Type);
                            },
                            property =>
                            {
                                Assert.Equal("title", property.Key);
                                Assert.Equal("string", property.Value.GetEffective(document).Type);
                            },
                            property =>
                            {
                                Assert.Equal("completed", property.Key);
                                Assert.Equal("boolean", property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("createdAt", property.Key);
                                Assert.Equal("string", property.Value.Type);
                                Assert.Equal("date-time", property.Value.Format);
                            });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties,
                            property =>
                            {
                                Assert.Equal("code", property.Key);
                                Assert.Equal("integer", property.Value.GetEffective(document).Type);
                            },
                            property =>
                            {
                                Assert.Equal("message", property.Key);
                                Assert.Equal("string", property.Value.GetEffective(document).Type);
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
            var operation = paths.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            // Same schema should be produced for both content-types
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal("object", item.Schema.Type);
                Assert.NotNull(item.Schema.AllOf);
                Assert.Collection(item.Schema.AllOf,
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                            {
                                Assert.Equal("Id", property.Key);
                                Assert.Equal("integer", property.Value.GetEffective(document).Type);
                            },
                            property =>
                            {
                                Assert.Equal("Title", property.Key);
                                Assert.Equal("string", property.Value.GetEffective(document).Type);
                            },
                            property =>
                            {
                                Assert.Equal("Completed", property.Key);
                                Assert.Equal("boolean", property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("CreatedAt", property.Key);
                                Assert.Equal("string", property.Value.Type);
                                Assert.Equal("date-time", property.Value.Format);
                            });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties,
                            property =>
                            {
                                Assert.Equal("Code", property.Key);
                                Assert.Equal("integer", property.Value.GetEffective(document).Type);
                            },
                            property =>
                            {
                                Assert.Equal("Message", property.Key);
                                Assert.Equal("string", property.Value.GetEffective(document).Type);
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
            var operation = paths.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            // Forms can be provided in both the URL and via form data
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            // Same schema should be produced for both content-types
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal("object", item.Schema.Type);
                Assert.NotNull(item.Schema.Properties);
                Assert.Collection(item.Schema.Properties,
                    property =>
                    {
                        Assert.Equal("Name", property.Key);
                        Assert.Equal("string", property.Value.Type);
                    });
            }
        });
    }

    [Route("/form-model-single-prop")]
    private void ActionWithFormModelSingleProp([FromForm] ModelWithSingleProperty model) { }

    private class ModelWithSingleProperty
    {
        public string Name { get; set; }
    }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesFormModelWithFile_MvcAction()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ActionWithFormModelWithFile));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            var item = Assert.Single(content.Values);
            Assert.NotNull(item.Schema);
            Assert.Equal("object", item.Schema.Type);
            Assert.Collection(item.Schema.Properties,
                property =>
                {
                    Assert.Equal("Name", property.Key);
                    Assert.Equal("string", property.Value.GetEffective(document).Type);
                },
                property =>
                {
                    Assert.Equal("Description", property.Key);
                    Assert.Equal("string", property.Value.GetEffective(document).Type);
                },
                property =>
                {
                    Assert.Equal("Resume", property.Key);
                    Assert.Equal("string", property.Value.GetEffective(document).Type);
                    Assert.Equal("binary", property.Value.GetEffective(document).Format);
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
            var operation = paths.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal("object", item.Schema.Type);
                Assert.Collection(item.Schema.Properties,
                    property =>
                    {
                        Assert.Equal("name", property.Key);
                        Assert.Equal("string", property.Value.GetEffective(document).Type);
                    },
                    property =>
                    {
                        Assert.Equal("description", property.Key);
                        Assert.Equal("string", property.Value.GetEffective(document).Type);
                    },
                    property =>
                    {
                        Assert.Equal("resume", property.Key);
                        Assert.Equal("string", property.Value.Type);
                        Assert.Equal("binary", property.Value.Format);
                    });
            }
        });
    }

    [Theory]
    [InlineData(nameof(ActionWithDateTimeForm), "string", "date-time")]
    [InlineData(nameof(ActionWithGuidForm), "string", "uuid")]
    [InlineData(nameof(ActionWithIntForm), "integer", "int32")]
    public async Task GetOpenApiRequestBody_HandlesFormWithPrimitives_MvcAction(string actionMethodName, string type, string format)
    {
        // Arrange
        var action = CreateActionDescriptor(actionMethodName);

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            var item = Assert.Single(content.Values);
            Assert.NotNull(item.Schema);
            Assert.Equal("object", item.Schema.Type);
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
        [([FromForm] int id) => {}, "integer", "int32"],
        [([FromForm] long id) => {}, "integer", "int64"],
        [([FromForm] float id) => {}, "number", "float"],
        [([FromForm] double id) => {}, "number", "double"],
        [([FromForm] decimal id) => {}, "number", "double"],
        [([FromForm] bool id) => {}, "boolean", null],
        [([FromForm] string id) => {}, "string", null],
        [([FromForm] char id) => {}, "string", "char"],
        [([FromForm] byte id) => {}, "integer", "uint8"],
        [([FromForm] short id) => {}, "integer", "int16"],
        [([FromForm] ushort id) => {}, "integer", "uint16"],
        [([FromForm] uint id) => {}, "integer", "uint32"],
        [([FromForm] ulong id) => {}, "integer", "uint64"],
        [([FromForm] Uri id) => {}, "string", "uri"],
        [([FromForm] TimeOnly id) => {}, "string", "time"],
        [([FromForm] DateOnly id) => {}, "string", "date"]
    ];

    [Theory]
    [MemberData(nameof(FromFormWithPrimitives))]
    public async Task GetOpenApiRequestBody_HandlesFormWithPrimitives(Delegate requestHandler, string schemaType, string schemaFormat)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal("object", item.Schema.Type);
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
            var operation = paths.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal("object", item.Schema.Type);
                Assert.Collection(item.Schema.AllOf,
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                            {
                                Assert.Equal("id", property.Key);
                                Assert.Equal("integer", property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("title", property.Key);
                                Assert.Equal("string", property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("completed", property.Key);
                                Assert.Equal("boolean", property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("createdAt", property.Key);
                                Assert.Equal("string", property.Value.Type);
                                Assert.Equal("date-time", property.Value.Format);
                            });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                        {
                            Assert.Equal("formFile", property.Key);
                            Assert.Equal("string", property.Value.Type);
                            Assert.Equal("binary", property.Value.Format);
                        });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                        {
                            Assert.Equal("guid", property.Key);
                            Assert.Equal("string", property.Value.Type);
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
            var operation = paths.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;
            foreach (var item in content.Values)
            {
                Assert.NotNull(item.Schema);
                Assert.Equal("object", item.Schema.Type);
                Assert.Collection(item.Schema.AllOf,
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                            {
                                Assert.Equal("id", property.Key);
                                Assert.Equal("integer", property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("title", property.Key);
                                Assert.Equal("string", property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("completed", property.Key);
                                Assert.Equal("boolean", property.Value.Type);
                            },
                            property =>
                            {
                                Assert.Equal("createdAt", property.Key);
                                Assert.Equal("string", property.Value.Type);
                                Assert.Equal("date-time", property.Value.Format);
                            });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                        {
                            Assert.Equal("formFile", property.Key);
                            Assert.Equal("string", property.Value.Type);
                            Assert.Equal("binary", property.Value.Format);
                        });
                    },
                    allOfItem =>
                    {
                        Assert.Collection(allOfItem.Properties, property =>
                        {
                            Assert.Equal("guid", property.Key);
                            Assert.Equal("string", property.Value.Type);
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
                var operation = path.Value.Operations[OperationType.Get];
                Assert.NotNull(operation.RequestBody.Content);
                var content = Assert.Single(operation.RequestBody.Content);
                Assert.Equal("application/octet-stream", content.Key);
                Assert.NotNull(content.Value.Schema);
                Assert.Equal("string", content.Value.Schema.GetEffective(document).Type);
                Assert.Equal("binary", content.Value.Schema.GetEffective(document).Format);
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
            var operation = path.Value.Operations[OperationType.Get];
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/octet-stream", content.Key);
            Assert.NotNull(content.Value.Schema);
            Assert.Equal("string", content.Value.Schema.Type);
            Assert.Equal("binary", content.Value.Schema.Format);
        }
    }

    [Route("/stream")]
    private void ActionWithStream(Stream stream) { }
    [Route("/pipereader")]
    private void ActionWithPipeReader(PipeReader pipeReader) { }
}
