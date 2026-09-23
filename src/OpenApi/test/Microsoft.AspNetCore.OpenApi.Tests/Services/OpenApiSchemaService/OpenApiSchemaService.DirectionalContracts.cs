// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

#nullable disable

public partial class OpenApiSchemaServiceTests
{
    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task SchemaGenerationMode_Inferred_UsesDirectionalObjectContracts(OpenApiSpecVersion openApiVersion)
    {
        var builder = CreateBuilder();
        builder.MapPost("/models", (DirectionalModel model) => model);

        var options = CreateInferredOptions();
        options.OpenApiVersion = openApiVersion;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = document.Paths["/models"].Operations[HttpMethod.Post];
            var inputReference = Assert.IsType<OpenApiSchemaReference>(
                operation.RequestBody.Content["application/json"].Schema);
            var outputReference = Assert.IsType<OpenApiSchemaReference>(
                operation.Responses["200"].Content["application/json"].Schema);
            Assert.Equal("DirectionalModel.Input", inputReference.Reference.Id);
            Assert.Equal("DirectionalModel.Output", outputReference.Reference.Id);

            var input = document.Components.Schemas[inputReference.Reference.Id];
            Assert.Equal(
                ["both", "inputOnly", "nullableInput", "requiredButOmittable"],
                input.Properties.Keys.Order(StringComparer.Ordinal));
            Assert.Equal(["requiredButOmittable"], input.Required);
            Assert.True(input.Properties["nullableInput"].Type.HasValue &&
                input.Properties["nullableInput"].Type.Value.HasFlag(JsonSchemaType.Null));

            var output = document.Components.Schemas[outputReference.Reference.Id];
            Assert.Equal(
                ["both", "nullableInput", "outputOnly", "requiredButOmittable"],
                output.Properties.Keys.Order(StringComparer.Ordinal));
            Assert.Null(output.Required);
            Assert.False(output.Properties["nullableInput"].Type.HasValue &&
                output.Properties["nullableInput"].Type.Value.HasFlag(JsonSchemaType.Null));
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task SchemaGenerationMode_Inferred_RespectsRequiredConstructorParameterOption(
        bool respectRequiredConstructorParameters,
        bool expectedRequired)
    {
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.RespectRequiredConstructorParameters = respectRequiredConstructorParameters);
        var builder = CreateBuilder(services);
        builder.MapPost("/", (ConstructorContract model) => model);

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var requestReference = Assert.IsType<OpenApiSchemaReference>(
                document.Paths["/"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema);
            var responseReference = Assert.IsType<OpenApiSchemaReference>(
                document.Paths["/"].Operations[HttpMethod.Post].Responses["200"].Content["application/json"].Schema);
            var request = document.Components.Schemas[requestReference.Reference.Id];
            var response = document.Components.Schemas[responseReference.Reference.Id];

            Assert.Equal(expectedRequired, request.Required?.Contains("value") ?? false);
            Assert.Null(response.Required);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_RecursiveDirectionalReferencesRemainIsolated()
    {
        var first = await CreateDocument(reverseEndpoints: false);
        var second = await CreateDocument(reverseEndpoints: true);

        Assert.Equal(first.Components.Schemas.Keys, second.Components.Schemas.Keys);
        Assert.Equal(["DirectionalNode.Input", "DirectionalNode.Output"], first.Components.Schemas.Keys);
        AssertRecursiveReference(first, "DirectionalNode.Input");
        AssertRecursiveReference(first, "DirectionalNode.Output");

        static async Task<OpenApiDocument> CreateDocument(bool reverseEndpoints)
        {
            var builder = CreateBuilder();
            if (reverseEndpoints)
            {
                builder.MapGet("/nodes", () => new DirectionalNode());
                builder.MapPost("/nodes", (DirectionalNode node) => { });
            }
            else
            {
                builder.MapPost("/nodes", (DirectionalNode node) => { });
                builder.MapGet("/nodes", () => new DirectionalNode());
            }

            return await VerifyOpenApiDocument(builder, CreateInferredOptions(), _ => { });
        }

        static void AssertRecursiveReference(OpenApiDocument document, string componentId)
        {
            var next = document.Components.Schemas[componentId].Properties["next"];
            Assert.Collection(
                next.OneOf,
                branch => Assert.Equal(JsonSchemaType.Null, branch.Type),
                branch => Assert.Equal(componentId, Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_TransformerRequestedSchemaUsesNeutralContract()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectionalModel model) => model);
        OpenApiSchema neutralSchema = null;
        var options = CreateInferredOptions();
        options.AddOperationTransformer(async (_, context, cancellationToken) =>
        {
            neutralSchema = await context.GetOrCreateSchemaAsync(
                typeof(DirectionalModel),
                cancellationToken: cancellationToken);
        });

        await VerifyOpenApiDocument(builder, options, _ => { });

        Assert.NotNull(neutralSchema);
        Assert.Equal(
            ["both", "inputOnly", "nullableInput", "outputOnly", "requiredButOmittable"],
            neutralSchema.Properties.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void InferredShape_DirectionalFactsMatchReflectionAndSourceGeneration()
    {
        var reflectionOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        var generatedOptions = new JsonSerializerOptions(DirectionalJsonContext.Default.Options);

        foreach (var purpose in Enum.GetValues<InferredSchemaPurpose>())
        {
            var reflection = InferredSchemaShapeBuilder.Build(reflectionOptions, typeof(DirectionalModel), purpose);
            var generated = InferredSchemaShapeBuilder.Build(generatedOptions, typeof(DirectionalModel), purpose);

            Assert.Equal(reflection.Purpose, generated.Purpose);
            Assert.Equal(
                reflection[typeof(DirectionalModel)].Properties.Select(ToComparableFact),
                generated[typeof(DirectionalModel)].Properties.Select(ToComparableFact));
        }

        static object ToComparableFact(InferredSchemaProperty property) => new
        {
            property.Identity.MemberName,
            property.Identity.JsonName,
            property.PropertyType.AllowsNull,
            property.IsRequired,
        };
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_UsesExplicitPopulateContractForInput()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (PopulationContract model) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var requestReference = Assert.IsType<OpenApiSchemaReference>(
                document.Paths["/"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema);
            Assert.Contains("values", document.Components.Schemas[requestReference.Reference.Id].Properties.Keys);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_TransformerTraversalUsesDirectionalProperties()
    {
        var rootPropertySets = new List<string>();
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectionalModel model) => model);
        var options = CreateInferredOptions();
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type == typeof(DirectionalModel) &&
                context.JsonPropertyInfo is null &&
                schema.Properties is not null)
            {
                rootPropertySets.Add(string.Join(",", schema.Properties.Keys.Order(StringComparer.Ordinal)));
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, _ => { });

        Assert.Equal(
            [
                "both,inputOnly,nullableInput,requiredButOmittable",
                "both,nullableInput,outputOnly,requiredButOmittable",
            ],
            rootPropertySets.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task SchemaGenerationMode_Legacy_ReusesVersionNeutralContract(OpenApiSpecVersion openApiVersion)
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectionalModel model) => model);
        var options = new OpenApiOptions { OpenApiVersion = openApiVersion };

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestReference = Assert.IsType<OpenApiSchemaReference>(
                operation.RequestBody.Content["application/json"].Schema);
            var responseReference = Assert.IsType<OpenApiSchemaReference>(
                operation.Responses["200"].Content["application/json"].Schema);
            Assert.Equal(nameof(DirectionalModel), requestReference.Reference.Id);
            Assert.Equal(requestReference.Reference.Id, responseReference.Reference.Id);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_DisambiguatesCrossPurposeNameCollisions()
    {
        var builder = CreateBuilder();
        builder.MapPost(
            "/",
            (InferredNaming.First.Duplicate model) => new InferredNaming.Second.Duplicate());

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            Assert.Equal(
                "Duplicate.Input",
                Assert.IsType<OpenApiSchemaReference>(
                    operation.RequestBody.Content["application/json"].Schema).Reference.Id);
            Assert.Equal(
                "Duplicate.Output",
                Assert.IsType<OpenApiSchemaReference>(
                    operation.Responses["200"].Content["application/json"].Schema).Reference.Id);
            Assert.Equal(["Duplicate.Input", "Duplicate.Output"], document.Components.Schemas.Keys);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_RejectsCrossPurposeCustomIdCollisions()
    {
        var builder = CreateBuilder();
        builder.MapPost(
            "/",
            (InferredNaming.First.Duplicate model) => new InferredNaming.Second.Duplicate());
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = _ => "Contract";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));

        Assert.Contains("distinct serializer contract identities", exception.Message);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_RejectsCustomIdForDivergentDirectionalContract()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectionalModel model) => model);
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = typeInfo =>
            typeInfo.Type == typeof(DirectionalModel)
                ? "DirectionalContract"
                : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));

