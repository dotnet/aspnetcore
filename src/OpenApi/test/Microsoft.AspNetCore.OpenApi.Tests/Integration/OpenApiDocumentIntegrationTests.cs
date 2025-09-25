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

        var ruleName = "OpenApiDocumentReferencesAreValid";
        var rule = new ValidationRule<OpenApiDocument>(ruleName, (context, item) =>
        {
            var visitor = new OpenApiSchemaReferenceVisitor(ruleName, context, documentNode);

            var walker = new OpenApiWalker(visitor);
            walker.Walk(item);
        });

        var ruleSet = new ValidationRuleSet();
        ruleSet.Add(typeof(OpenApiDocument), rule);

        var errors = document.Validate(ruleSet);

        Assert.Empty(errors);
    }

    private async Task<string> GetOpenApiDocument(string documentName, OpenApiSpecVersion version)
    {
        var documentService = fixture.Services.GetRequiredKeyedService<OpenApiDocumentService>(documentName);
        var scopedServiceProvider = fixture.Services.CreateScope();
        var document = await documentService.GetOpenApiDocumentAsync(scopedServiceProvider.ServiceProvider);

        return await document.SerializeAsJsonAsync(version);
    }

    private sealed class OpenApiSchemaReferenceVisitor(
        string ruleName,
        IValidationContext context,
        JsonNode document) : OpenApiVisitorBase
    {
        public override void Visit(IOpenApiReferenceHolder referenceHolder)
        {
            if (referenceHolder is OpenApiSchemaReference { Reference.IsLocal: true } reference)
            {
                ValidateSchemaReference(reference);
            }
        }

        public override void Visit(IOpenApiSchema schema)
        {
            if (schema is OpenApiSchemaReference { Reference.IsLocal: true } reference)
            {
                ValidateSchemaReference(reference);
            }
        }

        private void ValidateSchemaReference(OpenApiSchemaReference reference)
        {
            try
            {
                if (reference.RecursiveTarget is not null)
                {
                    return;
                }
            }
            catch (InvalidOperationException ex)
            {
                // Thrown if a circular reference is detected
                context.Enter($"{PathString[2..]}/{OpenApiSchemaKeywords.RefKeyword}");
                context.CreateError(ruleName, ex.Message);
                context.Exit();

                return;
            }

            var id = reference.Reference.ReferenceV3;

            if (id is { Length: > 0 } && !IsValidSchemaReference(id, document))
            {
                var isValid = false;

                // Sometimes ReferenceV3 is not a valid JSON pointer, but the $ref
                // associated with it still points to a valid location in the document.
                // In these cases, we need to find it manually to verify that fact before
                // generating a warning that the schema reference is indeed invalid.
                var parent = Find(PathString, document);
                var @ref = parent[OpenApiSchemaKeywords.RefKeyword];
                var path = PathString[2..]; // Trim off the leading "#/" as the context is already at the root

                if (@ref is not null && @ref.GetValueKind() is System.Text.Json.JsonValueKind.String &&
                    @ref.GetValue<string>() is { Length: > 0 } refId)
                {
                    id = refId;
                    path += $"/{OpenApiSchemaKeywords.RefKeyword}";
                    isValid = IsValidSchemaReference(id, document);
                }

                if (!isValid)
                {
                    context.Enter(path);
                    context.CreateWarning(ruleName, $"The schema reference '{id}' does not point to an existing schema.");
                    context.Exit();
                }
            }

            static bool IsValidSchemaReference(string id, JsonNode baseNode)
                => Find(id, baseNode) is not null;

            static JsonNode Find(string id, JsonNode baseNode)
            {
                var pointer = new JsonPointer(id.Replace("#/", "/"));
                return pointer.Find(baseNode);
            }
        }
    }
}
