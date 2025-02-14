// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using System.Text.RegularExpressions;

[UsesVerify]
public sealed class OpenApiDocumentIntegrationTests(SampleAppFixture fixture) : IClassFixture<SampleAppFixture>
{
    private static Regex DateTimeRegex() => new(
        @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}[+-]\d{2}:\d{2}",
        RegexOptions.Compiled);

    [Theory]
    [InlineData("v1", OpenApiSpecVersion.OpenApi3_0)]
    [InlineData("v2", OpenApiSpecVersion.OpenApi3_0)]
    [InlineData("controllers", OpenApiSpecVersion.OpenApi3_0)]
    [InlineData("responses", OpenApiSpecVersion.OpenApi3_0)]
    [InlineData("forms", OpenApiSpecVersion.OpenApi3_0)]
    [InlineData("schemas-by-ref", OpenApiSpecVersion.OpenApi3_0)]
    [InlineData("xml", OpenApiSpecVersion.OpenApi3_0)]
    [InlineData("v1", OpenApiSpecVersion.OpenApi3_1)]
    [InlineData("v2", OpenApiSpecVersion.OpenApi3_1)]
    [InlineData("controllers", OpenApiSpecVersion.OpenApi3_1)]
    [InlineData("responses", OpenApiSpecVersion.OpenApi3_1)]
    [InlineData("forms", OpenApiSpecVersion.OpenApi3_1)]
    [InlineData("schemas-by-ref", OpenApiSpecVersion.OpenApi3_1)]
    [InlineData("xml", OpenApiSpecVersion.OpenApi3_1)]
    public async Task VerifyOpenApiDocument(string documentName, OpenApiSpecVersion version)
    {
        var documentService = fixture.Services.GetRequiredKeyedService<OpenApiDocumentService>(documentName);
        var scopedServiceProvider = fixture.Services.CreateScope();
        var document = await documentService.GetOpenApiDocumentAsync(scopedServiceProvider.ServiceProvider);
        var json = await document.SerializeAsJsonAsync(version);
        var baseSnapshotsDirectory = SkipOnHelixAttribute.OnHelix()
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), "Integration", "snapshots")
            : "snapshots";
        var outputDirectory = Path.Combine(baseSnapshotsDirectory, version.ToString());
        await Verifier.Verify(json)
            .UseDirectory(outputDirectory)
            .ScrubLinesWithReplace(line => DateTimeRegex().Replace(line, "[datetime]"))
            .UseParameters(documentName);
    }
}