        Assert.Contains("'DirectionalContract'", exception.Message);
        Assert.Contains(typeof(DirectionalModel).ToString(), exception.Message);
        Assert.Contains("cannot distinguish the schema purpose", exception.Message);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_SharesUnchangedCustomIdForIdenticalDirectionalContract()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (IdenticalDirectionalModel model) => model);
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = typeInfo =>
            typeInfo.Type == typeof(IdenticalDirectionalModel)
                ? "AuthoritativeContract"
                : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var inputReference = Assert.IsType<OpenApiSchemaReference>(
                operation.RequestBody.Content["application/json"].Schema);
            var outputReference = Assert.IsType<OpenApiSchemaReference>(
                operation.Responses["200"].Content["application/json"].Schema);

            Assert.Equal("AuthoritativeContract", inputReference.Reference.Id);
            Assert.Equal(inputReference.Reference.Id, outputReference.Reference.Id);
            Assert.Single(document.Components.Schemas);
            Assert.Contains("AuthoritativeContract", document.Components.Schemas.Keys);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_RejectsParentCustomIdForTransitiveDirectionalDifference()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectionalParent model) => model);
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = typeInfo => typeInfo.Type switch
        {
            var type when type == typeof(DirectionalParent) => "ParentContract",
            var type when type == typeof(DirectionalChild) => "ChildContract",
            _ => OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo),
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));

        Assert.Contains("'ParentContract'", exception.Message);
        Assert.Contains(typeof(DirectionalParent).ToString(), exception.Message);
        Assert.Contains("cannot distinguish the schema purpose", exception.Message);
    }

