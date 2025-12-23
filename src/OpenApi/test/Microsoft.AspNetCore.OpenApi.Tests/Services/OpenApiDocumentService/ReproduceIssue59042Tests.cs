// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

public class ReproduceIssue59042Tests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetOpenApiRequestBody_RespectsDescriptionOnFromFormProperty()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/form", ([FromForm] FormWithDescription form) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;

            // Check multipart/form-data
            Assert.Contains("multipart/form-data", content.Keys);
            var multipartMediaType = content["multipart/form-data"];
            Assert.NotNull(multipartMediaType.Schema);
            Assert.NotNull(multipartMediaType.Schema.Properties);

            Assert.Contains("name", multipartMediaType.Schema.Properties);
            var nameProperty = multipartMediaType.Schema.Properties["name"];
            Assert.Equal("The name of the item", nameProperty.Description);

            Assert.Contains("file", multipartMediaType.Schema.Properties);
            var fileProperty = multipartMediaType.Schema.Properties["file"];
            Assert.Equal("The file to upload", fileProperty.Description);

            // Check application/x-www-form-urlencoded
            Assert.Contains("application/x-www-form-urlencoded", content.Keys);
            var urlEncodedMediaType = content["application/x-www-form-urlencoded"];
            Assert.NotNull(urlEncodedMediaType.Schema);
            Assert.NotNull(urlEncodedMediaType.Schema.Properties);

            Assert.Contains("name", urlEncodedMediaType.Schema.Properties);
            var urlEncodedNameProperty = urlEncodedMediaType.Schema.Properties["name"];
            Assert.Equal("The name of the item", urlEncodedNameProperty.Description);
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_RespectsDescriptionOnFromFormParameter()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/form-param", ([FromForm, Description("The ID")] int id) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;

            var mediaType = content["multipart/form-data"];
            Assert.NotNull(mediaType.Schema);
            Assert.NotNull(mediaType.Schema.Properties);

            Assert.Contains("id", mediaType.Schema.Properties);
            var property = mediaType.Schema.Properties["id"];

            Assert.Equal("The ID", property.Description);
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_RespectsDescriptionOnFromFormComplexParameter()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/form-complex", ([FromForm, Description("The Complex Object")] FormWithDescription form) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var paths = Assert.Single(document.Paths.Values);
            var operation = paths.Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = operation.RequestBody.Content;

            var mediaType = content["multipart/form-data"];
            Assert.NotNull(mediaType.Schema);
            Assert.NotNull(mediaType.Schema.AllOf);

            Assert.Equal("The Complex Object", mediaType.Schema.Description ?? mediaType.Schema.AllOf.FirstOrDefault(s => s.Description != null)?.Description);
        });
    }

    private class FormWithDescription
    {
        [Description("The name of the item")]
        public string Name { get; set; }

        [Description("The file to upload")]
        public IFormFile File { get; set; }
    }
}
