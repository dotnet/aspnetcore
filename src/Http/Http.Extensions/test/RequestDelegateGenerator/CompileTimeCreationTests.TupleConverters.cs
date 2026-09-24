// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class CompileTimeCreationTests
{
    [Fact]
    public async Task GeneratesDeterministicTupleConverterManifestFromRdgEndpoints()
    {
        var generated = await GenerateWithOpenApiAsync("""
app.MapGet("/value", () => (1, "two"));
app.MapPost("/reference", (Tuple<bool, long> value) => value);
""");

        var boolLong = "JsonArrayTupleConverters.CreateTuple<bool, long>()";
        var intString = "JsonArrayTupleConverters.CreateValueTuple<int, string>()";
        Assert.Contains(boolLong, generated);
        Assert.Contains(intString, generated);
        Assert.True(generated.IndexOf(boolLong, StringComparison.Ordinal) < generated.IndexOf(intString, StringComparison.Ordinal));
        Assert.Contains("candidate is global::Microsoft.AspNetCore.OpenApi.JsonArrayTupleConverter", generated);
        Assert.Contains("!options.Converters.Any(candidate => candidate.CanConvert(typeof(TTuple)))", generated);
        Assert.True(
            generated.IndexOf("RegisterGeneratedTupleConverters(tupleJsonOptions.SerializerOptions);", StringComparison.Ordinal) <
            generated.IndexOf("MetadataPopulator populateMetadata", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GeneratesTupleConvertersForBoundedNestedDtoProperties()
    {
        var project = CreateProject(includeOpenApi: true);
        project = project.AddDocument(
            "TestMapActions.cs",
            SourceText.From(GetMapActionString("""app.MapGet("/", () => new TupleResponse());"""), Encoding.UTF8)).Project;
        project = project.AddDocument(
            "TupleResponse.cs",
            SourceText.From("""
public sealed class TupleResponse
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]
    public (int, string) Value { get; set; }
}
""", Encoding.UTF8)).Project;

        var generated = await GenerateAsync(project);

        Assert.Contains(
            "JsonArrayTupleConverters.CreateValueTuple<int, string>()",
            generated);
    }

    [Fact]
    public async Task GeneratesTupleConvertersForJsonBodiesNestedInAsParameters()
    {
        var project = CreateProject(includeOpenApi: true);
        project = project.AddDocument(
            "TestMapActions.cs",
            SourceText.From(GetMapActionString("""
app.MapPost("/", ([Microsoft.AspNetCore.Http.AsParameters] TupleParameters parameters) => parameters.Value);
"""), Encoding.UTF8)).Project;
        project = project.AddDocument(
            "TupleParameters.cs",
            SourceText.From("""
public sealed class TupleParameters
{
    [Microsoft.AspNetCore.Mvc.FromBody]
    public (int, string) Value { get; set; }
}
""", Encoding.UTF8)).Project;

        var generated = await GenerateAsync(project);

        Assert.Contains(
            "JsonArrayTupleConverters.CreateValueTuple<int, string>()",
            generated);
    }

    [Fact]
    public async Task OpenApiReferenceWithoutTupleContractsEmitsNoTupleFactory()
    {
        var generated = await GenerateWithOpenApiAsync("""app.MapGet("/", () => "Hello");""");

        Assert.Contains("RegisterGeneratedTupleConverters", generated);
        Assert.DoesNotContain("JsonArrayTupleConverters.Create", generated);
    }

    [Fact]
    public async Task GeneratesComposedConverterForLongTupleWithoutRegisteringRestAsAContract()
    {
        var generated = await GenerateWithOpenApiAsync("""
app.MapGet("/", () => (1, 2, 3, 4, 5, 6, 7, 8, 9));
""");

        Assert.Contains(
            "JsonArrayTupleConverters.CreateValueTuple<int, int, int, int, int, int, int, (int, int)>(global::Microsoft.AspNetCore.OpenApi.JsonArrayTupleConverters.CreateValueTuple<int, int>())",
            generated);
        Assert.Equal(1, CountOccurrences(generated, "JsonArrayTupleConverters.CreateValueTuple<int, int>()"));
    }

    [Fact]
    public async Task GeneratesConvertersForNestedTupleElementsInsideRest()
    {
        var generated = await GenerateWithOpenApiAsync("""
app.MapGet("/value", () => (1, 2, 3, 4, 5, 6, 7, ("eight", true), 9));
app.MapGet("/reference", () => new Tuple<int, int, int, int, int, int, int, Tuple<Tuple<string, bool>, int>>(
    1, 2, 3, 4, 5, 6, 7, new Tuple<Tuple<string, bool>, int>(new Tuple<string, bool>("eight", true), 9)));
""");

        Assert.Equal(1, CountOccurrences(generated, "JsonArrayTupleConverters.CreateValueTuple<string, bool>()"));
        Assert.Equal(1, CountOccurrences(generated, "JsonArrayTupleConverters.CreateTuple<string, bool>()"));
        Assert.Equal(1, CountOccurrences(generated, "JsonArrayTupleConverters.CreateValueTuple<(string, bool), int>()"));
        Assert.Equal(1, CountOccurrences(generated, "JsonArrayTupleConverters.CreateTuple<global::System.Tuple<string, bool>, int>()"));
    }

    [Fact]
    public async Task GeneratesConvertersForDtoTuplePropertiesInsideRest()
    {
        var project = CreateProject(includeOpenApi: true);
        project = project.AddDocument(
            "TestMapActions.cs",
            SourceText.From(GetMapActionString("""
app.MapGet("/", () => (1, 2, 3, 4, 5, 6, 7, new TailDto(), 9));
"""), Encoding.UTF8)).Project;
        project = project.AddDocument(
            "TailDto.cs",
            SourceText.From("""
public sealed class TailDto
{
    public (short, decimal) Value { get; set; }
}
""", Encoding.UTF8)).Project;

        var generated = await GenerateAsync(project);

        Assert.Equal(1, CountOccurrences(generated, "JsonArrayTupleConverters.CreateValueTuple<short, decimal>()"));
        Assert.Equal(1, CountOccurrences(generated, "JsonArrayTupleConverters.CreateValueTuple<global::TailDto, int>()"));
    }

    [Fact]
    public async Task OmitsTupleRegistrationWithoutOpenApiReference()
    {
        var generated = await GenerateAsync(CreateProject().AddDocument(
            "TestMapActions.cs",
            SourceText.From(GetMapActionString("""app.MapGet("/", () => (1, "two"));"""), Encoding.UTF8)).Project);

        Assert.DoesNotContain("RegisterGeneratedTupleConverters", generated);
        Assert.DoesNotContain("JsonArrayTupleConverters.Create", generated);
    }

#pragma warning disable ASP0040 // Tests exercise experimental OpenAPI APIs.
    [Fact]
    public async Task GeneratedTupleConverters_RequireExplicitOptIn()
    {
        var compilation = await GenerateCompilationAsync(CreateProject(includeOpenApi: true).AddDocument(
            "TestMapActions.cs",
            SourceText.From(GetMapActionString("""app.MapGet("/", () => (1, "two"));"""), Encoding.UTF8)).Project);
        using var services = CreateServiceProvider();
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: services);
        var httpContext = CreateHttpContext(services);

        await endpoint.RequestDelegate(httpContext);

        await VerifyResponseBodyAsync(httpContext, "{}");
    }

    [Fact]
    public async Task GeneratedTupleConverters_UseExplicitFactoryOptIn()
    {
        var compilation = await GenerateCompilationAsync(CreateProject(includeOpenApi: true).AddDocument(
            "TestMapActions.cs",
            SourceText.From(GetMapActionString("""app.MapGet("/", () => (1, "two"));"""), Encoding.UTF8)).Project);
        using var services = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.Converters.Add(new JsonArrayTupleConverter()));
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: services);
        var httpContext = CreateHttpContext(services);

        await endpoint.RequestDelegate(httpContext);

        await VerifyResponseBodyAsync(httpContext, """[1,"two"]""");
    }

    [Fact]
    public async Task GeneratedTupleConverters_PreserveClosedConverterAndFrozenOptions()
    {
        var compilation = await GenerateCompilationAsync(CreateProject(includeOpenApi: true).AddDocument(
            "TestMapActions.cs",
            SourceText.From(GetMapActionString("""app.MapGet("/", () => (1, "two"));"""), Encoding.UTF8)).Project);
        using var services = CreateServiceProvider(serviceCollection =>
            serviceCollection.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, string>());
                options.SerializerOptions.MakeReadOnly();
            }));
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: services);
        var httpContext = CreateHttpContext(services);

        await endpoint.RequestDelegate(httpContext);

        await VerifyResponseBodyAsync(httpContext, """[1,"two"]""");
    }

    [Fact]
    public async Task GeneratedTupleConverters_PreserveUserConverterPrecedence()
    {
        var compilation = await GenerateCompilationAsync(CreateProject(includeOpenApi: true).AddDocument(
            "TestMapActions.cs",
            SourceText.From(GetMapActionString("""app.MapGet("/", () => (1, "two"));"""), Encoding.UTF8)).Project);
        using var services = CreateServiceProvider(serviceCollection =>
            serviceCollection.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new CustomTupleConverter());
                options.SerializerOptions.Converters.Add(new JsonArrayTupleConverter());
            }));
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: services);
        var httpContext = CreateHttpContext(services);

        await endpoint.RequestDelegate(httpContext);

        await VerifyResponseBodyAsync(httpContext, "\"custom\"");
    }
