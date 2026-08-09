// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints.Generators;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

#nullable enable

public abstract class RazorComponentsMetadataGeneratorTestBase
{
    protected const string DefaultHostSource = """
        namespace TestHost;

        public sealed partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
        {
        }
        """;

    private static readonly CSharpParseOptions ParseOptions = new(
        LanguageVersion.Preview,
        DocumentationMode.Diagnose);

    private static readonly MetadataReference[] FrameworkReferences = CreateFrameworkReferences();

    protected static GeneratorTestResult RunGenerator(
        string referencedSource,
        string hostSource = DefaultHostSource,
        bool assertUpdatedCompilation = true,
        string? referencedAssemblyName = null,
        string? hostAssemblyName = null,
        IReadOnlyCollection<string>? hostFrameworkAssemblyNames = null,
        params string[] expectedDiagnosticIds)
    {
        referencedAssemblyName ??= $"TestComponents_{Guid.NewGuid():N}";
        var referencedCompilation = CreateCompilation(
            referencedAssemblyName,
            referencedSource + """

                internal static class GeneratorTestComponentsReference
                {
                    internal static readonly System.Type Value = typeof(Microsoft.AspNetCore.Components.IComponent);
                }
                """,
            FrameworkReferences);
        var referencedImage = Emit(referencedCompilation, "referenced input");
        var referencedMetadata = MetadataReference.CreateFromImage(referencedImage);

        var hostCompilation = CreateCompilation(
            hostAssemblyName ?? $"TestHost_{Guid.NewGuid():N}",
            hostSource,
            [.. SelectHostFrameworkReferences(hostFrameworkAssemblyNames), referencedMetadata]);

        AssertNoUnexpectedInputErrors(hostCompilation);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new RazorComponentsMetadataGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            hostCompilation,
            out var updatedCompilation,
            out var generatorDiagnostics);
        CollectionAssert.AreEqual(
            expectedDiagnosticIds.Order().ToArray(),
            generatorDiagnostics.Select(diagnostic => diagnostic.Id).Order().ToArray());

        var runResult = driver.GetRunResult();
        if (assertUpdatedCompilation)
        {
            AssertNoErrors(updatedCompilation, "updated host");
        }