#nullable enable

    private sealed class DirectionalModel
    {
        private string? _inputOnly;
        private string? _nullableInput;

        public string Both { get; set; } = string.Empty;

        public string OutputOnly { get; } = string.Empty;

        public string? InputOnly
        {
            set => _inputOnly = value;
        }

        [AllowNull]
        public string NullableInput
        {
            get => _nullableInput ?? string.Empty;
            set => _nullableInput = value;
        }

        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RequiredButOmittable { get; set; }

        [JsonIgnore]
        public string Ignored { get; set; } = string.Empty;
    }

    private sealed class ConstructorContract
    {
        [JsonConstructor]
        public ConstructorContract(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    private sealed class DirectionalNode
    {
        public DirectionalNode? Next { get; set; }

        public string OutputOnly { get; } = string.Empty;
    }

    private sealed class IdenticalDirectionalModel
    {
        public string? Value { get; set; }
    }

    private sealed class DirectionalParent
    {
        public DirectionalChild? Child { get; set; }
    }

    private sealed class DirectionalChild
    {
        private string? _inputOnly;

        public string OutputOnly { get; } = string.Empty;

        public string? InputOnly
        {
            set => _inputOnly = value;
        }
    }

    private sealed class PopulationContract
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public List<string> Values { get; } = [];
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(DirectionalModel))]
    private sealed partial class DirectionalJsonContext : JsonSerializerContext;

#nullable disable
}
