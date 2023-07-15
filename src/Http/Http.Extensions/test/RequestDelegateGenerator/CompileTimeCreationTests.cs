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
        var source = GetMapActionString("""app.MapGet("/", (string name) => "Hello {name}!");app.MapGet("/bye", (string name) => "Bye {name}!");""");
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
    public async Task SupportsDifferentInterceptorsFromSameLocation()
    {
        var project = CreateProject();
        var source = GetMapActionString("""app.MapGet("/", (string name) => "Hello {name}!");""");
        var otherSource = GetMapActionString("""app.MapGet("/", (int age) => "Hello {age}!");""", "OtherTestMapActions");
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmitsDiagnosticForUnsupportedAnonymousMethod(bool isAsync)
    {
        var source = isAsync
            ? @"app.MapGet(""/hello"", async (int value) => await Task.FromResult(new { Delay = value }));"
            : @"app.MapGet(""/hello"", (int value) => new { Delay = value });";
        var (generatorRunResult, compilation) = await RunGeneratorAsync(source);

        // Emits diagnostic but generates no source
        var result = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveAnonymousReturnType.Id, diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public async Task EmitsDiagnosticForGenericTypeParam()
    {
        var source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class RouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTestEndpoints<T>(this IEndpointRouteBuilder app) where T : class
    {
        app.MapGet("/", (T value) => "Hello world!");
        app.MapGet("/", () => new T());
        app.MapGet("/", (Wrapper<T> value) => "Hello world!");
        app.MapGet("/", async () =>
        {
            await Task.CompletedTask;
            return new T();
        });
        return app;
    }
}

file class Wrapper<T> { }
""";
        var project = CreateProject();
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var diagnostics);
        var generatorRunResult = driver.GetRunResult();

        // Emits diagnostic but generates no source
        var result = Assert.IsType<GeneratorRunResult>(Assert.Single(generatorRunResult.Results));
        Assert.Empty(result.GeneratedSources);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(DiagnosticDescriptors.TypeParametersNotSupported.Id, diagnostic.Id));
    }

    [Theory]
    [InlineData("protected")]
    [InlineData("private")]
    public async Task EmitsDiagnosticForPrivateOrProtectedTypes(string accessibility)
    {
        var source = $$"""
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class RouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTestEndpoints<T>(this IEndpointRouteBuilder app) where T : class
    {
        app.MapGet("/", (MyType value) => "Hello world!");
        app.MapGet("/", () => new MyType());
        app.MapGet("/", (Wrapper<MyType> value) => "Hello world!");
        app.MapGet("/", async () =>
        {
            await Task.CompletedTask;
            return new MyType();
        });
        return app;
    }

    {{accessibility}} class MyType { }
}

public class Wrapper<T> { }
""";
        var project = CreateProject();
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var diagnostics);
        var generatorRunResult = driver.GetRunResult();

        // Emits diagnostic but generates no source
        var result = Assert.IsType<GeneratorRunResult>(Assert.Single(generatorRunResult.Results));
        Assert.Empty(result.GeneratedSources);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(DiagnosticDescriptors.InaccessibleTypesNotSupported.Id, diagnostic.Id));
    }

    [Fact]
    public async Task HandlesEndpointsWithAndWithoutDiagnostics()
    {
        var source = $$"""
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class TestMapActions
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/a", (MyType? value) => "Hello world!");
        app.MapGet("/b", () => "Hello world!");
        app.MapPost("/c", (Wrapper<MyType>? value) => "Hello world!");
        return app;
    }

    private class MyType { }
}

public class Wrapper<T> { }
""";
        var project = CreateProject();
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var diagnostics);
        var generatorRunResult = driver.GetRunResult();

        // Emits diagnostic and generates source for all endpoints
        var result = Assert.IsType<GeneratorRunResult>(Assert.Single(generatorRunResult.Results));
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(DiagnosticDescriptors.InaccessibleTypesNotSupported.Id, diagnostic.Id));

        // All endpoints can be invoked
        var endpoints = GetEndpointsFromCompilation(updatedCompilation, skipGeneratedCodeCheck: true);
        foreach (var endpoint in endpoints)
        {
            var httpContext = CreateHttpContext();
            await endpoint.RequestDelegate(httpContext);
            await VerifyResponseBodyAsync(httpContext, "Hello world!");
        }

        await VerifyAgainstBaselineUsingFile(updatedCompilation);
    }
}
