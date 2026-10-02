// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi.SourceGenerators.Json;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Tests;

[UsesVerify]
public class ValidatedJsonSchemaGeneratorTests
{
    [Fact]
    public Task GeneratesDeterministicArtifact()
        => SnapshotTestHelper.VerifyAdditionalFile(
            """
            public static class Program
            {
                public static void Main()
                {
                }
            }
            """,
            new ValidatedJsonSchemaGenerator(),
            "/different/machine/path/person.schema.json",
            """{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"object","properties":{"name":{"type":"string"}},"required":["name"],"additionalProperties":false}""",
            new Dictionary<string, string>
            {
                ["build_metadata.AdditionalFiles.OpenApiValidatedJsonSchema"] = "true",
                ["build_metadata.AdditionalFiles.LogicalName"] = "Person",
                ["build_metadata.AdditionalFiles.Dialect"] = "Draft202012",
                ["build_metadata.AdditionalFiles.Capabilities"] = "None",
            },
            out _);

    [Fact]
    public Task UsesExplicitSchemaIdentity()
        => SnapshotTestHelper.VerifyAdditionalFile(
            """
            public static class Program
            {
                public static void Main()
                {
                }
            }
            """,
            new ValidatedJsonSchemaGenerator(),
            "/machine/independent/person.schema.json",
            """{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"object"}""",
            new Dictionary<string, string>
            {
                ["build_metadata.AdditionalFiles.OpenApiValidatedJsonSchema"] = "true",
                ["build_metadata.AdditionalFiles.LogicalName"] = "Person",
                ["build_metadata.AdditionalFiles.Dialect"] = "Draft202012",
                ["build_metadata.AdditionalFiles.SchemaIdentity"] = "5f26e1108737429022275068f71a3f513d04a26e071e8ebecce88b76cff9a83a",
            },
            out _);

    [Fact]
    public Task RejectsInvalidExplicitSchemaIdentity()
        => SnapshotTestHelper.VerifyAdditionalFile(
            """
            public static class Program
            {
                public static void Main()
                {
                }
            }
            """,
            new ValidatedJsonSchemaGenerator(),
            "/machine/independent/person.schema.json",
            """{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"object"}""",
            new Dictionary<string, string>
            {
                ["build_metadata.AdditionalFiles.OpenApiValidatedJsonSchema"] = "true",
                ["build_metadata.AdditionalFiles.LogicalName"] = "Person",
                ["build_metadata.AdditionalFiles.Dialect"] = "Draft202012",
                ["build_metadata.AdditionalFiles.SchemaIdentity"] = "not-a-sha256",
            },
            out _);

    [Fact]
    public Task GeneratesClosedBinding()
        => SnapshotTestHelper.VerifyAdditionalFile(
            """
            using Microsoft.AspNetCore.OpenApi;

            public sealed class Person;

            public sealed class PersonValidator :
                IOpenApiValidatedJsonSchemaValidator<PersonValidatedJsonSchemaArtifact, PersonValidator>
            {
                public static string ConfigurationIdentity => "person-validator-v1";

                public static ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
                    ReadOnlyMemory<byte> utf8Json,
                    OpenApiSchemaEvidencePurpose purpose,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(OpenApiJsonSchemaValidationResult.Valid);
            }

            public static class Program
            {
                public static void Main()
                {
                }
            }
            """,
            new ValidatedJsonSchemaGenerator(),
            "/machine/independent/person.schema.json",
            """{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"object","additionalProperties":false}""",
            new Dictionary<string, string>
            {
                ["build_metadata.AdditionalFiles.OpenApiValidatedJsonSchema"] = "true",
                ["build_metadata.AdditionalFiles.LogicalName"] = "Person",
                ["build_metadata.AdditionalFiles.Dialect"] = "Draft202012",
                ["build_metadata.AdditionalFiles.Capabilities"] = "None",
                ["build_metadata.AdditionalFiles.ClrType"] = "global::Person",
                ["build_metadata.AdditionalFiles.ValidatorType"] = "global::PersonValidator",
                ["build_metadata.AdditionalFiles.ValidatorConfigurationIdentity"] = "person-validator-v1",
            },
            out _);

    [Theory]
    [InlineData("Draft4", "http://json-schema.org/draft-04/schema#")]
    [InlineData("Draft6", "http://json-schema.org/draft-06/schema#")]
    [InlineData("Draft7", "http://json-schema.org/draft-07/schema#")]
    [InlineData("Draft201909", "https://json-schema.org/draft/2019-09/schema")]
    [InlineData("Draft202012", "https://json-schema.org/draft/2020-12/schema")]
    public void NormalizerSupportsEveryDeclaredDialect(
        string dialectName,
        string dialectUri)
    {
        var dialect = ValidatedJsonSchemaGeneratorNormalizer.ParseDialect(dialectName);
        var normalized = ValidatedJsonSchemaGeneratorNormalizer.Normalize(
            $$"""{"$schema":"{{dialectUri}}","type":"string"}""",
            dialect);

        var schema = Assert.IsType<JsonObject>(normalized.Normalized);
        Assert.Equal(
            "https://json-schema.org/draft/2020-12/schema",
            Assert.IsType<JsonString>(schema.Properties["$schema"]).Value);
    }

