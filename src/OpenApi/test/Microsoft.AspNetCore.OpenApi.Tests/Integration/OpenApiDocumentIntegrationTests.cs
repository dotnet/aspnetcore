// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Reader;

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
        var json = await GetOpenApiDocument(documentName, version);
        var baseSnapshotsDirectory = SkipOnHelixAttribute.OnHelix()
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), "Integration", "snapshots")
            : "snapshots";
        var outputDirectory = Path.Combine(baseSnapshotsDirectory, version.ToString());
        await Verify(json)
            .UseDirectory(outputDirectory)
            .UseParameters(documentName);
    }

    [Theory]
    [MemberData(nameof(OpenApiDocuments))]
    public async Task OpenApiDocumentIsValid(string documentName, OpenApiSpecVersion version)
    {
        var json = await GetOpenApiDocument(documentName, version);

        var actual = OpenApiDocument.Parse(json, format: "json");

        Assert.NotNull(actual);
        Assert.NotNull(actual.Document);
        Assert.NotNull(actual.Diagnostic);
        Assert.NotNull(actual.Diagnostic.Errors);
        Assert.Empty(actual.Diagnostic.Errors);

        var ruleSet = ValidationRuleSet.GetDefaultRuleSet();

        var errors = actual.Document.Validate(ruleSet);
        Assert.Empty(errors);
    }

    [Theory] // See https://github.com/dotnet/aspnetcore/issues/63090
    [MemberData(nameof(OpenApiDocuments))]
    public async Task OpenApiDocumentReferencesAreValid(string documentName, OpenApiSpecVersion version)
    {
        var json = await GetOpenApiDocument(documentName, version);

        var result = OpenApiDocument.Parse(json, format: "json");

        var document = result.Document;
        var documentNode = JsonNode.Parse(json);

        var actual = new List<string>();

        // TODO What other parts of the document should also be validated for references to be comprehensive?
        // Likely also needs to be recursive to validate all references in schemas, parameters, etc.
        if (document.Components is { Schemas.Count: > 0 } components)
        {
            foreach (var schema in components.Schemas)
            {
                if (schema.Value.Properties is { Count: > 0 } properties)
                {
                    foreach (var property in properties)
                    {
                        if (property.Value is not OpenApiSchemaReference reference)
                        {
                            continue;
                        }

                        var id = reference.Reference.ReferenceV3;

                        if (!IsValidSchemaReference(id, documentNode))
                        {
                            actual.Add($"Reference '{id}' on property '{property.Key}' of schema '{schema.Key}' is invalid.");
                        }
                    }
                }

                if (schema.Value.AllOf is { Count: > 0 } allOf)
                {
                    foreach (var child in allOf)
                    {
                        if (child is not OpenApiSchemaReference reference)
                        {
                            continue;
                        }

                        var id = reference.Reference.ReferenceV3;

                        if (!IsValidSchemaReference(id, documentNode))
                        {
                            actual.Add($"Reference '{id}' for AllOf of schema '{schema.Key}' is invalid.");
                        }
                    }
                }

                if (schema.Value.AnyOf is { Count: > 0 } anyOf)
                {
                    foreach (var child in anyOf)
                    {
                        if (child is not OpenApiSchemaReference reference)
                        {
                            continue;
                        }

                        var id = reference.Reference.ReferenceV3;

                        if (!IsValidSchemaReference(id, documentNode))
                        {
                            actual.Add($"Reference '{id}' for AnyOf of schema '{schema.Key}' is invalid.");
                        }
                    }
                }

                if (schema.Value.OneOf is { Count: > 0 } oneOf)
                {
                    foreach (var child in oneOf)
                    {
                        if (child is not OpenApiSchemaReference reference)
                        {
                            continue;
                        }

                        var id = reference.Reference.ReferenceV3;

                        if (!IsValidSchemaReference(id, documentNode))
                        {
                            actual.Add($"Reference '{id}' for OneOf of schema '{schema.Key}' is invalid.");
                        }
                    }
                }

                if (schema.Value.Discriminator is { Mapping.Count: > 0 } discriminator)
                {
                    foreach (var child in discriminator.Mapping)
                    {
                        if (child.Value is not OpenApiSchemaReference reference)
                        {
                            continue;
                        }

                        var id = reference.Reference.ReferenceV3;

                        if (!IsValidSchemaReference(id, documentNode))
                        {
                            actual.Add($"Reference '{id}' for Discriminator '{child.Key}' of schema '{schema.Key}' is invalid.");
                        }
                    }
                }
            }
        }

        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                if (operation.Value.Parameters is not { Count: > 0 } parameters)
                {
                    continue;
                }

                foreach (var parameter in parameters)
                {
                    if (parameter.Schema is not OpenApiSchemaReference reference)
                    {
                        continue;
                    }

                    var id = reference.Reference.ReferenceV3;

                    if (!IsValidSchemaReference(id, documentNode))
                    {
                        actual.Add($"Reference '{id}' on parameter '{parameter.Name}' of path '{path.Key}' of operation '{operation.Key}' is invalid.");
                    }
                }
            }
        }

        Assert.Empty(actual);

        static bool IsValidSchemaReference(string id, JsonNode baseNode)
        {
            var pointer = new JsonPointer(id.Replace("#/", "/"));
            return pointer.Find(baseNode) is not null;
        }
    }

    private async Task<string> GetOpenApiDocument(string documentName, OpenApiSpecVersion version)
    {
        var documentService = fixture.Services.GetRequiredKeyedService<OpenApiDocumentService>(documentName);
        var scopedServiceProvider = fixture.Services.CreateScope();
        var document = await documentService.GetOpenApiDocumentAsync(scopedServiceProvider.ServiceProvider);
        return await document.SerializeAsJsonAsync(version);
    }
}
