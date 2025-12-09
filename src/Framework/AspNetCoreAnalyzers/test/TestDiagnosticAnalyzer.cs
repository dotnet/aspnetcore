// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.AspNetCore.Analyzers;

internal class TestDiagnosticAnalyzerRunner : DiagnosticAnalyzerRunner
{
    public TestDiagnosticAnalyzerRunner(DiagnosticAnalyzer analyzer)
    {
        Analyzer = analyzer;
    }

    public DiagnosticAnalyzer Analyzer { get; }

    public async Task<ClassifiedSpan[]> GetClassificationSpansAsync(TextSpan textSpan, params string[] sources)
    {
        var project = CreateProjectWithReferencesInBinDir(GetType().Assembly, sources);
        var doc = project.Solution.GetDocument(project.Documents.First().Id);

        var result = await Classifier.GetClassifiedSpansAsync(doc, textSpan, CancellationToken.None);

        return result.ToArray();
    }

    public Task<CompletionResult> GetCompletionsAndServiceAsync(int caretPosition, params string[] sources)
    {
        var source = sources.First();
        var insertionChar = source[caretPosition - 1];
        return GetCompletionsAndServiceAsync(caretPosition, CompletionTrigger.CreateInsertionTrigger(insertionChar), sources);
    }

    public async Task<CompletionResult> GetCompletionsAndServiceAsync(int caretPosition, CompletionTrigger completionTrigger, params string[] sources)
    {
        var project = CreateProjectWithReferencesInBinDir(GetType().Assembly, sources);
        var doc = project.Solution.GetDocument(project.Documents.First().Id);
        var originalText = await doc.GetTextAsync().ConfigureAwait(false);

        var completionService = CompletionService.GetService(doc);
        var shouldTriggerCompletion = completionService.ShouldTriggerCompletion(originalText, caretPosition, completionTrigger);

        if (shouldTriggerCompletion)
        {
            var result = await completionService.GetCompletionsAsync(doc, caretPosition, completionTrigger);
            var completionSpan = completionService.GetDefaultCompletionListSpan(originalText, caretPosition);

            return new(doc, completionService, result, completionSpan, shouldTriggerCompletion);
        }
        else
        {
            return new(doc, completionService, default, default, shouldTriggerCompletion);
        }
    }

    private async Task<(SyntaxToken token, SemanticModel model)> TryGetStringSyntaxTokenAtPositionAsync(int caretPosition, params string[] sources)
    {
        var project = CreateProjectWithReferencesInBinDir(GetType().Assembly, sources);
        var document = project.Solution.GetDocument(project.Documents.First().Id);

        var semanticModel = await document.GetSemanticModelAsync(CancellationToken.None).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return default;
        }

        var root = await document.GetSyntaxRootAsync(CancellationToken.None).ConfigureAwait(false);
        if (root == null)
        {
            return default;
        }

        var stringToken = root.FindToken(caretPosition);

        return (token: stringToken, model: semanticModel);
    }

    public async Task<AspNetCoreBraceMatchingResult?> GetBraceMatchesAsync(int caretPosition, params string[] sources)
    {
        var (token, model) = await TryGetStringSyntaxTokenAtPositionAsync(caretPosition, sources);
        var braceMatcher = new RoutePatternBraceMatcher();

        return braceMatcher.FindBraces(model, token, caretPosition, CancellationToken.None);
    }

    public async Task<List<AspNetCoreHighlightSpan>> GetHighlightingAsync(int caretPosition, params string[] sources)
    {
        var (token, model) = await TryGetStringSyntaxTokenAtPositionAsync(caretPosition, sources);
        var highlighter = new RoutePatternHighlighter();

        var highlights = highlighter.GetDocumentHighlights(model, token, caretPosition, CancellationToken.None);
        return highlights.SelectMany(h => h.HighlightSpans).ToList();
    }

    public Task<Diagnostic[]> GetDiagnosticsAsync(params string[] sources)
    {
        var project = CreateProjectWithReferencesInBinDir(GetType().Assembly, sources);

        return GetDiagnosticsAsync(project);
    }

    private static readonly Lazy<IExportProviderFactory> ExportProviderFactory;

    static TestDiagnosticAnalyzerRunner()
    {
        ExportProviderFactory = new Lazy<IExportProviderFactory>(
            () =>
            {
#pragma warning disable VSTHRD011 // Use AsyncLazy<T>
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                var assemblies = MefHostServices.DefaultAssemblies.ToList();
                assemblies.Add(RoutePatternClassifier.TestAccessor.ExternalAccessAssembly);

                var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true);
                var parts = Task.Run(() => discovery.CreatePartsAsync(assemblies)).GetAwaiter().GetResult();
                var catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts); //.WithDocumentTextDifferencingService();

                var configuration = CompositionConfiguration.Create(catalog);
                var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
                return runtimeComposition.CreateExportProviderFactory();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
#pragma warning restore VSTHRD011 // Use AsyncLazy<T>
            },
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private static AdhocWorkspace CreateWorkspace()
    {
        var exportProvider = ExportProviderFactory.Value.CreateExportProvider();
        var host = MefHostServices.Create(exportProvider.AsCompositionContext());
        return new AdhocWorkspace(host);
    }

    public static Project CreateProjectWithReferencesInBinDir(Assembly testAssembly, params string[] source)
    {
        // The deps file in the project is incorrect and does not contain "compile" nodes for some references.
        // However these binaries are always present in the bin output. As a "temporary" workaround, we'll add
        // every dll file that's present in the test's build output as a metadatareference.

        Func<Workspace> createWorkspace = CreateWorkspace;

        var project = DiagnosticProject.Create(testAssembly, source, createWorkspace, [typeof(RoutePatternClassifier), typeof(ExtensionMethodsCompletionProvider)]);
        foreach (var assembly in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
        {
            if (!project.MetadataReferences.Any(c => string.Equals(Path.GetFileNameWithoutExtension(c.Display), Path.GetFileNameWithoutExtension(assembly), StringComparison.OrdinalIgnoreCase)))
            {
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(assembly));
            }
        }

        return project;
    }

    public Task<Diagnostic[]> GetDiagnosticsAsync(Project project)
    {
        return GetDiagnosticsAsync(new[] { project }, Analyzer, Array.Empty<string>());
    }

    protected override CompilationOptions ConfigureCompilationOptions(CompilationOptions options)
    {
        return options.WithOutputKind(OutputKind.ConsoleApplication);
    }
}

public record CompletionResult(Document Document, CompletionService Service, CompletionList Completions, TextSpan CompletionListSpan, bool ShouldTriggerCompletion);