    [Fact]
    public void NormalizerConvertsDraft4ExclusiveBound()
    {
        var normalized = ValidatedJsonSchemaGeneratorNormalizer.Normalize(
            """
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "type": "number",
              "minimum": 1,
              "exclusiveMinimum": true
            }
            """,
            GeneratedSchemaDialect.Draft4);

        var schema = Assert.IsType<JsonObject>(normalized.Normalized);
        Assert.False(schema.Properties.ContainsKey("minimum"));
        Assert.Equal("1", Assert.IsType<JsonNumber>(schema.Properties["exclusiveMinimum"]).Value);
    }

    [Fact]
    public void NormalizerRejectsUnresolvedReference()
    {
        var exception = Assert.Throws<SchemaGenerationException>(() =>
            ValidatedJsonSchemaGeneratorNormalizer.Normalize(
                """
                {
                  "$schema": "https://json-schema.org/draft/2020-12/schema",
                  "$ref": "#/$defs/missing"
                }
                """,
                GeneratedSchemaDialect.Draft202012));

        Assert.Equal("OASGEN006", exception.Code);
    }

    [Fact]
    public void NormalizerConvertsLegacyTupleAndDependencies()
    {
        var normalized = ValidatedJsonSchemaGeneratorNormalizer.Normalize(
            """
            {
              "$schema": "http://json-schema.org/draft-07/schema#",
              "type": "object",
              "definitions": { "name": { "type": "string" } },
              "properties": {
                "tuple": {
                  "type": "array",
                  "items": [{ "$ref": "#/definitions/name" }],
                  "additionalItems": false
                }
              },
              "dependencies": {
                "tuple": ["other"],
                "other": { "required": ["tuple"] }
              }
            }
            """,
            GeneratedSchemaDialect.Draft7);

        var schema = Assert.IsType<JsonObject>(normalized.Normalized);
        Assert.True(schema.Properties.ContainsKey("$defs"));
        Assert.True(schema.Properties.ContainsKey("dependentRequired"));
        Assert.True(schema.Properties.ContainsKey("dependentSchemas"));
        var tuple = Assert.IsType<JsonObject>(
            Assert.IsType<JsonObject>(schema.Properties["properties"]).Properties["tuple"]);
        Assert.True(tuple.Properties.ContainsKey("prefixItems"));
        Assert.False(Assert.IsType<JsonBoolean>(tuple.Properties["items"]).Value);
        var normalizedReference = Assert.IsType<JsonObject>(
            Assert.IsType<JsonArray>(
                Assert.IsType<JsonObject>(
                    Assert.IsType<JsonArray>(tuple.Properties["prefixItems"]).Items[0])
                .Properties["allOf"]).Items[0]);
        Assert.Equal(
            "#/$defs/name",
            Assert.IsType<JsonString>(normalizedReference.Properties["$ref"]).Value);
    }

    [Theory]
    [InlineData(
        """{"$schema":"https://json-schema.org/draft/2020-12/schema","$dynamicRef":"#node"}""",
        "OASGEN004")]
    [InlineData(
        """{"$schema":"https://json-schema.org/draft/2019-09/schema","$recursiveRef":"#"}""",
        "OASGEN004")]
    [InlineData(
        """{"$schema":"https://json-schema.org/draft/2020-12/schema","$ref":"https://example.com/schema"}""",
        "OASGEN006")]
    [InlineData(
        """{"$schema":"https://json-schema.org/draft/2020-12/schema","$vocabulary":{"https://example.com/vocab":true}}""",
        "OASGEN005")]
    public void NormalizerRejectsUnsupportedSemantics(string schema, string diagnostic)
    {
        var exception = Assert.Throws<SchemaGenerationException>(() =>
            ValidatedJsonSchemaGeneratorNormalizer.Normalize(
                schema,
                schema.Contains("2019-09", StringComparison.Ordinal)
                    ? GeneratedSchemaDialect.Draft201909
                    : GeneratedSchemaDialect.Draft202012));

        Assert.Equal(diagnostic, exception.Code);
    }

    [Fact]
    public void NormalizerRejectsDeclaredDialectMismatch()
    {
        var exception = Assert.Throws<SchemaGenerationException>(() =>
            ValidatedJsonSchemaGeneratorNormalizer.Normalize(
                """{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"string"}""",
                GeneratedSchemaDialect.Draft4));

        Assert.Equal("OASGEN003", exception.Code);
    }
}
