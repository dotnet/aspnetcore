# Microsoft.CodeAnalysis.Diagnostics

``` diff
-namespace Microsoft.CodeAnalysis.Diagnostics {
 {
-    public abstract class AnalysisContext {
 {
-        protected AnalysisContext();

-        public virtual void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags analysisMode);

-        public virtual void EnableConcurrentExecution();

-        public abstract void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action);

-        public abstract void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct;

-        public abstract void RegisterCompilationAction(Action<CompilationAnalysisContext> action);

-        public abstract void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action);

-        public void RegisterOperationAction(Action<OperationAnalysisContext> action, params OperationKind[] operationKinds);

-        public virtual void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds);

-        public virtual void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action);

-        public virtual void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action);

-        public abstract void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action);

-        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds);

-        public abstract void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds);

-        public abstract void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) where TLanguageKindEnum : struct;

-        public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct;

-        public abstract void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action);

-        public bool TryGetValue<TValue>(SourceText text, SourceTextValueProvider<TValue> valueProvider, out TValue value);

-    }
-    public class AnalysisResult {
 {
-        public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; private set; }

-        public ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> AnalyzerTelemetryInfo { get; private set; }

-        public ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> CompilationDiagnostics { get; private set; }

-        public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> SemanticDiagnostics { get; private set; }

-        public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> SyntaxDiagnostics { get; private set; }

-        public ImmutableArray<Diagnostic> GetAllDiagnostics();

-        public ImmutableArray<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer analyzer);

-    }
-    public sealed class AnalyzerFileReference : AnalyzerReference, IEquatable<AnalyzerReference> {
 {
-        public AnalyzerFileReference(string fullPath, IAnalyzerAssemblyLoader assemblyLoader);

-        public override string Display { get; }

-        public override string FullPath { get; }

-        public override object Id { get; }

-        public event EventHandler<AnalyzerLoadFailureEventArgs> AnalyzerLoadFailed;

-        public bool Equals(AnalyzerReference other);

-        public override bool Equals(object obj);

-        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language);

-        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages();

-        public Assembly GetAssembly();

-        public override int GetHashCode();

-    }
-    public sealed class AnalyzerImageReference : AnalyzerReference {
 {
-        public AnalyzerImageReference(ImmutableArray<DiagnosticAnalyzer> analyzers, string fullPath = null, string display = null);

-        public override string Display { get; }

-        public override string FullPath { get; }

-        public override object Id { get; }

-        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language);

-        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages();

-    }
-    public sealed class AnalyzerLoadFailureEventArgs : EventArgs {
 {
-        public AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode errorCode, string message, Exception exceptionOpt = null, string typeNameOpt = null);

-        public AnalyzerLoadFailureEventArgs.FailureErrorCode ErrorCode { get; }

-        public Exception Exception { get; }

-        public string Message { get; }

-        public string TypeName { get; }

-        public enum FailureErrorCode {
 {
-            NoAnalyzers = 3,

-            None = 0,

-            UnableToCreateAnalyzer = 2,

-            UnableToLoadAnalyzer = 1,

-        }
-    }
-    public class AnalyzerOptions {
 {
-        public AnalyzerOptions(ImmutableArray<AdditionalText> additionalFiles);

-        public ImmutableArray<AdditionalText> AdditionalFiles { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public AnalyzerOptions WithAdditionalFiles(ImmutableArray<AdditionalText> additionalFiles);

-    }
-    public abstract class AnalyzerReference {
 {
-        protected AnalyzerReference();

-        public virtual string Display { get; }

-        public abstract string FullPath { get; }

-        public abstract object Id { get; }

-        public abstract ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language);

-        public abstract ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages();

-    }
-    public struct CodeBlockAnalysisContext {
 {
-        public CodeBlockAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public SyntaxNode CodeBlock { get; }

-        public AnalyzerOptions Options { get; }

-        public ISymbol OwningSymbol { get; }

-        public SemanticModel SemanticModel { get; }

-        public void ReportDiagnostic(Diagnostic diagnostic);

-    }
-    public abstract class CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct {
 {
-        protected CodeBlockStartAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public SyntaxNode CodeBlock { get; }

-        public AnalyzerOptions Options { get; }

-        public ISymbol OwningSymbol { get; }

-        public SemanticModel SemanticModel { get; }

-        public abstract void RegisterCodeBlockEndAction(Action<CodeBlockAnalysisContext> action);

-        public abstract void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds);

-        public void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds);

-    }
-    public struct CompilationAnalysisContext {
 {
-        public CompilationAnalysisContext(Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public Compilation Compilation { get; }

-        public AnalyzerOptions Options { get; }

-        public void ReportDiagnostic(Diagnostic diagnostic);

-        public bool TryGetValue<TValue>(SyntaxTree tree, SyntaxTreeValueProvider<TValue> valueProvider, out TValue value);

-        public bool TryGetValue<TValue>(SourceText text, SourceTextValueProvider<TValue> valueProvider, out TValue value);

-    }
-    public abstract class CompilationStartAnalysisContext {
 {
-        protected CompilationStartAnalysisContext(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public Compilation Compilation { get; }

-        public AnalyzerOptions Options { get; }

-        public abstract void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action);

-        public abstract void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct;

-        public abstract void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action);

-        public void RegisterOperationAction(Action<OperationAnalysisContext> action, params OperationKind[] operationKinds);

-        public virtual void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds);

-        public virtual void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action);

-        public virtual void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action);

-        public abstract void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action);

-        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds);

-        public abstract void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds);

-        public abstract void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) where TLanguageKindEnum : struct;

-        public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct;

-        public abstract void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action);

-        public bool TryGetValue<TValue>(SyntaxTree tree, SyntaxTreeValueProvider<TValue> valueProvider, out TValue value);

-        public bool TryGetValue<TValue>(SourceText text, SourceTextValueProvider<TValue> valueProvider, out TValue value);

-    }
-    public class CompilationWithAnalyzers {
 {
-        public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken);

-        public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions analysisOptions);

-        public CompilationWithAnalyzersOptions AnalysisOptions { get; }

-        public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }

-        public CancellationToken CancellationToken { get; }

-        public Compilation Compilation { get; }

-        public static void ClearAnalyzerState(ImmutableArray<DiagnosticAnalyzer> analyzers);

-        public Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync();

-        public Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(CancellationToken cancellationToken);

-        public Task<AnalysisResult> GetAnalysisResultAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken);

-        public Task<AnalysisResult> GetAnalysisResultAsync(CancellationToken cancellationToken);

-        public Task<ImmutableArray<Diagnostic>> GetAnalyzerCompilationDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken);

-        public Task<ImmutableArray<Diagnostic>> GetAnalyzerCompilationDiagnosticsAsync(CancellationToken cancellationToken);

-        public Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync();

-        public Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken);

-        public Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(CancellationToken cancellationToken);

-        public Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsAsync(SemanticModel model, Nullable<TextSpan> filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken);

-        public Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsAsync(SemanticModel model, Nullable<TextSpan> filterSpan, CancellationToken cancellationToken);

-        public Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsAsync(SyntaxTree tree, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken);

-        public Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsAsync(SyntaxTree tree, CancellationToken cancellationToken);

-        public Task<AnalyzerTelemetryInfo> GetAnalyzerTelemetryInfoAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken);

-        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(IEnumerable<Diagnostic> diagnostics, Compilation compilation);

-        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(ImmutableArray<Diagnostic> diagnostics, Compilation compilation);

-        public static bool IsDiagnosticAnalyzerSuppressed(DiagnosticAnalyzer analyzer, CompilationOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null);

-    }
-    public sealed class CompilationWithAnalyzersOptions {
 {
-        public CompilationWithAnalyzersOptions(AnalyzerOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, bool concurrentAnalysis, bool logAnalyzerExecutionTime);

-        public CompilationWithAnalyzersOptions(AnalyzerOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, bool concurrentAnalysis, bool logAnalyzerExecutionTime, bool reportSuppressedDiagnostics);

-        public CompilationWithAnalyzersOptions(AnalyzerOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, bool concurrentAnalysis, bool logAnalyzerExecutionTime, bool reportSuppressedDiagnostics, Func<Exception, bool> analyzerExceptionFilter);

-        public Func<Exception, bool> AnalyzerExceptionFilter { get; }

-        public bool ConcurrentAnalysis { get; }

-        public bool LogAnalyzerExecutionTime { get; }

-        public Action<Exception, DiagnosticAnalyzer, Diagnostic> OnAnalyzerException { get; }

-        public AnalyzerOptions Options { get; }

-        public bool ReportSuppressedDiagnostics { get; }

-    }
-    public abstract class DiagnosticAnalyzer {
 {
-        protected DiagnosticAnalyzer();

-        public abstract ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

-        public sealed override bool Equals(object obj);

-        public sealed override int GetHashCode();

-        public abstract void Initialize(AnalysisContext context);

-        public sealed override string ToString();

-    }
-    public sealed class DiagnosticAnalyzerAttribute : Attribute {
 {
-        public DiagnosticAnalyzerAttribute(string firstLanguage, params string[] additionalLanguages);

-        public string[] Languages { get; }

-    }
-    public static class DiagnosticAnalyzerExtensions {
 {
-        public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

-        public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions analysisOptions);

-    }
-    public enum GeneratedCodeAnalysisFlags {
 {
-        Analyze = 1,

-        None = 0,

-        ReportDiagnostics = 2,

-    }
-    public struct OperationAnalysisContext {
 {
-        public OperationAnalysisContext(IOperation operation, ISymbol containingSymbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public Compilation Compilation { get; }

-        public ISymbol ContainingSymbol { get; }

-        public IOperation Operation { get; }

-        public AnalyzerOptions Options { get; }

-        public void ReportDiagnostic(Diagnostic diagnostic);

-    }
-    public struct OperationBlockAnalysisContext {
 {
-        public OperationBlockAnalysisContext(ImmutableArray<IOperation> operationBlocks, ISymbol owningSymbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public Compilation Compilation { get; }

-        public ImmutableArray<IOperation> OperationBlocks { get; }

-        public AnalyzerOptions Options { get; }

-        public ISymbol OwningSymbol { get; }

-        public void ReportDiagnostic(Diagnostic diagnostic);

-    }
-    public abstract class OperationBlockStartAnalysisContext {
 {
-        protected OperationBlockStartAnalysisContext(ImmutableArray<IOperation> operationBlocks, ISymbol owningSymbol, Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public Compilation Compilation { get; }

-        public ImmutableArray<IOperation> OperationBlocks { get; }

-        public AnalyzerOptions Options { get; }

-        public ISymbol OwningSymbol { get; }

-        public void RegisterOperationAction(Action<OperationAnalysisContext> action, params OperationKind[] operationKinds);

-        public abstract void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds);

-        public abstract void RegisterOperationBlockEndAction(Action<OperationBlockAnalysisContext> action);

-    }
-    public struct SemanticModelAnalysisContext {
 {
-        public SemanticModelAnalysisContext(SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public AnalyzerOptions Options { get; }

-        public SemanticModel SemanticModel { get; }

-        public void ReportDiagnostic(Diagnostic diagnostic);

-    }
-    public sealed class SourceTextValueProvider<TValue> {
 {
-        public SourceTextValueProvider(Func<SourceText, TValue> computeValue, IEqualityComparer<SourceText> sourceTextComparer = null);

-    }
-    public sealed class SuppressionInfo {
 {
-        public AttributeData Attribute { get; }

-        public string Id { get; }

-    }
-    public struct SymbolAnalysisContext {
 {
-        public SymbolAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public Compilation Compilation { get; }

-        public AnalyzerOptions Options { get; }

-        public ISymbol Symbol { get; }

-        public void ReportDiagnostic(Diagnostic diagnostic);

-    }
-    public struct SyntaxNodeAnalysisContext {
 {
-        public SyntaxNodeAnalysisContext(SyntaxNode node, ISymbol containingSymbol, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);

-        public SyntaxNodeAnalysisContext(SyntaxNode node, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public Compilation Compilation { get; }

-        public ISymbol ContainingSymbol { get; }

-        public SyntaxNode Node { get; }

-        public AnalyzerOptions Options { get; }

-        public SemanticModel SemanticModel { get; }

-        public void ReportDiagnostic(Diagnostic diagnostic);

-    }
-    public struct SyntaxTreeAnalysisContext {
 {
-        public SyntaxTreeAnalysisContext(SyntaxTree tree, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);

-        public CancellationToken CancellationToken { get; }

-        public AnalyzerOptions Options { get; }

-        public SyntaxTree Tree { get; }

-        public void ReportDiagnostic(Diagnostic diagnostic);

-    }
-    public sealed class SyntaxTreeValueProvider<TValue> {
 {
-        public SyntaxTreeValueProvider(Func<SyntaxTree, TValue> computeValue, IEqualityComparer<SyntaxTree> syntaxTreeComparer = null);

-    }
-    public sealed class UnresolvedAnalyzerReference : AnalyzerReference {
 {
-        public UnresolvedAnalyzerReference(string unresolvedPath);

-        public override string Display { get; }

-        public override string FullPath { get; }

-        public override object Id { get; }

-        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language);

-        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages();

-    }
-}
```

