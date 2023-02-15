// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public class RequestDelegateGeneratorTestBase : LoggedTest
{
    internal static async Task<(GeneratorRunResult, Compilation)> RunGeneratorAsync(string sources, params string[] updatedSources)
    {
        var compilation = await CreateCompilationAsync(sources);
        var generator = new RequestDelegateGenerator().AsSourceGenerator();

        // Enable the source generator in tests
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        // Run the source generator
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var _);
        foreach (var updatedSource in updatedSources)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(GetMapActionString(updatedSource), path: $"TestMapActions.cs");
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

    internal static StaticRouteHandlerModel.Endpoint GetStaticEndpoint(GeneratorRunResult result, string stepName) =>
        Assert.Single(GetStaticEndpoints(result, stepName));

    internal static StaticRouteHandlerModel.Endpoint[] GetStaticEndpoints(GeneratorRunResult result, string stepName)
    {
        // We only invoke the generator once in our test scenarios
        if (result.TrackedSteps.TryGetValue(stepName, out var staticEndpointSteps))
        {
            return staticEndpointSteps
                .SelectMany(step => step.Outputs)
                .Select(output => Assert.IsType<StaticRouteHandlerModel.Endpoint>(output.Value))
                .ToArray();
        }

        return Array.Empty<StaticRouteHandlerModel.Endpoint>();
    }

    internal static Endpoint GetEndpointFromCompilation(Compilation compilation, bool expectSourceKey = true) =>
        Assert.Single(GetEndpointsFromCompilation(compilation, expectSourceKey));

    internal static Endpoint[] GetEndpointsFromCompilation(Compilation compilation, bool expectSourceKey = true)
    {
        var assemblyName = compilation.AssemblyName!;
        var symbolsName = Path.ChangeExtension(assemblyName, "pdb");

        var output = new MemoryStream();
        var pdb = new MemoryStream();

        var emitOptions = new EmitOptions(
            debugInformationFormat: DebugInformationFormat.PortablePdb,
            pdbFilePath: symbolsName);

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

        var assembly = AssemblyLoadContext.Default.LoadFromStream(output, pdb);
        var handler = assembly.GetType("TestMapActions")
            ?.GetMethod("MapTestEndpoints", BindingFlags.Public | BindingFlags.Static)
            ?.CreateDelegate<Func<IEndpointRouteBuilder, IEndpointRouteBuilder>>();
        var sourceKeyType = assembly.GetType("Microsoft.AspNetCore.Builder.SourceKey");

        Assert.NotNull(handler);

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = handler(builder);

        var dataSource = Assert.Single(builder.DataSources);

        // Trigger Endpoint build by calling getter.
        var endpoints = dataSource.Endpoints.ToArray();

        foreach (var endpoint in endpoints)
        {
            var sourceKeyMetadata = endpoint.Metadata.FirstOrDefault(metadata => metadata.GetType() == sourceKeyType);

            if (expectSourceKey)
            {
                Assert.NotNull(sourceKeyMetadata);
            }
            else
            {
                Assert.Null(sourceKeyMetadata);
            }
        }

        return endpoints;
    }

    internal HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory);
        var jsonOptions = new JsonOptions();
        serviceCollection.AddSingleton(Options.Create(jsonOptions));
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        return httpContext;
    }

    internal HttpContext CreateHttpContextWithBody(Todo requestData)
    {
        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.Request.Headers["Content-Type"] = "application/json";

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(requestData);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        return httpContext;
    }

    internal static async Task VerifyResponseBodyAsync(HttpContext httpContext, string expectedBody, int expectedStatusCode = 200)
    {
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
    }

    private static string GetMapActionString(string sources) => $$"""
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

public static class TestMapActions
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        {{sources}}
        return app;
    }

    public interface ITodo
    {
        public int Id { get; }
        public string? Name { get; }
        public bool IsComplete { get; }
    }

    public class Todo
    {
        public int Id { get; set; }
        public string? Name { get; set; } = "Todo";
        public bool IsComplete { get; set; }
    }

    public class FromBodyAttribute : Attribute, IFromBodyMetadata
    {
        public bool AllowEmpty { get; set; }
    }
}
""";
    private static Task<Compilation> CreateCompilationAsync(string sources)
    {
        var source = GetMapActionString(sources);
        var projectName = $"TestProject-{Guid.NewGuid()}";
        var project = new AdhocWorkspace().CurrentSolution
            .AddProject(projectName, projectName, LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp11));

        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;

        // Add in required metadata references
        var resolver = new AppLocalResolver();
        var dependencyContext = DependencyContext.Load(typeof(RequestDelegateGeneratorTestBase).Assembly);

        Assert.NotNull(dependencyContext);

        foreach (var defaultCompileLibrary in dependencyContext.CompileLibraries)
        {
            foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(resolver))
            {
                // Skip the source generator itself
                if (resolveReferencePath.Equals(typeof(RequestDelegateGenerator).Assembly.Location, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(resolveReferencePath));
            }
        }

        // Create a Roslyn compilation for the syntax tree.
        return project.GetCompilationAsync();
    }

    internal async Task VerifyAgainstBaselineUsingFile(Compilation compilation, [CallerMemberName] string callerName = "")
    {
        var baselineFilePath = Path.Combine("RequestDelegateGenerator", "Baselines", $"{callerName}.generated.txt");
        var generatedSyntaxTree = compilation.SyntaxTrees.Last();
        var generatedCode = await generatedSyntaxTree.GetTextAsync();
        var baseline = await File.ReadAllTextAsync(baselineFilePath);
        var expectedLines = baseline
            .TrimEnd() // Trim newlines added by autoformat
            .Replace("%GENERATEDCODEATTRIBUTE%", RequestDelegateGeneratorSources.GeneratedCodeAttribute)
            .Split(Environment.NewLine);

        Assert.True(CompareLines(expectedLines, generatedCode, out var errorMessage), errorMessage);
    }

    private bool CompareLines(string[] expectedLines, SourceText sourceText, out string message)
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

    private class EmptyServiceProvider : IServiceScope, IServiceProvider, IServiceScopeFactory, IServiceProviderIsService
    {
        public IServiceProvider ServiceProvider => this;

        public IServiceScope CreateScope()
        {
            return this;
        }

        public void Dispose() { }

        public object GetService(Type serviceType)
        {
            if (IsService(serviceType))
            {
                return this;
            }

            return null;
        }

        public bool IsService(Type serviceType) =>
            serviceType == typeof(IServiceProvider) ||
            serviceType == typeof(IServiceScopeFactory) ||
            serviceType == typeof(IServiceProviderIsService);
    }

    private class DefaultEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public DefaultEndpointRouteBuilder(IApplicationBuilder applicationBuilder)
        {
            ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
            DataSources = new List<EndpointDataSource>();
        }

        private IApplicationBuilder ApplicationBuilder { get; }

        public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();

        public ICollection<EndpointDataSource> DataSources { get; }

        public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
    }

    public class Todo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Todo";
        public bool IsComplete { get; set; }
    }

    internal sealed class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public RequestBodyDetectionFeature(bool canHaveBody)
        {
            CanHaveBody = canHaveBody;
        }

        public bool CanHaveBody { get; }
    }
}
