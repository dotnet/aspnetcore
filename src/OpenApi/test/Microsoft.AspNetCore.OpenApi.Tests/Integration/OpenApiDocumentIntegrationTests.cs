// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.OpenApi;

[UsesVerify]
public sealed class OpenApiDocumentIntegrationTests(SampleAppFixture fixture) : IClassFixture<SampleAppFixture>
{
    public static TheoryData<string, OpenApiSpecVersion> OpenApiDocuments()
    {
        OpenApiSpecVersion[] versions =
        [
            OpenApiSpecVersion.OpenApi3_0,
            OpenApiSpecVersion.OpenApi3_1,
        ];

        var testCases = new TheoryData<string, OpenApiSpecVersion>();

        foreach (var version in versions)
        {
            testCases.Add("v1", version);
            testCases.Add("v2", version);
            testCases.Add("controllers", version);
            testCases.Add("responses", version);
            testCases.Add("forms", version);
            testCases.Add("schemas-by-ref", version);
            testCases.Add("xml", version);
        }

        return testCases;
    }

    [Theory]
    [MemberData(nameof(OpenApiDocuments))]
    public async Task VerifyOpenApiDocument(string documentName, OpenApiSpecVersion version)
    {
        using var client = fixture.CreateClient();
        var json = await client.GetStringAsync($"/openapi/{documentName}.json");
        var baseSnapshotsDirectory = SkipOnHelixAttribute.OnHelix()
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), "Integration", "snapshots")
            : "snapshots";
        var outputDirectory = Path.Combine(baseSnapshotsDirectory, version.ToString());
        await Verify(json)
            .UseDirectory(outputDirectory)
            .UseParameters(documentName);
    }
}
