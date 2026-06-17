// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

[UsesVerify]
public sealed class OpenApiDocumentLocalizationTests(LocalizedSampleAppFixture fixture) : IClassFixture<LocalizedSampleAppFixture>
{
    [Fact]
    public async Task VerifyOpenApiDocumentIsInvariant()
    {
        using var client = fixture.CreateClient();
        var json = await client.GetStringAsync("/openapi/localized.json");
        var outputDirectory = SkipOnHelixAttribute.OnHelix()
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), "Integration", "snapshots")
            : "snapshots";
        await Verify(json)
            .UseDirectory(outputDirectory);
    }
}