        return new GeneratorTestResult(
            referencedAssemblyName,
            referencedImage,
            (CSharpCompilation)hostCompilation,
            (CSharpCompilation)updatedCompilation,
            runResult);
    }

    protected static RazorComponentsMetadataContext LoadContext(GeneratorTestResult result, out LoadedCompilation loaded)
    {
        loaded = result.EmitAndLoad();
        var contextType = loaded.HostAssembly.GetTypes().Single(
            type => typeof(RazorComponentsMetadataContext).IsAssignableFrom(type) && !type.IsAbstract);
        return (RazorComponentsMetadataContext)Activator.CreateInstance(contextType)!;
    }

    protected static string GetGeneratedSource(GeneratorTestResult result)
        => Assert.ContainsSingle(result.MetadataGeneratedSources).SourceText.ToString();

    protected static IEnumerable<ComponentDescriptor> GetReferencedComponents(
        RazorComponentsMetadataContext context,
        GeneratorTestResult result)
        => context.Components.Where(component =>
            string.Equals(
                component.Type.Assembly.GetName().Name,
                result.ReferencedAssemblyName,
                StringComparison.Ordinal));

    protected static IEnumerable<JSInvokableMethodDescriptor> GetReferencedJSInvokableMethods(
        RazorComponentsMetadataContext context,
        GeneratorTestResult result)
        => context.JSInvokableMethods.Where(method =>
            string.Equals(method.AssemblyName, result.ReferencedAssemblyName, StringComparison.Ordinal));

    protected static Diagnostic AssertDiagnostic(
        GeneratorTestResult result,
        string id,
        DiagnosticSeverity severity)
    {
        var diagnostic = Assert.ContainsSingle(diagnostic => diagnostic.Id == id, result.Diagnostics);
        Assert.AreEqual(severity, diagnostic.Severity);
        return diagnostic;
    }

    private static CSharpCompilation CreateCompilation(
        string assemblyName,
        string source,
        IEnumerable<MetadataReference> references)
        => CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, ParseOptions, $"{assemblyName}.cs")],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                allowUnsafe: true,
                deterministic: true)
                .WithSpecificDiagnosticOptions(
                    new Dictionary<string, ReportDiagnostic>
                    {
                        ["ASPNETCORE9004"] = ReportDiagnostic.Suppress,
                    }));

    private static byte[] Emit(Compilation compilation, string description)
    {
        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        Assert.IsTrue(
            emitResult.Success,
            $"{description} compilation failed:{Environment.NewLine}{string.Join(Environment.NewLine, emitResult.Diagnostics)}");
        return stream.ToArray();
    }

    private static void AssertNoUnexpectedInputErrors(Compilation compilation)
    {
        var errors = compilation.GetDiagnostics()
            .Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.Id != "CS0534" &&
                !(diagnostic.Id == "CS0234" &&
                  diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("ComponentTypeInfo", StringComparison.Ordinal)))
            .ToArray();
        Assert.IsEmpty(
            errors,
            $"input host compilation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    private static void AssertNoErrors(Compilation compilation, string description)
    {
        var errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.IsEmpty(
            errors,
            $"{description} compilation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    private static MetadataReference[] CreateFrameworkReferences()
    {
        // Touch the application assemblies before enumerating the loaded set.
        _ = typeof(IComponent);
        _ = typeof(Microsoft.AspNetCore.Components.ConfigureBrowser);
        _ = typeof(RazorComponentsMetadataContext);
        _ = typeof(JSInvokableAttribute);
        _ = typeof(System.Text.Json.JsonSerializer);

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies)
        {
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                paths.Add(path);
            }
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                paths.Add(assembly.Location);
            }
        }

        return paths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    }

    private static IEnumerable<MetadataReference> SelectHostFrameworkReferences(
        IReadOnlyCollection<string>? assemblyNames)
    {
        if (assemblyNames is null)
        {
            return FrameworkReferences;
        }

        var selectedAssemblyNames = assemblyNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return FrameworkReferences.Where(reference =>
        {
            if (reference.Display is not string display)
            {
                return true;
            }

            var assemblyName = Path.GetFileNameWithoutExtension(display);
            return !assemblyName.StartsWith("Microsoft.AspNetCore.", StringComparison.OrdinalIgnoreCase) ||
                selectedAssemblyNames.Contains(assemblyName);
        });
    }

    protected sealed class GeneratorTestResult
    {
        private readonly byte[] _referencedImage;

        internal GeneratorTestResult(
            string referencedAssemblyName,
            byte[] referencedImage,
            CSharpCompilation inputCompilation,
            CSharpCompilation updatedCompilation,
            GeneratorDriverRunResult runResult)
        {
            ReferencedAssemblyName = referencedAssemblyName;
            _referencedImage = referencedImage;
            InputCompilation = inputCompilation;
            UpdatedCompilation = updatedCompilation;
            RunResult = runResult;
        }

        public string ReferencedAssemblyName { get; }

        public CSharpCompilation InputCompilation { get; }

        public CSharpCompilation UpdatedCompilation { get; }

        public GeneratorDriverRunResult RunResult { get; }

        public ImmutableArray<GeneratedSourceResult> GeneratedSources
            => Assert.ContainsSingle(RunResult.Results).GeneratedSources;

        public ImmutableArray<GeneratedSourceResult> MetadataGeneratedSources
            => [.. GeneratedSources.Where(static source => source.HintName.EndsWith(".Metadata.g.cs", StringComparison.Ordinal))];

        public ImmutableArray<Diagnostic> Diagnostics
            => RunResult.Diagnostics;

        public LoadedCompilation EmitAndLoad()
        {
            var hostImage = Emit(UpdatedCompilation, "updated host");
            var loadContext = new TestAssemblyLoadContext();
            using var referencedStream = new MemoryStream(_referencedImage);
            var referencedAssembly = loadContext.LoadFromStream(referencedStream);
            using var hostStream = new MemoryStream(hostImage);
            var hostAssembly = loadContext.LoadFromStream(hostStream);
            return new LoadedCompilation(loadContext, referencedAssembly, hostAssembly);
        }
    }

    protected sealed class LoadedCompilation : IDisposable
    {
        private readonly AssemblyLoadContext _loadContext;

        internal LoadedCompilation(
            AssemblyLoadContext loadContext,
            Assembly referencedAssembly,
            Assembly hostAssembly)
        {
            _loadContext = loadContext;
            ReferencedAssembly = referencedAssembly;
            HostAssembly = hostAssembly;
        }

        public Assembly ReferencedAssembly { get; }

        public Assembly HostAssembly { get; }

        public void Dispose() => _loadContext.Unload();
    }

    private sealed class TestAssemblyLoadContext : AssemblyLoadContext
    {
        public TestAssemblyLoadContext()
            : base(isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }

}
