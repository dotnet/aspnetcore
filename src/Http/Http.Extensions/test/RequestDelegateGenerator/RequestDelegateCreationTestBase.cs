// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Globalization;
using System.IO.Pipelines;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract class RequestDelegateCreationTestBase : LoggedTest
{
    // Change this to true and run tests in development to regenerate baseline files.
    public bool RegenerateBaselines = false;

    protected abstract bool IsGeneratorEnabled { get; }

    internal static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Preview).WithFeatures(new[] { new KeyValuePair<string, string>("InterceptorsNamespaces", "Microsoft.AspNetCore.Http.Generated") });
    private static readonly Project _baseProject = CreateProject();
    private static readonly string _interceptsLocationAttributeRegex = @"\[global::System\.Runtime\.CompilerServices\.InterceptsLocationAttribute\(\d+, "".*""\)\]";

    internal async Task<(GeneratorRunResult?, Compilation)> RunGeneratorAsync(string sources, params string[] updatedSources)
    {
        // Create a Roslyn compilation for the syntax tree.
        var compilation = await CreateCompilationAsync(sources);

        // Return the compilation immediately if
        // the generator is not enabled.
        if (!IsGeneratorEnabled)
        {
            return (null, compilation);
        }

        // Configure the generator driver and run
        // the compilation with it if the generator
        // is enabled.
        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
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

    internal static RequestDelegateGenerator.StaticRouteHandlerModel.Endpoint GetStaticEndpoint(GeneratorRunResult result, string stepName) =>
        Assert.Single(GetStaticEndpoints(result, stepName));

    internal static RequestDelegateGenerator.StaticRouteHandlerModel.Endpoint[] GetStaticEndpoints(GeneratorRunResult result, string stepName)
    {
        // We only invoke the generator once in our test scenarios
        if (result.TrackedSteps.TryGetValue(stepName, out var staticEndpointSteps))
        {
            return staticEndpointSteps
                .SelectMany(step => step.Outputs)
                .Select(output => Assert.IsType<RequestDelegateGenerator.StaticRouteHandlerModel.Endpoint>(output.Value))
                .ToArray();
        }

        return Array.Empty<RequestDelegateGenerator.StaticRouteHandlerModel.Endpoint>();
    }

    internal static void VerifyStaticEndpointModel(GeneratorRunResult? result, Action<RequestDelegateGenerator.StaticRouteHandlerModel.Endpoint> runAssertions)
    {
        if (result.HasValue)
        {
            runAssertions(GetStaticEndpoint(result.Value, GeneratorSteps.EndpointModelStep));
        }
    }

    internal static void VerifyStaticEndpointModels(GeneratorRunResult? result, Action<RequestDelegateGenerator.StaticRouteHandlerModel.Endpoint[]> runAssertions)
    {
        if (result.HasValue)
        {
            runAssertions(GetStaticEndpoints(result.Value, GeneratorSteps.EndpointModelStep));
        }
    }

    internal Endpoint GetEndpointFromCompilation(Compilation compilation, bool? expectGeneratedCodeOverride = null, IServiceProvider serviceProvider = null) =>
        Assert.Single(GetEndpointsFromCompilation(compilation, expectGeneratedCodeOverride, serviceProvider));

    internal Endpoint[] GetEndpointsFromCompilation(Compilation compilation, bool? expectGeneratedCodeOverride = null, IServiceProvider serviceProvider = null, bool skipGeneratedCodeCheck = false)
    {
        var assemblyName = compilation.AssemblyName!;
        var symbolsName = Path.ChangeExtension(assemblyName, "pdb");
        var expectGeneratedCode = (expectGeneratedCodeOverride ?? true) && IsGeneratorEnabled;

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
            var newSyntaxTree = CSharpSyntaxTree.Create(syntaxRootNode, options: ParseOptions, encoding: encoding, path: syntaxTree.FilePath);

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

        Assert.NotNull(handler);

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider ?? CreateServiceProvider()));
        _ = handler(builder);

        var dataSource = Assert.Single(builder.DataSources);

        // Trigger Endpoint build by calling getter.
        var endpoints = dataSource.Endpoints.ToArray();

        if (skipGeneratedCodeCheck == true)
        {
            return endpoints;
        }

        foreach (var endpoint in endpoints)
        {
            var generatedCodeAttribute = endpoint.Metadata.OfType<GeneratedCodeAttribute>().SingleOrDefault();

            if (expectGeneratedCode)
            {
                Assert.NotNull(generatedCodeAttribute);
                var generatedCode = Assert.IsType<GeneratedCodeAttribute>(generatedCodeAttribute);
                Assert.Equal(typeof(RequestDelegateGeneratorSources).Assembly.FullName, generatedCode.Tool);
                Assert.Equal(typeof(RequestDelegateGeneratorSources).Assembly.GetName().Version?.ToString(), generatedCode.Version);
            }
            else
            {
                Assert.Null(generatedCodeAttribute);
            }
        }

        return endpoints;
    }

    internal HttpContext CreateHttpContext(IServiceProvider serviceProvider = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider ?? CreateServiceProvider();

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        return httpContext;
    }

    public ServiceProvider CreateServiceProvider(Action<IServiceCollection> configureServices = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory);
        if (configureServices is not null)
        {
            configureServices(serviceCollection);
        }
        return serviceCollection.BuildServiceProvider();
    }

    internal HttpContext CreateHttpContextWithBody(Todo requestData, IServiceProvider serviceProvider = null)
    {
        var httpContext = CreateHttpContext(serviceProvider);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.Request.Headers["Content-Type"] = "application/json";

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(requestData);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        return httpContext;
    }

    internal static async Task<string> GetResponseBodyAsync(HttpContext httpContext)
    {
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        return await streamReader.ReadToEndAsync();
    }

    internal static async Task VerifyResponseJsonBodyAsync<T>(HttpContext httpContext, Action<T> check, int expectedStatusCode = 200)
    {
        var body = await GetResponseBodyAsync(httpContext);
        var deserializedObject = JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        check(deserializedObject);
    }

    internal static async Task VerifyResponseJsonNodeAsync(HttpContext httpContext, Action<JsonNode> check, int expectedStatusCode = 200, string expectedContentType = "application/json; charset=utf-8")
    {
        var body = await GetResponseBodyAsync(httpContext);
        var node = JsonNode.Parse(body);

        Assert.Equal(expectedContentType, httpContext.Response.ContentType);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        check(node);
    }

    internal static async Task VerifyResponseBodyAsync(HttpContext httpContext, string expectedBody, int expectedStatusCode = 200)
    {
        var body = await GetResponseBodyAsync(httpContext);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
    }

    internal static string GetMapActionString(string sources, string className = "TestMapActions") => $$"""
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Generators.Tests;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Http;

public static class {{className}}
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        {{sources}}
        return app;
    }

    public static IResult TestResult(this IResultExtensions _) => TypedResults.Text("Hello World!");
}
""";
    private static Task<Compilation> CreateCompilationAsync(string sources)
    {
        var source = GetMapActionString(sources);
        var project = _baseProject.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        // Create a Roslyn compilation for the syntax tree.
        return project.GetCompilationAsync();
    }

    internal static Project CreateProject(Func<CSharpCompilationOptions, CSharpCompilationOptions> modifyCompilationOptions = null)
    {
        var projectName = $"TestProject-{Guid.NewGuid()}";
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Disable);
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
        var dependencyContext = DependencyContext.Load(typeof(RequestDelegateCreationTestBase).Assembly);

        Assert.NotNull(dependencyContext);

        foreach (var defaultCompileLibrary in dependencyContext.CompileLibraries)
        {
            foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(resolver))
            {
                // Skip the source generator itself
                if (resolveReferencePath.Equals(typeof(RequestDelegateGenerator.RequestDelegateGenerator).Assembly.Location, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(resolveReferencePath));
            }
        }

        return project;
    }

    internal async Task VerifyAgainstBaselineUsingFile(Compilation compilation, [CallerMemberName] string callerName = "")
    {
        if (!IsGeneratorEnabled)
        {
            return;
        }

        var baselineFilePathMetadataValue = typeof(RequestDelegateCreationTestBase).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>().Single(d => d.Key == "RequestDelegateGeneratorTestBaselines").Value;
        var baselineFilePathRoot = SkipOnHelixAttribute.OnHelix()
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), "RequestDelegateGenerator", "Baselines")
            : baselineFilePathMetadataValue;
        var baselineFilePath = Path.Combine(baselineFilePathRoot!, $"{callerName}.generated.txt");
        var generatedSyntaxTree = compilation.SyntaxTrees.Last();
        var generatedCode = await generatedSyntaxTree.GetTextAsync();

        if (RegenerateBaselines)
        {
            var newSource = generatedCode.ToString()
                .Replace(RequestDelegateGeneratorSources.GeneratedCodeAttribute, "%GENERATEDCODEATTRIBUTE%");
            newSource = Regex.Replace(newSource, _interceptsLocationAttributeRegex, "%INTERCEPTSLOCATIONATTRIBUTE%");
            newSource += Environment.NewLine;
            await File.WriteAllTextAsync(baselineFilePath, newSource);
            Assert.Fail("RegenerateBaselines=true. Do not merge PRs with this set.");
        }

        var baseline = await File.ReadAllTextAsync(baselineFilePath);
        var expectedLines = baseline
            .TrimEnd() // Trim newlines added by autoformat
            .Replace("%GENERATEDCODEATTRIBUTE%", RequestDelegateGeneratorSources.GeneratedCodeAttribute)
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
            if (Regex.IsMatch(actualLine, _interceptsLocationAttributeRegex))
            {
                index++;
                continue;
            }
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

    internal sealed class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public RequestBodyDetectionFeature(bool canHaveBody)
        {
            CanHaveBody = canHaveBody;
        }

        public bool CanHaveBody { get; }
    }

    internal sealed class PipeRequestBodyFeature : IRequestBodyPipeFeature
    {
        public PipeRequestBodyFeature(PipeReader pipeReader)
        {
            Reader = pipeReader;
        }
        public PipeReader Reader { get; set; }
    }
}
