// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class OpenApiSourceGeneratorTests
{
    [Fact]
    public async Task WorksWithSimpleSourceFileWithXmlComment()
    {
        var (generatorRunResult, compilation) = await RunGeneratorAsync("""
/// <summary>This is a test</summary>
/// <param name="datetime">Represents the current time.</param>
/// <response code="200">Returns a string indicating that the month is valid.</response>
/// <response code="404">Not found is the month is in the first half of the year.</response>
app.MapGet("/hello", Results<Ok<string>, NotFound> (DateTime datetime) => datetime.Month <= 6 ? TypedResults.NotFound() : TypedResults.Ok("Valid month"));
/// <summary>This is a another test</summary>
app.MapPost("/hello", () => TypedResults.Ok("Valid month"));
""");
        var results = Assert.IsType<GeneratorRunResult>(generatorRunResult);
    }

    private static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Preview).WithFeatures(new[] { new KeyValuePair<string, string>("InterceptorsPreview", "") });
    private static readonly Project _baseProject = CreateProject();

    private async Task<(GeneratorRunResult, Compilation)> RunGeneratorAsync(string sources, params string[] updatedSources)
    {
        // Create a Roslyn compilation for the syntax tree.
        var compilation = await CreateCompilationAsync(sources);

        // Configure the generator driver and run
        // the compilation with it if the generator
        // is enabled.
        var generator = new OpenApiSourceGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var _);
        foreach (var updatedSource in updatedSources)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(GetMapActionString(updatedSource), path: $"TestMapActions.cs", options: ParseOptions);
            compilation = compilation
                .ReplaceSyntaxTree(compilation.SyntaxTrees.First(), syntaxTree);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out updatedCompilation,
                out var _);
        }
        var diagnostics = updatedCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));
        var runResult = driver.GetRunResult();

        return (Assert.Single(runResult.Results), updatedCompilation);
    }

    internal static string GetMapActionString(string sources) => $$"""
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

var app = WebApplication.Create();

{{sources}}

app.Run();
""";
    private static Task<Compilation> CreateCompilationAsync(string sources)
    {
        var source = GetMapActionString(sources);
        var project = _baseProject.AddDocument("Program.cs", SourceText.From(source, Encoding.UTF8)).Project;
        // Create a Roslyn compilation for the syntax tree.
        return project.GetCompilationAsync();
    }

    internal static Project CreateProject(Func<CSharpCompilationOptions, CSharpCompilationOptions> modifyCompilationOptions = null)
    {
        var projectName = $"TestProject-{Guid.NewGuid()}";
        var compilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            .WithNullableContextOptions(NullableContextOptions.Enable);
        if (modifyCompilationOptions is not null)
        {
            compilationOptions = modifyCompilationOptions(compilationOptions);
        }
        var project = new AdhocWorkspace().CurrentSolution
            .AddProject(projectName, projectName, LanguageNames.CSharp)
            .WithCompilationOptions(compilationOptions)
            .WithParseOptions(ParseOptions);

        // Add in required metadata references
        var resolver = new AppLocalResolver();
        var dependencyContext = DependencyContext.Load(typeof(OpenApiSourceGeneratorTests).Assembly);

        Assert.NotNull(dependencyContext);

        foreach (var defaultCompileLibrary in dependencyContext.CompileLibraries)
        {
            foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(resolver))
            {
                // Skip the source generator itself
                if (resolveReferencePath.Equals(typeof(OpenApiSourceGenerator).Assembly.Location, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(resolveReferencePath));
            }
        }

        return project;
    }

    private sealed class AppLocalResolver : ICompilationAssemblyResolver
    {
        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            foreach (var assembly in library.Assemblies)
            {
                var dll = Path.Combine(Directory.GetCurrentDirectory(), "refs", Path.GetFileName(assembly));
                if (File.Exists(dll))
                {
                    assemblies ??= new();
                    assemblies.Add(dll);
                    return true;
                }

                dll = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(assembly));
                if (File.Exists(dll))
                {
                    assemblies ??= new();
                    assemblies.Add(dll);
                    return true;
                }
            }

            return false;
        }
    }
}
