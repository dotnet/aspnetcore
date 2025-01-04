// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

[UsesVerify]
public sealed class OpenApiDocumentIntegrationTests(SampleAppFixture fixture) : IClassFixture<SampleAppFixture>
{
    [Theory]
    [InlineData("v1")]
    [InlineData("v2")]
    [InlineData("controllers")]
    [InlineData("responses")]
    [InlineData("forms")]
    [InlineData("schemas-by-ref")]
    public async Task VerifyOpenApiDocument(string documentName)
    {
        var documentService = fixture.Services.GetRequiredKeyedService<OpenApiDocumentService>(documentName);
        var scopedServiceProvider = fixture.Services.CreateScope();
        var document = await documentService.GetOpenApiDocumentAsync(scopedServiceProvider.ServiceProvider);
        await Verifier.Verify(GetOpenApiJson(document))
            .UseDirectory(SkipOnHelixAttribute.OnHelix()
                ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), "Integration", "snapshots")
                : "snapshots")
            .UseParameters(documentName);
    }

    private static string GetOpenApiJson(OpenApiDocument document)
    {
        using var textWriter = new StringWriter(CultureInfo.InvariantCulture);
        var jsonWriter = new OpenApiJsonWriter(textWriter);
        document.SerializeAsV31(jsonWriter);
        return textWriter.ToString();
    }
}
