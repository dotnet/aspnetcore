// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

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
        using var client = fixture.CreateClient();
        var json = await client.GetStringAsync($"/openapi/{documentName}.json");
        await Verify(json)
            .UseDirectory(SkipOnHelixAttribute.OnHelix()
                ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), "Integration", "snapshots")
                : "snapshots")
            .UseParameters(documentName);
    }
}
