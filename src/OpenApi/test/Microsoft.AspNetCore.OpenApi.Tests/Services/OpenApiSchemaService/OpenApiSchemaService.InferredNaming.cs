// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.OpenApi;

public partial class OpenApiSchemaServiceTests
{
    [Fact]
    public async Task SchemaGenerationMode_Inferred_DisambiguatesNamespaceCollisionsIndependentOfEndpointOrder()
    {
        var first = await CreateDocument(reverseEndpoints: false);
        var second = await CreateDocument(reverseEndpoints: true);

        var firstSchemas = JsonNode.Parse(await first.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        var secondSchemas = JsonNode.Parse(await second.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        Assert.True(JsonNode.DeepEquals(firstSchemas, secondSchemas));
        Assert.Equal(
            "First.Duplicate",
            Assert.IsType<OpenApiSchemaReference>(first.Paths["/first"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema).Reference.Id);
        Assert.Equal(
            "Second.Duplicate",
            Assert.IsType<OpenApiSchemaReference>(first.Paths["/second"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema).Reference.Id);
        Assert.Equal(["First.Duplicate", "Second.Duplicate"], first.Components.Schemas.Keys);
        foreach (var version in new[] { OpenApiSpecVersion.OpenApi3_0, OpenApiSpecVersion.OpenApi3_1, OpenApiSpecVersion.OpenApi3_2 })
        {
            var serialized = JsonNode.Parse(await first.SerializeAsJsonAsync(version));
            Assert.Equal(
                "#/components/schemas/First.Duplicate",
                serialized?["paths"]?["/first"]?["post"]?["requestBody"]?["content"]?["application/json"]?["schema"]?["$ref"]?.GetValue<string>());
            Assert.Equal(
                "#/components/schemas/Second.Duplicate",
                serialized?["paths"]?["/second"]?["post"]?["requestBody"]?["content"]?["application/json"]?["schema"]?["$ref"]?.GetValue<string>());
        }

        static async Task<OpenApiDocument> CreateDocument(bool reverseEndpoints)
        {
            var builder = CreateBuilder();
            if (reverseEndpoints)
            {
                builder.MapPost("/second", (InferredNaming.Second.Duplicate value) => { });
                builder.MapPost("/first", (InferredNaming.First.Duplicate value) => { });
            }
            else
            {
                builder.MapPost("/first", (InferredNaming.First.Duplicate value) => { });
                builder.MapPost("/second", (InferredNaming.Second.Duplicate value) => { });
            }

            return await VerifyOpenApiDocument(builder, CreateInferredOptions(), _ => { });
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_ExplicitDefaultReferenceIdUsesProgressiveDisambiguation()
    {
        var builder = CreateBuilder();
        builder.MapPost("/first", (InferredNaming.First.Duplicate value) => { });
        builder.MapPost("/second", (InferredNaming.Second.Duplicate value) => { });
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = OpenApiOptions.CreateDefaultSchemaReferenceId;

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal(["First.Duplicate", "Second.Duplicate"], document.Components.Schemas.Keys);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_DisambiguatedIdsFlowThroughArraysAndNullableWrappers()
    {
        var builder = CreateBuilder();
        builder.MapGet("/", () => new InferredNaming.CollisionContainer());

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var container = document.Components.Schemas[nameof(InferredNaming.CollisionContainer)];
            Assert.Equal(
                "First.Duplicate",
                Assert.IsType<OpenApiSchemaReference>(container.Properties["items"].Items).Reference.Id);
            Assert.Collection(
                container.Properties["optional"].OneOf,
                branch => Assert.Equal(JsonSchemaType.Null, branch.Type),
                branch => Assert.Equal(
                    "Second.Duplicate",
                    Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_DisambiguatesNestedAndGenericCollisions()
    {
        var builder = CreateBuilder();
        builder.MapPost("/outer-a", (InferredNaming.OuterA.Item value) => { });
        builder.MapPost("/outer-b", (InferredNaming.OuterB.Item value) => { });
        builder.MapPost("/generic-a", (InferredNaming.Pair<InferredNaming.AAndB, InferredNaming.C> value) => { });
        builder.MapPost("/generic-b", (InferredNaming.Pair<InferredNaming.A, InferredNaming.BAndC> value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            Assert.Contains("OuterA.Item", document.Components.Schemas.Keys);
            Assert.Contains("OuterB.Item", document.Components.Schemas.Keys);

            var genericIds = new[]
            {
                Assert.IsType<OpenApiSchemaReference>(document.Paths["/generic-a"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema).Reference.Id,
                Assert.IsType<OpenApiSchemaReference>(document.Paths["/generic-b"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema).Reference.Id,
            };
            Assert.Equal(2, genericIds.Distinct(StringComparer.Ordinal).Count());
            Assert.All(genericIds, id => Assert.Matches("^[a-zA-Z0-9._-]+$", id));
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_DisambiguatedIdsFlowThroughOneOfAndAllOf()
    {
        var builder = CreateBuilder();
        builder.MapPost("/polymorphic", (InferredNaming.PolymorphicBase value) => { });
        builder.MapPost("/other-derived", (InferredNaming.Other.Derived value) => { });
        builder.MapPost("/inherited", (InferredNaming.Inheritance.Derived value) => { });
        builder.MapPost("/other-base", (InferredNaming.Other.Base value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var polymorphic = document.Components.Schemas[nameof(InferredNaming.PolymorphicBase)];
            Assert.Collection(
                polymorphic.Discriminator.Mapping,
                mapping => Assert.Equal(
                    Assert.IsType<OpenApiSchemaReference>(polymorphic.OneOf[0]).Reference.ReferenceV3,
                    mapping.Value.Reference.ReferenceV3),
                mapping => Assert.Equal(
                    Assert.IsType<OpenApiSchemaReference>(polymorphic.OneOf[1]).Reference.ReferenceV3,
                    mapping.Value.Reference.ReferenceV3));

            var derivedId = Assert.IsType<OpenApiSchemaReference>(
                document.Paths["/inherited"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema).Reference.Id;
            var baseReference = Assert.IsType<OpenApiSchemaReference>(document.Components.Schemas[derivedId].AllOf[0]);
            Assert.Equal("Inheritance.Base", baseReference.Reference.Id);
            Assert.Contains(baseReference.Reference.Id, document.Components.Schemas.Keys);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_DisambiguatesContextualPolymorphicIdsSymmetrically()
    {
        var builder = CreateBuilder();
        builder.MapPost("/ab", (InferredNaming.AB value) => { });
        builder.MapPost("/a", (InferredNaming.A value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var abBranch = Assert.IsType<OpenApiSchemaReference>(document.Components.Schemas["AB"].OneOf[0]).Reference.Id;
            var aBranch = Assert.IsType<OpenApiSchemaReference>(document.Components.Schemas["A"].OneOf[0]).Reference.Id;
            Assert.Equal("AB.C", abBranch);
            Assert.Equal("A.BC", aBranch);
            Assert.NotEqual(abBranch, aBranch);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_DisambiguatedMultiLevelPlaceholdersAreOrderIndependent()
    {
        var leafFirst = await CreateDocument(reverseEndpoints: false);
        var basesFirst = await CreateDocument(reverseEndpoints: true);

        var leafFirstSchemas = JsonNode.Parse(await leafFirst.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        var basesFirstSchemas = JsonNode.Parse(await basesFirst.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        Assert.Equal(leafFirstSchemas?.ToJsonString(), basesFirstSchemas?.ToJsonString());

        var leaf = leafFirst.Components.Schemas["Leaf"];
        Assert.Equal("Chain.Middle", Assert.IsType<OpenApiSchemaReference>(leaf.AllOf[0]).Reference.Id);
        var middle = leafFirst.Components.Schemas["Chain.Middle"];
        Assert.Equal("Chain.Base", Assert.IsType<OpenApiSchemaReference>(middle.AllOf[0]).Reference.Id);

        static async Task<OpenApiDocument> CreateDocument(bool reverseEndpoints)
        {
            var builder = CreateBuilder();
            if (reverseEndpoints)
            {
                builder.MapPost("/other-middle", (InferredNaming.Other.Middle value) => { });
                builder.MapPost("/other-base", (InferredNaming.Other.Base value) => { });
                builder.MapPost("/base", (InferredNaming.Chain.Base value) => { });
                builder.MapPost("/middle", (InferredNaming.Chain.Middle value) => { });
                builder.MapPost("/leaf", (InferredNaming.Chain.Leaf value) => { });
            }
            else
            {
                builder.MapPost("/leaf", (InferredNaming.Chain.Leaf value) => { });
                builder.MapPost("/middle", (InferredNaming.Chain.Middle value) => { });
                builder.MapPost("/base", (InferredNaming.Chain.Base value) => { });
                builder.MapPost("/other-base", (InferredNaming.Other.Base value) => { });
                builder.MapPost("/other-middle", (InferredNaming.Other.Middle value) => { });
            }

            return await VerifyOpenApiDocument(builder, CreateInferredOptions(), _ => { });
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_CustomReferenceIdCollisionThrows()
    {
        var builder = CreateBuilder();
        builder.MapPost("/first", (InferredNaming.First.Duplicate value) => { });
        builder.MapPost("/second", (InferredNaming.Second.Duplicate value) => { });
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = typeInfo =>
            typeInfo.Type == typeof(InferredNaming.First.Duplicate) ||
            typeInfo.Type == typeof(InferredNaming.Second.Duplicate)
                ? "Duplicate"
                : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));

        Assert.Contains("custom OpenAPI schema reference ID 'Duplicate'", exception.Message);
        Assert.Contains(typeof(InferredNaming.First.Duplicate).ToString(), exception.Message);
        Assert.Contains(typeof(InferredNaming.Second.Duplicate).ToString(), exception.Message);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_CustomUniqueAndNullReferenceIdsAreAuthoritative()
    {
        var builder = CreateBuilder();
        builder.MapPost("/first", (InferredNaming.First.Duplicate value) => { });
        builder.MapPost("/second", (InferredNaming.Second.Duplicate value) => { });
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = typeInfo => typeInfo.Type switch
        {
            var type when type == typeof(InferredNaming.First.Duplicate) => "FirstContract",
            var type when type == typeof(InferredNaming.Second.Duplicate) => null,
            _ => OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo),
        };

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal(
                "FirstContract",
                Assert.IsType<OpenApiSchemaReference>(document.Paths["/first"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema).Reference.Id);
            Assert.IsType<OpenApiSchema>(
                document.Paths["/second"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema);
            Assert.DoesNotContain(document.Components.Schemas.Keys, key => key.Contains("Second", StringComparison.Ordinal));
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("Invalid/Id")]
    public async Task SchemaGenerationMode_Inferred_InvalidCustomReferenceIdThrows(string referenceId)
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (InferredNaming.First.Duplicate value) => { });
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = typeInfo =>
            typeInfo.Type == typeof(InferredNaming.First.Duplicate)
                ? referenceId
                : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));

        Assert.Contains("must contain only ASCII letters", exception.Message);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_PreservesJsonPatchAliases()
    {
        var builder = CreateBuilder();
        builder.MapPatch("/untyped", (JsonPatchDocument value) => { });
        builder.MapPatch("/typed", (JsonPatchDocument<InferredNaming.PatchModel> value) => { });
        builder.MapPost("/other", (InferredNaming.JsonPatchDocument value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            Assert.Single(document.Components.Schemas, schema => schema.Key == "SystemTextJson.JsonPatchDocument");
            Assert.Equal(
                "SystemTextJson.JsonPatchDocument",
                Assert.IsType<OpenApiSchemaReference>(document.Paths["/untyped"].Operations[HttpMethod.Patch].RequestBody.Content["application/json-patch+json"].Schema).Reference.Id);
            Assert.Equal(
                "SystemTextJson.JsonPatchDocument",
                Assert.IsType<OpenApiSchemaReference>(document.Paths["/typed"].Operations[HttpMethod.Patch].RequestBody.Content["application/json-patch+json"].Schema).Reference.Id);
            Assert.Equal(
                "InferredNaming.JsonPatchDocument",
                Assert.IsType<OpenApiSchemaReference>(document.Paths["/other"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema).Reference.Id);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Legacy_PreservesCollidingComponentBehavior()
    {
        var builder = CreateBuilder();
        builder.MapPost("/first", (InferredNaming.First.Duplicate value) => { });
        builder.MapPost("/second", (InferredNaming.Second.Duplicate value) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.Equal(
                "Duplicate",
                Assert.IsType<OpenApiSchemaReference>(document.Paths["/first"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema).Reference.Id);
            Assert.Equal(
                "Duplicate",
                Assert.IsType<OpenApiSchemaReference>(document.Paths["/second"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema).Reference.Id);
            Assert.Single(document.Components.Schemas, schema => schema.Key == "Duplicate");
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_DoesNotRenameTransformerAuthoredComponents()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (InferredNaming.First.Duplicate value) => { });
        var options = CreateInferredOptions();
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.AddComponent("TransformerAuthored", new OpenApiSchema { Type = JsonSchemaType.String });
            document.AddComponent("TransformerContainer", new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchemaReference("TransformerAuthored", document),
                },
            });
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal(JsonSchemaType.String, document.Components.Schemas["TransformerAuthored"].Type);
            Assert.Equal(
                "TransformerAuthored",
                Assert.IsType<OpenApiSchemaReference>(document.Components.Schemas["TransformerContainer"].Properties["value"]).Reference.Id);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_TransformerRequestedTypesUseStableFallbackIds()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (InferredNaming.First.Duplicate value) => { });
        var options = CreateInferredOptions();
        options.AddOperationTransformer(async (operation, context, cancellationToken) =>
        {
            operation.Responses["400"] = new OpenApiResponse
            {
                Description = "Second duplicate",
                Content = new Dictionary<string, IOpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = await context.GetOrCreateSchemaAsync(
                            typeof(InferredNaming.Second.Duplicate),
                            cancellationToken: cancellationToken),
                    },
                },
            };
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Contains("Duplicate", document.Components.Schemas.Keys);
            var responseSchema = Assert.IsType<OpenApiSchema>(
                document.Paths["/"].Operations[HttpMethod.Post].Responses["400"].Content["application/json"].Schema);
            var referenceId = Assert.IsType<string>(
                responseSchema.Metadata[Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaId]);
            Assert.StartsWith("Duplicate-", referenceId, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_TransformerRequestedPolymorphicGraphUsesStableReferences()
    {
        var builder = CreateBuilder();
        builder.MapGet("/", () => { });
        var options = CreateInferredOptions();
        options.AddOperationTransformer(async (operation, context, cancellationToken) =>
        {
            operation.Responses["200"] = CreateResponse(
                await GetResolvedTransformerSchemaAsync(
                    context,
                    typeof(InferredNaming.PolymorphicBase),
                    cancellationToken));
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var rootReference = Assert.IsType<OpenApiSchemaReference>(
                document.Paths["/"].Operations[HttpMethod.Get].Responses["200"].Content["application/json"].Schema);
            var root = document.Components.Schemas[rootReference.Reference.Id];
            Assert.Equal(2, root.OneOf.Count);
            Assert.All(root.OneOf, branch =>
                Assert.Contains(Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id, document.Components.Schemas.Keys));
            Assert.Equal(
                root.OneOf.Select(branch => Assert.IsType<OpenApiSchemaReference>(branch).Reference.ReferenceV3),
                root.Discriminator.Mapping.Select(mapping => mapping.Value.Reference.ReferenceV3));
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_TransformerRequestedInheritanceGraphUsesStableBaseReference()
    {
        var builder = CreateBuilder();
        builder.MapGet("/", () => { });
        var options = CreateInferredOptions();
        options.AddOperationTransformer(async (operation, context, cancellationToken) =>
        {
            operation.Responses["200"] = CreateResponse(
                await GetResolvedTransformerSchemaAsync(
                    context,
                    typeof(InferredNaming.Inheritance.Derived),
                    cancellationToken));
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var derivedReference = Assert.IsType<OpenApiSchemaReference>(
                document.Paths["/"].Operations[HttpMethod.Get].Responses["200"].Content["application/json"].Schema);
            var derived = document.Components.Schemas[derivedReference.Reference.Id];
            var baseReference = Assert.IsType<OpenApiSchemaReference>(derived.AllOf[0]);
            Assert.Contains(baseReference.Reference.Id, document.Components.Schemas.Keys);
            Assert.StartsWith("Base-", baseReference.Reference.Id, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_LateCustomIdCollisionWithContextualPolymorphicIdThrows()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (InferredNaming.PolymorphicBase value) => { });
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = typeInfo => typeInfo.Type switch
        {
            var type when type == typeof(InferredNaming.First.Derived) => "FirstDerived",
            var type when type == typeof(InferredNaming.Second.Derived) => "SecondDerived",
            var type when type == typeof(InferredNaming.Other.Base) => "PolymorphicBaseFirstDerived",
            _ => OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo),
        };
        options.AddOperationTransformer(async (_, context, cancellationToken) =>
        {
            await context.GetOrCreateSchemaAsync(
                typeof(InferredNaming.Other.Base),
                cancellationToken: cancellationToken);
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));

        Assert.Contains("PolymorphicBaseFirstDerived", exception.Message);
        Assert.Contains("distinct serializer contract identities", exception.Message);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_TransformerRequestedGraphsAreOrderIndependent()
    {
        var first = await CreateDocument(reverse: false);
        var second = await CreateDocument(reverse: true);

        var firstSchemas = JsonNode.Parse(await first.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        var secondSchemas = JsonNode.Parse(await second.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        Assert.True(JsonNode.DeepEquals(firstSchemas, secondSchemas));

        static async Task<OpenApiDocument> CreateDocument(bool reverse)
        {
            var builder = CreateBuilder();
            builder.MapGet("/", () => { });
            var options = CreateInferredOptions();
            options.AddOperationTransformer(async (operation, context, cancellationToken) =>
            {
                var types = reverse
                    ? new[] { typeof(InferredNaming.Inheritance.Derived), typeof(InferredNaming.PolymorphicBase) }
                    : new[] { typeof(InferredNaming.PolymorphicBase), typeof(InferredNaming.Inheritance.Derived) };
                foreach (var type in types)
                {
                    var statusCode = type == typeof(InferredNaming.PolymorphicBase) ? "200" : "201";
                    operation.Responses[statusCode] = CreateResponse(
                        await GetResolvedTransformerSchemaAsync(context, type, cancellationToken));
                }
            });

            return await VerifyOpenApiDocument(builder, options, _ => { });
        }
    }

    private static OpenApiResponse CreateResponse(IOpenApiSchema schema)
        => new()
        {
            Description = "Response",
            Content = new Dictionary<string, IOpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType { Schema = schema },
            },
        };

    private static async Task<IOpenApiSchema> GetResolvedTransformerSchemaAsync(
        OpenApiOperationTransformerContext context,
        Type type,
        CancellationToken cancellationToken)
    {
        var schema = await context.GetOrCreateSchemaAsync(type, cancellationToken: cancellationToken);
        var schemaId = Assert.IsType<string>(
            schema.Metadata[Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaId]);
        return OpenApiSchemaService.ResolveReferenceForSchema(context.Document, schema, schemaId);
    }
}

#nullable enable

namespace InferredNaming
{
    [System.Text.Json.Serialization.JsonPolymorphic]
    [System.Text.Json.Serialization.JsonDerivedType(typeof(BC), "bc")]
    internal abstract class A;

    [System.Text.Json.Serialization.JsonPolymorphic]
    [System.Text.Json.Serialization.JsonDerivedType(typeof(C), "c")]
    internal abstract class AB;

    internal sealed class AAndB;
    internal sealed class BAndC;
    internal sealed class BC : A;
    internal sealed class C : AB;
    internal sealed class Pair<TFirst, TSecond>;
    internal sealed class PatchModel;
    internal sealed class JsonPatchDocument;

    internal sealed class CollisionContainer
    {
        public First.Duplicate[] Items { get; set; } = [];

        public Second.Duplicate? Optional { get; set; }
    }

    internal static class OuterA
    {
        internal sealed class Item;
    }

    internal static class OuterB
    {
        internal sealed class Item;
    }

    [System.Text.Json.Serialization.JsonPolymorphic]
    [System.Text.Json.Serialization.JsonDerivedType(typeof(First.Derived), "first")]
    [System.Text.Json.Serialization.JsonDerivedType(typeof(Second.Derived), "second")]
    internal abstract class PolymorphicBase;

    namespace First
    {
        internal sealed class Duplicate;
        internal sealed class Derived : PolymorphicBase;
    }

    namespace Second
    {
        internal sealed class Duplicate;
        internal sealed class Derived : PolymorphicBase;
    }

    namespace Inheritance
    {
        internal class Base
        {
            public string Value { get; set; } = string.Empty;
        }

        internal sealed class Derived : Base
        {
            public string Local { get; set; } = string.Empty;
        }
    }

    namespace Other
    {
        internal sealed class Base;
        internal sealed class Derived;
        internal sealed class Middle;
    }

    namespace Chain
    {
        internal class Base
        {
            public string BaseValue { get; set; } = string.Empty;
        }

        internal class Middle : Base
        {
            public string MiddleValue { get; set; } = string.Empty;
        }

        internal sealed class Leaf : Middle
        {
            public string LeafValue { get; set; } = string.Empty;
        }
    }
}