#pragma warning restore ASP0040

    private static async Task<string> GenerateWithOpenApiAsync(string sources)
    {
        var project = CreateProject(includeOpenApi: true);
        project = project.AddDocument(
            "TestMapActions.cs",
            SourceText.From(GetMapActionString(sources), Encoding.UTF8)).Project;
        return await GenerateAsync(project);
    }

    private static async Task<string> GenerateAsync(Project project)
    {
        var updatedCompilation = await GenerateCompilationAsync(project);
        var generatedSources = updatedCompilation.SyntaxTrees
            .Where(tree => tree.FilePath.EndsWith(".g.cs", StringComparison.Ordinal))
            .OrderBy(tree => tree.FilePath, StringComparer.Ordinal)
            .Select(tree => tree.GetText().ToString());
        return string.Join(Environment.NewLine, generatedSources);
    }

    private static async Task<Compilation> GenerateCompilationAsync(Project project)
    {
        var compilation = await project.GetCompilationAsync();
        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator],
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation!, out var updatedCompilation, out _);

        Assert.Empty(updatedCompilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning));
        return updatedCompilation;
    }

    private static int CountOccurrences(string value, string substring)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(substring, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }

    private sealed class CustomTupleConverter : JsonConverter<(int, string)>
    {
        public override (int, string) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, (int, string) value, JsonSerializerOptions options)
            => writer.WriteStringValue("custom");
    }
}
