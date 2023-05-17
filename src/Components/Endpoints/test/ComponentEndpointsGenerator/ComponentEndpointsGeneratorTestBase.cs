// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints.Generator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests;

public abstract class ComponentEndpointsGeneratorTestBase : LoggedTest
{
    public bool RegenerateBaselines => bool.TryParse(Environment.GetEnvironmentVariable("ASPNETCORE_TEST_BASELINES"), out var result) && result;

    protected abstract bool IsGeneratorEnabled { get; }

    protected static List<MetadataReference> _metadataReferences = ResolveMetadataReferences();

    protected static Project _baseProject = CreateProject();

    internal async Task<(GeneratorRunResult?, Compilation)> RunGeneratorAsync(string sources, Project project = null, params string[] updatedSources)
    {
        // Create a Roslyn compilation for the syntax tree.
        var compilation = await CreateCompilationAsync(sources, project);

        // Return the compilation immediately if
        // the generator is not enabled.
        if (!IsGeneratorEnabled)
        {
            return (null, compilation);
        }

        // Configure the generator driver and run
        // the compilation with it if the generator
        // is enabled.
        var generator = new ComponentEndpointsGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);
        var diagnostics = updatedCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));
        var runResult = driver.GetRunResult();

        return (Assert.Single(runResult.Results), updatedCompilation);
    }

    private static Task<Compilation> CreateCompilationAsync(string sources, Project existing)
    {
        var project = (existing ?? _baseProject);
        if (sources != null)
        {
            project = project.AddDocument("TestComponent.cs", SourceText.From(sources, Encoding.UTF8)).Project;
        }
        
        // Create a Roslyn compilation for the syntax tree.
        return project.GetCompilationAsync();
    }

    protected static async Task<(MetadataReference, byte[])> CreateClassLibraryAsync(string name, string source, MetadataReference[] references = null)
    {
        var project = CreateProject(name);
        project = project.AddMetadataReferences(references ?? Array.Empty<MetadataReference>());
        project = project.AddDocument("LibraryComponent.cs", SourceText.From(source)).Project;
        var compilation = await project.GetCompilationAsync();
        var memoryStream = new MemoryStream();
        var result = compilation.Emit(memoryStream);
        Assert.True(result.Success);
        memoryStream.Position = 0;
        var library = MetadataReference.CreateFromStream(memoryStream);
        return (library, memoryStream.ToArray());
    }

    protected static Project CreateProject(string name = null, MetadataReference[] references = null, ProjectReference[] projectReferences = null)
    {
        var projectName = name ?? $"TestProject-{Guid.NewGuid()}";
        var project = new AdhocWorkspace().CurrentSolution
            .AddProject(projectName, projectName, LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp11));

        project = references != null ? project.AddMetadataReferences(references) : project;
        project = projectReferences != null ? project.AddProjectReferences(projectReferences) : project;

        return project.AddMetadataReferences(_metadataReferences);
    }

    private static List<MetadataReference> ResolveMetadataReferences()
    {
        // Add in required metadata references
        var resolver = new AppLocalResolver();
        var dependencyContext = DependencyContext.Load(typeof(ComponentEndpointsGeneratorTestBase).Assembly);

        Assert.NotNull(dependencyContext);

        var metadataReferences = new List<MetadataReference>();
        foreach (var defaultCompileLibrary in dependencyContext.CompileLibraries)
        {
            if (!defaultCompileLibrary.Name.Contains("Microsoft.Extensions", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("System", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("Microsoft.JSInterop", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("BlazorUnitedApp", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("mscorlib", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("Microsoft.Win32.Primitives", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("Microsoft.Win32.Registry", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("Microsoft.CSharp", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("Microsoft.CSharp.Reference", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("Microsoft.Net", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase) &&
                !defaultCompileLibrary.Name.Contains("netstandard", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(resolver))
            {
                // Skip the source generator itself
                if (resolveReferencePath.Equals(typeof(ComponentEndpointsGenerator).Assembly.Location, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // Skip the test project
                if (resolveReferencePath.Equals(typeof(ComponentEndpointsGeneratorTestBase).Assembly.Location, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // Skip the test project
                if (resolveReferencePath.Equals(typeof(BlazorUnitedApp.App).Assembly.Location, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                metadataReferences.Add(MetadataReference.CreateFromFile(resolveReferencePath));
            }
        }

        return metadataReferences;
    }

    internal async Task VerifyAgainstBaselineUsingFile(Compilation compilation, [CallerMemberName] string callerName = "")
    {
        if (!IsGeneratorEnabled)
        {
            return;
        }

        var baselineFilePathMetadataValue = typeof(ComponentEndpointsGeneratorTestBase).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>().Single(d => d.Key == "ComponentEndpointsGeneratorTestBaselines").Value;
        var baselineFilePathRoot = SkipOnHelixAttribute.OnHelix()
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), "ComponentEndpointsGenerator", "Baselines")
            : baselineFilePathMetadataValue;
        var baselineFilePath = Path.Combine(baselineFilePathRoot!, $"{callerName}.generated.txt");
        var generatedSyntaxTree = compilation.SyntaxTrees.Last();
        var generatedCode = await generatedSyntaxTree.GetTextAsync();

        if (RegenerateBaselines)
        {
            var newSource = generatedCode.ToString()
                .Replace(ComponentEndpointsGeneratorSources.GeneratedCodeAttribute, "%GENERATEDCODEATTRIBUTE%")
                + Environment.NewLine;
            await File.WriteAllTextAsync(baselineFilePath, newSource);
            Assert.Fail("RegenerateBaselines=true. Do not merge PRs with this set.");
        }

        var baseline = await File.ReadAllTextAsync(baselineFilePath);
        var expectedLines = baseline
            .TrimEnd() // Trim newlines added by autoformat
            .Replace("%GENERATEDCODEATTRIBUTE%", ComponentEndpointsGeneratorSources.GeneratedCodeAttribute)
            .Split(Environment.NewLine);

        Assert.True(CompareLines(expectedLines, generatedCode, out var errorMessage), errorMessage);
    }

    private static bool CompareLines(string[] expectedLines, SourceText sourceText, out string message)
    {
        if (expectedLines.Length != sourceText.Lines.Count)
        {
            message = $"Line numbers do not match. Expected: {expectedLines.Length} lines, but generated {sourceText.Lines.Count}";
            return false;
        }
        var index = 0;
        foreach (var textLine in sourceText.Lines)
        {
            var expectedLine = expectedLines[index].Trim().ReplaceLineEndings();
            var actualLine = textLine.ToString().Trim().ReplaceLineEndings();
            if (!expectedLine.Equals(actualLine, StringComparison.Ordinal))
            {
                message = $"""
Line {textLine.LineNumber} does not match.
Expected Line:
{expectedLine}
Actual Line:
{textLine}
""";
                return false;
            }
            index++;
        }
        message = string.Empty;
        return true;
    }

    internal TestServer GetTestServer(Compilation compilation, params byte[][] otherLibraries)
    {
        var assemblyName = compilation.AssemblyName!;
        var symbolsName = Path.ChangeExtension(assemblyName, "pdb");

        var output = new MemoryStream();
        var pdb = new MemoryStream();

        var emitOptions = new EmitOptions(
            debugInformationFormat: DebugInformationFormat.PortablePdb,
            pdbFilePath: symbolsName,
            outputNameOverride: $"TestProject-{Guid.NewGuid()}");

        var embeddedTexts = new List<EmbeddedText>();

        // Make sure we embed the sources in pdb for easy debugging
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var text = syntaxTree.GetText();
            var encoding = text.Encoding ?? Encoding.UTF8;
            var buffer = encoding.GetBytes(text.ToString());
            var sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

            var syntaxRootNode = (CSharpSyntaxNode)syntaxTree.GetRoot();
            var newSyntaxTree = CSharpSyntaxTree.Create(syntaxRootNode, options: null, encoding: encoding, path: syntaxTree.FilePath);

            compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);

            embeddedTexts.Add(EmbeddedText.FromSource(syntaxTree.FilePath, sourceText));
        }

        var result = compilation.Emit(output, pdb, options: emitOptions, embeddedTexts: embeddedTexts);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity > DiagnosticSeverity.Warning));
        Assert.True(result.Success);

        output.Position = 0;
        pdb.Position = 0;

        foreach (var bytes in otherLibraries)
        {
            var stream = new MemoryStream(bytes)
            {
                Position = 0
            };
            AssemblyLoadContext.Default.LoadFromStream(stream);
        }
        
        var assembly = AssemblyLoadContext.Default.LoadFromStream(output, pdb);
        var builder = WebHostBuilderFactory.CreateFromAssemblyEntryPoint(assembly, Array.Empty<string>());

        if (builder is not null)
        {
            builder.UseEnvironment(Environments.Development);
        }

        var testHost = new TestServer(builder);
        return testHost;
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
