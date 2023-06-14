// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class CompileTimeCreationTests : RequestDelegateCreationTests
{
    protected override bool IsGeneratorEnabled { get; } = true;

    [Fact]
    public async Task MapGet_WithRequestDelegate_DoesNotGenerateSources()
    {
        var (generatorRunResult, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (HttpContext context) => Task.CompletedTask);
""");
        var results = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        Assert.Empty(GetStaticEndpoints(results, GeneratorSteps.EndpointModelStep));

        var endpoint = GetEndpointFromCompilation(compilation, false);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "");
    }

    [Fact]
    public async Task MapAction_ExplicitRouteParamWithInvalidName_SimpleReturn()
    {
        var source = $$"""app.MapGet("/{routeValue}", ([FromRoute(Name = "invalidName" )] string parameterName) => parameterName);""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => endpoint.RequestDelegate(httpContext));
        Assert.Equal("'invalidName' is not a route parameter.", exception.Message);
    }

    [Fact]
    public async Task SupportsSameInterceptorsFromDifferentFiles()
    {
        var project = CreateProject();
        var source = GetMapActionString("""app.MapGet("/", (string name) => "Hello {name}!");""");
        var otherSource = GetMapActionString("""app.MapGet("/", (string name) => "Hello {name}!");""", "OtherTestMapActions");
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        project = project.AddDocument("OtherTestMapActions.cs", SourceText.From(otherSource, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var _);

        var diagnostics = updatedCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));

        await VerifyAgainstBaselineUsingFile(updatedCompilation);
    }

    [Fact]
    public async Task SourceMapsAllPathsInAttribute()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var mappedDirectory = Path.Combine(currentDirectory, "path", "mapped");
        var project = CreateProject(modifyCompilationOptions:
            (options) =>
            {
                return options.WithSourceReferenceResolver(
                    new SourceFileResolver(ImmutableArray<string>.Empty, currentDirectory, ImmutableArray.Create(new KeyValuePair<string, string>(currentDirectory, mappedDirectory))));
            });
        var source = GetMapActionString("""app.MapGet("/", () => "Hello world!");""");
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8), filePath: Path.Combine(currentDirectory, "TestMapActions.cs")).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var diags);

        var diagnostics = updatedCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));

        var endpoint = GetEndpointFromCompilation(updatedCompilation);
        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
    }
}
