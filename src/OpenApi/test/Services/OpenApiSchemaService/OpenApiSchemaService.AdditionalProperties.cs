// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task SetsAdditionalPropertiesOnBuiltInTypeWithExtensionData()
    {
        var builder = CreateBuilder();

        builder.MapPost("/", () => new ProblemDetails());

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Components.Schemas["ProblemDetails"];
            Assert.NotNull(schema.AdditionalProperties);
            Assert.Null(schema.AdditionalProperties.Type);
        });
    }

    [Fact]
    public async Task SetAdditionalPropertiesOnDefinedTypeWithExtensionData()
    {
        var builder = CreateBuilder();

        builder.MapPost("/", () => new MyExtensionDataType("test"));

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Components.Schemas["MyExtensionDataType"];
            Assert.NotNull(schema.AdditionalProperties);
            Assert.Null(schema.AdditionalProperties.Type);
        });
    }

    [Fact]
    public async Task SetsAdditionalPropertiesOnDictionaryTypesWithSchema()
    {
        var builder = CreateBuilder();

        builder.MapPost("/", (Dictionary<string, Guid> data) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Post];
            var schema = operation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.NotNull(schema.AdditionalProperties);
            Assert.Equal("string", schema.AdditionalProperties.Type);
            Assert.Equal("uuid", schema.AdditionalProperties.Format);
        });
    }

    private class MyExtensionDataType(string name)
    {
        public string Name => name;

        [JsonExtensionData]
        public IDictionary<string, object> ExtensionData { get; set; }
    }
}
