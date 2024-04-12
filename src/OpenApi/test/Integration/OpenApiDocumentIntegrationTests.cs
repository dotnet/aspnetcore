// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

public sealed class OpenApiDocumentIntegrationTests(SampleAppFixture fixture) : IClassFixture<SampleAppFixture>
{
    [Theory]
    [InlineData("v1")]
    [InlineData("v2")]
    [InlineData("responses")]
    [InlineData("forms")]
    public async Task VerifyOpenApiDocument(string documentName)
    {
        var documentService = fixture.Services.GetRequiredKeyedService<OpenApiDocumentService>(documentName);
        var document = await documentService.GetOpenApiDocumentAsync();
        await Verify(GetOpenApiJson(document)).UseParameters(documentName);
    }

    private static string GetOpenApiJson(OpenApiDocument document)
    {
        using var textWriter = new StringWriter(CultureInfo.InvariantCulture);
        var jsonWriter = new OpenApiJsonWriter(textWriter);
        document.SerializeAsV3(jsonWriter);
        return textWriter.ToString();
    }
}
