// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
            Assert.NotNull(content.Value.Schema.Properties);
            Assert.Contains("formFile1", content.Value.Schema.Properties);
            Assert.Contains("formFile2", content.Value.Schema.Properties);
            var formFile1Property = content.Value.Schema.Properties["formFile1"];
            Assert.Equal("string", formFile1Property.Type);
            Assert.Equal("binary", formFile1Property.Format);
            var formFile2Property = content.Value.Schema.Properties["formFile2"];
            Assert.Equal("string", formFile2Property.Type);
            Assert.Equal("binary", formFile2Property.Format);
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

}
