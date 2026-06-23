// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

[UsesVerify]
public sealed class OpenApiDocumentEnumNamingPolicyIntegrationTests : OpenApiDocumentServiceTestBase
{
    public static TheoryData<OpenApiSpecVersion> SpecVersions() => new()
    {
        OpenApiSpecVersion.OpenApi3_0,
        OpenApiSpecVersion.OpenApi3_1,
        OpenApiSpecVersion.OpenApi3_2,
    };

    [Theory]
    [MemberData(nameof(SpecVersions))]
    public async Task GetOpenApiParameters_NullableEnumWithGlobalNamingPolicy_NonBodySchemaEnumIncludesNull(OpenApiSpecVersion version)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower));
        });
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/api/query", (Priority? priority) => { });

        await VerifyDocument(builder, version);
    }

    [Theory]
    [MemberData(nameof(SpecVersions))]
    public async Task GetOpenApiRequestBody_NullableEnumWithGlobalNamingPolicy_BodySchemaEnumIncludesNull(OpenApiSpecVersion version)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower));
        });
        var builder = CreateBuilder(serviceCollection);

        builder.MapPost("/body-enum", ([FromBody] Priority? priority) => { });

        await VerifyDocument(builder, version);
    }

    private static async Task VerifyDocument(IEndpointRouteBuilder builder, OpenApiSpecVersion version, [CallerMemberName] string testName = null)
    {
        OpenApiDocument capturedDocument = null;
        await VerifyOpenApiDocument(builder, document => capturedDocument = document);
        var json = await capturedDocument.SerializeAsJsonAsync(version);

        var baseSnapshotsDirectory = SkipOnHelixAttribute.OnHelix()
            ? Path.Combine(AppContext.BaseDirectory, "Integration", "snapshots")
            : "snapshots";
        var outputDirectory = Path.Combine(baseSnapshotsDirectory, version.ToString());

        await Verify(json)
            .UseDirectory(outputDirectory)
            .UseMethodName(testName);
    }
}
