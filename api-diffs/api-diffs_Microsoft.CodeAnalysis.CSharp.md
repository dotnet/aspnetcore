# Microsoft.CodeAnalysis.CSharp

``` diff
-namespace Microsoft.CodeAnalysis.CSharp {
 {
-    public struct AwaitExpressionInfo : IEquatable<AwaitExpressionInfo> {
 {
-        public IMethodSymbol GetAwaiterMethod { get; }

-        public IMethodSymbol GetResultMethod { get; }

-        public IPropertySymbol IsCompletedProperty { get; }

-        public bool IsDynamic { get; }

-        public bool Equals(AwaitExpressionInfo other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public struct Conversion : IEquatable<Conversion> {
 {
-        public bool Exists { get; }

-        public bool IsAnonymousFunction { get; }

-        public bool IsBoxing { get; }

-        public bool IsConstantExpression { get; }

-        public bool IsDynamic { get; }

-        public bool IsEnumeration { get; }

-        public bool IsExplicit { get; }

-        public bool IsIdentity { get; }

-        public bool IsImplicit { get; }

-        public bool IsInterpolatedString { get; }

-        public bool IsIntPtr { get; }

-        public bool IsMethodGroup { get; }

-        public bool IsNullable { get; }

-        public bool IsNullLiteral { get; }

-        public bool IsNumeric { get; }

-        public bool IsPointer { get; }

-        public bool IsReference { get; }

-        public bool IsStackAlloc { get; }

-        public bool IsThrow { get; }

-        public bool IsTupleConversion { get; }

-        public bool IsTupleLiteralConversion { get; }

-        public bool IsUnboxing { get; }

-        public bool IsUserDefined { get; }

-        public IMethodSymbol MethodSymbol { get; }

-        public bool Equals(Conversion other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(Conversion left, Conversion right);

-        public static bool operator !=(Conversion left, Conversion right);

-        public CommonConversion ToCommonConversion();

-        public override string ToString();

-    }
-    public sealed class CSharpCommandLineArguments : CommandLineArguments {
 {
-        public new CSharpCompilationOptions CompilationOptions { get; internal set; }

-        protected override CompilationOptions CompilationOptionsCore { get; }

-        public new CSharpParseOptions ParseOptions { get; internal set; }

-        protected override ParseOptions ParseOptionsCore { get; }

-    }
-    public class CSharpCommandLineParser : CommandLineParser {
 {
-        public static CSharpCommandLineParser Default { get; }

-        protected override string RegularFileExtension { get; }

-        public static CSharpCommandLineParser Script { get; }

-        protected override string ScriptFileExtension { get; }

-        public new CSharpCommandLineArguments Parse(IEnumerable<string> args, string baseDirectory, string sdkDirectory, string additionalReferenceDirectories = null);

-        public static IEnumerable<string> ParseConditionalCompilationSymbols(string value, out IEnumerable<Diagnostic> diagnostics);

-    }
-    public sealed class CSharpCompilation : Compilation {
 {
-        protected override IAssemblySymbol CommonAssembly { get; }

-        protected override ITypeSymbol CommonDynamicType { get; }

-        protected override INamespaceSymbol CommonGlobalNamespace { get; }

-        protected override INamedTypeSymbol CommonObjectType { get; }

-        protected override CompilationOptions CommonOptions { get; }

-        protected override INamedTypeSymbol CommonScriptClass { get; }

-        protected override IModuleSymbol CommonSourceModule { get; }

-        protected override IEnumerable<SyntaxTree> CommonSyntaxTrees { get; }

-        public override ImmutableArray<MetadataReference> DirectiveReferences { get; }

-        public override bool IsCaseSensitive { get; }

-        public override string Language { get; }

-        public LanguageVersion LanguageVersion { get; }

-        public new CSharpCompilationOptions Options { get; }

-        public override IEnumerable<AssemblyIdentity> ReferencedAssemblyNames { get; }

-        public new CSharpScriptCompilationInfo ScriptCompilationInfo { get; }

-        public new ImmutableArray<SyntaxTree> SyntaxTrees { get; }

-        public new CSharpCompilation AddReferences(params MetadataReference[] references);

-        public new CSharpCompilation AddReferences(IEnumerable<MetadataReference> references);

-        public new CSharpCompilation AddSyntaxTrees(params SyntaxTree[] trees);

-        public new CSharpCompilation AddSyntaxTrees(IEnumerable<SyntaxTree> trees);

-        protected override void AppendDefaultVersionResource(Stream resourceStream);

-        public Conversion ClassifyConversion(ITypeSymbol source, ITypeSymbol destination);

-        public new CSharpCompilation Clone();

-        protected override Compilation CommonAddSyntaxTrees(IEnumerable<SyntaxTree> trees);

-        protected override Compilation CommonClone();

-        protected override bool CommonContainsSyntaxTree(SyntaxTree syntaxTree);

-        protected override INamedTypeSymbol CommonCreateAnonymousTypeSymbol(ImmutableArray<ITypeSymbol> memberTypes, ImmutableArray<string> memberNames, ImmutableArray<Location> memberLocations, ImmutableArray<bool> memberIsReadOnly);

-        protected override IArrayTypeSymbol CommonCreateArrayTypeSymbol(ITypeSymbol elementType, int rank);

-        protected override INamespaceSymbol CommonCreateErrorNamespaceSymbol(INamespaceSymbol container, string name);

-        protected override INamedTypeSymbol CommonCreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity);

-        protected override IPointerTypeSymbol CommonCreatePointerTypeSymbol(ITypeSymbol elementType);

-        protected override INamedTypeSymbol CommonCreateTupleTypeSymbol(INamedTypeSymbol underlyingType, ImmutableArray<string> elementNames, ImmutableArray<Location> elementLocations);

-        protected override INamedTypeSymbol CommonCreateTupleTypeSymbol(ImmutableArray<ITypeSymbol> elementTypes, ImmutableArray<string> elementNames, ImmutableArray<Location> elementLocations);

-        protected override ISymbol CommonGetAssemblyOrModuleSymbol(MetadataReference reference);

-        protected override INamespaceSymbol CommonGetCompilationNamespace(INamespaceSymbol namespaceSymbol);

-        protected override IMethodSymbol CommonGetEntryPoint(CancellationToken cancellationToken);

-        protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility);

-        protected override INamedTypeSymbol CommonGetSpecialType(SpecialType specialType);

-        protected override INamedTypeSymbol CommonGetTypeByMetadataName(string metadataName);

-        protected override Compilation CommonRemoveAllSyntaxTrees();

-        protected override Compilation CommonRemoveSyntaxTrees(IEnumerable<SyntaxTree> trees);

-        protected override Compilation CommonReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree);

-        protected override Compilation CommonWithAssemblyName(string assemblyName);

-        protected override Compilation CommonWithOptions(CompilationOptions options);

-        protected override Compilation CommonWithReferences(IEnumerable<MetadataReference> newReferences);

-        protected override Compilation CommonWithScriptCompilationInfo(ScriptCompilationInfo info);

-        public override bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken));

-        public new bool ContainsSyntaxTree(SyntaxTree syntaxTree);

-        public static CSharpCompilation Create(string assemblyName, IEnumerable<SyntaxTree> syntaxTrees = null, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null);

-        public static CSharpCompilation CreateScriptCompilation(string assemblyName, SyntaxTree syntaxTree = null, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null, CSharpCompilation previousScriptCompilation = null, Type returnType = null, Type globalsType = null);

-        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public override ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public MetadataReference GetDirectiveReference(ReferenceDirectiveTriviaSyntax directive);

-        public new MetadataReference GetMetadataReference(IAssemblySymbol assemblySymbol);

-        public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public override ImmutableArray<Diagnostic> GetParseDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public new SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility);

-        public override IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken));

-        public new CSharpCompilation RemoveAllReferences();

-        public new CSharpCompilation RemoveAllSyntaxTrees();

-        public new CSharpCompilation RemoveReferences(params MetadataReference[] references);

-        public new CSharpCompilation RemoveReferences(IEnumerable<MetadataReference> references);

-        public new CSharpCompilation RemoveSyntaxTrees(params SyntaxTree[] trees);

-        public new CSharpCompilation RemoveSyntaxTrees(IEnumerable<SyntaxTree> trees);

-        public new CSharpCompilation ReplaceReference(MetadataReference oldReference, MetadataReference newReference);

-        public new CSharpCompilation ReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree);

-        public override CompilationReference ToMetadataReference(ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false);

-        public new CSharpCompilation WithAssemblyName(string assemblyName);

-        public CSharpCompilation WithOptions(CSharpCompilationOptions options);

-        public new CSharpCompilation WithReferences(params MetadataReference[] references);

-        public new CSharpCompilation WithReferences(IEnumerable<MetadataReference> references);

-        public CSharpCompilation WithScriptCompilationInfo(CSharpScriptCompilationInfo info);

-    }
-    public sealed class CSharpCompilationOptions : CompilationOptions, IEquatable<CSharpCompilationOptions> {
 {
-        public CSharpCompilationOptions(OutputKind outputKind, bool reportSuppressedDiagnostics, string moduleName, string mainTypeName, string scriptClassName, IEnumerable<string> usings, OptimizationLevel optimizationLevel, bool checkOverflow, bool allowUnsafe, string cryptoKeyContainer, string cryptoKeyFile, ImmutableArray<byte> cryptoPublicKey, Nullable<bool> delaySign, Platform platform, ReportDiagnostic generalDiagnosticOption, int warningLevel, IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions, bool concurrentBuild, bool deterministic, XmlReferenceResolver xmlReferenceResolver, SourceReferenceResolver sourceReferenceResolver, MetadataReferenceResolver metadataReferenceResolver, AssemblyIdentityComparer assemblyIdentityComparer, StrongNameProvider strongNameProvider);

-        public CSharpCompilationOptions(OutputKind outputKind, bool reportSuppressedDiagnostics, string moduleName, string mainTypeName, string scriptClassName, IEnumerable<string> usings, OptimizationLevel optimizationLevel, bool checkOverflow, bool allowUnsafe, string cryptoKeyContainer, string cryptoKeyFile, ImmutableArray<byte> cryptoPublicKey, Nullable<bool> delaySign, Platform platform, ReportDiagnostic generalDiagnosticOption, int warningLevel, IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions, bool concurrentBuild, bool deterministic, XmlReferenceResolver xmlReferenceResolver, SourceReferenceResolver sourceReferenceResolver, MetadataReferenceResolver metadataReferenceResolver, AssemblyIdentityComparer assemblyIdentityComparer, StrongNameProvider strongNameProvider, bool publicSign);

-        public CSharpCompilationOptions(OutputKind outputKind, bool reportSuppressedDiagnostics = false, string moduleName = null, string mainTypeName = null, string scriptClassName = null, IEnumerable<string> usings = null, OptimizationLevel optimizationLevel = OptimizationLevel.Debug, bool checkOverflow = false, bool allowUnsafe = false, string cryptoKeyContainer = null, string cryptoKeyFile = null, ImmutableArray<byte> cryptoPublicKey = default(ImmutableArray<byte>), Nullable<bool> delaySign = default(Nullable<bool>), Platform platform = Platform.AnyCpu, ReportDiagnostic generalDiagnosticOption = ReportDiagnostic.Default, int warningLevel = 4, IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions = null, bool concurrentBuild = true, bool deterministic = false, XmlReferenceResolver xmlReferenceResolver = null, SourceReferenceResolver sourceReferenceResolver = null, MetadataReferenceResolver metadataReferenceResolver = null, AssemblyIdentityComparer assemblyIdentityComparer = null, StrongNameProvider strongNameProvider = null, bool publicSign = false, MetadataImportOptions metadataImportOptions = MetadataImportOptions.Public);

-        public CSharpCompilationOptions(OutputKind outputKind, string moduleName, string mainTypeName, string scriptClassName, IEnumerable<string> usings, OptimizationLevel optimizationLevel, bool checkOverflow, bool allowUnsafe, string cryptoKeyContainer, string cryptoKeyFile, ImmutableArray<byte> cryptoPublicKey, Nullable<bool> delaySign, Platform platform, ReportDiagnostic generalDiagnosticOption, int warningLevel, IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions, bool concurrentBuild, XmlReferenceResolver xmlReferenceResolver, SourceReferenceResolver sourceReferenceResolver, MetadataReferenceResolver metadataReferenceResolver, AssemblyIdentityComparer assemblyIdentityComparer, StrongNameProvider strongNameProvider);

-        public CSharpCompilationOptions(OutputKind outputKind, string moduleName, string mainTypeName, string scriptClassName, IEnumerable<string> usings, OptimizationLevel optimizationLevel, bool checkOverflow, bool allowUnsafe, string cryptoKeyContainer, string cryptoKeyFile, ImmutableArray<byte> cryptoPublicKey, Nullable<bool> delaySign, Platform platform, ReportDiagnostic generalDiagnosticOption, int warningLevel, IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions, bool concurrentBuild, bool deterministic, XmlReferenceResolver xmlReferenceResolver, SourceReferenceResolver sourceReferenceResolver, MetadataReferenceResolver metadataReferenceResolver, AssemblyIdentityComparer assemblyIdentityComparer, StrongNameProvider strongNameProvider);

-        public bool AllowUnsafe { get; private set; }

-        public override string Language { get; }

-        public ImmutableArray<string> Usings { get; private set; }

-        protected override CompilationOptions CommonWithAssemblyIdentityComparer(AssemblyIdentityComparer comparer);

-        protected override CompilationOptions CommonWithCheckOverflow(bool checkOverflow);

-        protected override CompilationOptions CommonWithConcurrentBuild(bool concurrent);

-        protected override CompilationOptions CommonWithCryptoKeyContainer(string cryptoKeyContainer);

-        protected override CompilationOptions CommonWithCryptoKeyFile(string cryptoKeyFile);

-        protected override CompilationOptions CommonWithCryptoPublicKey(ImmutableArray<byte> cryptoPublicKey);

-        protected override CompilationOptions CommonWithDelaySign(Nullable<bool> delaySign);

-        protected override CompilationOptions CommonWithDeterministic(bool deterministic);

-        protected override CompilationOptions CommonWithFeatures(ImmutableArray<string> features);

-        protected override CompilationOptions CommonWithGeneralDiagnosticOption(ReportDiagnostic value);

-        protected override CompilationOptions CommonWithMainTypeName(string mainTypeName);

-        protected override CompilationOptions CommonWithMetadataImportOptions(MetadataImportOptions value);

-        protected override CompilationOptions CommonWithMetadataReferenceResolver(MetadataReferenceResolver resolver);

-        protected override CompilationOptions CommonWithModuleName(string moduleName);

-        protected override CompilationOptions CommonWithOptimizationLevel(OptimizationLevel value);

-        protected override CompilationOptions CommonWithOutputKind(OutputKind kind);

-        protected override CompilationOptions CommonWithPlatform(Platform platform);

-        protected override CompilationOptions CommonWithPublicSign(bool publicSign);

-        protected override CompilationOptions CommonWithReportSuppressedDiagnostics(bool reportSuppressedDiagnostics);

-        protected override CompilationOptions CommonWithScriptClassName(string scriptClassName);

-        protected override CompilationOptions CommonWithSourceReferenceResolver(SourceReferenceResolver resolver);

-        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions);

-        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> specificDiagnosticOptions);

-        protected override CompilationOptions CommonWithStrongNameProvider(StrongNameProvider provider);

-        protected override CompilationOptions CommonWithXmlReferenceResolver(XmlReferenceResolver resolver);

-        public bool Equals(CSharpCompilationOptions other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public CSharpCompilationOptions WithAllowUnsafe(bool enabled);

-        public new CSharpCompilationOptions WithAssemblyIdentityComparer(AssemblyIdentityComparer comparer);

-        public new CSharpCompilationOptions WithConcurrentBuild(bool concurrentBuild);

-        public new CSharpCompilationOptions WithCryptoKeyContainer(string name);

-        public new CSharpCompilationOptions WithCryptoKeyFile(string path);

-        public new CSharpCompilationOptions WithCryptoPublicKey(ImmutableArray<byte> value);

-        public new CSharpCompilationOptions WithDelaySign(Nullable<bool> value);

-        public new CSharpCompilationOptions WithDeterministic(bool deterministic);

-        public new CSharpCompilationOptions WithGeneralDiagnosticOption(ReportDiagnostic value);

-        public new CSharpCompilationOptions WithMainTypeName(string name);

-        public new CSharpCompilationOptions WithMetadataImportOptions(MetadataImportOptions value);

-        public new CSharpCompilationOptions WithMetadataReferenceResolver(MetadataReferenceResolver resolver);

-        public new CSharpCompilationOptions WithModuleName(string moduleName);

-        public new CSharpCompilationOptions WithOptimizationLevel(OptimizationLevel value);

-        public new CSharpCompilationOptions WithOutputKind(OutputKind kind);

-        public new CSharpCompilationOptions WithOverflowChecks(bool enabled);

-        public new CSharpCompilationOptions WithPlatform(Platform platform);

-        public new CSharpCompilationOptions WithPublicSign(bool publicSign);

-        public new CSharpCompilationOptions WithReportSuppressedDiagnostics(bool reportSuppressedDiagnostics);

-        public new CSharpCompilationOptions WithScriptClassName(string name);

-        public new CSharpCompilationOptions WithSourceReferenceResolver(SourceReferenceResolver resolver);

-        public new CSharpCompilationOptions WithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> values);

-        public new CSharpCompilationOptions WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> values);

-        public new CSharpCompilationOptions WithStrongNameProvider(StrongNameProvider provider);

-        public CSharpCompilationOptions WithUsings(IEnumerable<string> usings);

-        public CSharpCompilationOptions WithUsings(ImmutableArray<string> usings);

-        public CSharpCompilationOptions WithUsings(params string[] usings);

-        public CSharpCompilationOptions WithWarningLevel(int warningLevel);

-        public new CSharpCompilationOptions WithXmlReferenceResolver(XmlReferenceResolver resolver);

-    }
-    public class CSharpDiagnosticFormatter : DiagnosticFormatter {
 {
-        public static CSharpDiagnosticFormatter Instance { get; }

-    }
-    public static class CSharpExtensions {
 {
-        public static ControlFlowAnalysis AnalyzeControlFlow(this SemanticModel semanticModel, StatementSyntax statement);

-        public static ControlFlowAnalysis AnalyzeControlFlow(this SemanticModel semanticModel, StatementSyntax firstStatement, StatementSyntax lastStatement);

-        public static DataFlowAnalysis AnalyzeDataFlow(this SemanticModel semanticModel, ExpressionSyntax expression);

-        public static DataFlowAnalysis AnalyzeDataFlow(this SemanticModel semanticModel, StatementSyntax statement);

-        public static DataFlowAnalysis AnalyzeDataFlow(this SemanticModel semanticModel, StatementSyntax firstStatement, StatementSyntax lastStatement);

-        public static Conversion ClassifyConversion(this Compilation compilation, ITypeSymbol source, ITypeSymbol destination);

-        public static Conversion ClassifyConversion(this SemanticModel semanticModel, ExpressionSyntax expression, ITypeSymbol destination, bool isExplicitInSource = false);

-        public static Conversion ClassifyConversion(this SemanticModel semanticModel, int position, ExpressionSyntax expression, ITypeSymbol destination, bool isExplicitInSource = false);

-        public static IAliasSymbol GetAliasInfo(this SemanticModel semanticModel, IdentifierNameSyntax nameSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static AwaitExpressionInfo GetAwaitExpressionInfo(this SemanticModel semanticModel, AwaitExpressionSyntax awaitExpression);

-        public static SymbolInfo GetCollectionInitializerSymbolInfo(this SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken = default(CancellationToken));

-        public static CompilationUnitSyntax GetCompilationUnitRoot(this SyntaxTree tree, CancellationToken cancellationToken = default(CancellationToken));

-        public static Optional<object> GetConstantValue(this SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken = default(CancellationToken));

-        public static Conversion GetConversion(this IConversionOperation conversionExpression);

-        public static Conversion GetConversion(this SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken = default(CancellationToken));

-        public static IMethodSymbol GetDeclaredSymbol(this SemanticModel semanticModel, AccessorDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static INamedTypeSymbol GetDeclaredSymbol(this SemanticModel semanticModel, AnonymousObjectCreationExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static IPropertySymbol GetDeclaredSymbol(this SemanticModel semanticModel, AnonymousObjectMemberDeclaratorSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ISymbol GetDeclaredSymbol(this SemanticModel semanticModel, ArgumentSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static IMethodSymbol GetDeclaredSymbol(this SemanticModel semanticModel, BaseMethodDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ISymbol GetDeclaredSymbol(this SemanticModel semanticModel, BasePropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static INamedTypeSymbol GetDeclaredSymbol(this SemanticModel semanticModel, BaseTypeDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ILocalSymbol GetDeclaredSymbol(this SemanticModel semanticModel, CatchDeclarationSyntax catchDeclaration, CancellationToken cancellationToken = default(CancellationToken));

-        public static INamedTypeSymbol GetDeclaredSymbol(this SemanticModel semanticModel, DelegateDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static IFieldSymbol GetDeclaredSymbol(this SemanticModel semanticModel, EnumMemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static IEventSymbol GetDeclaredSymbol(this SemanticModel semanticModel, EventDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static IAliasSymbol GetDeclaredSymbol(this SemanticModel semanticModel, ExternAliasDirectiveSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ILocalSymbol GetDeclaredSymbol(this SemanticModel semanticModel, ForEachStatementSyntax forEachStatement, CancellationToken cancellationToken = default(CancellationToken));

-        public static IPropertySymbol GetDeclaredSymbol(this SemanticModel semanticModel, IndexerDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static IRangeVariableSymbol GetDeclaredSymbol(this SemanticModel semanticModel, JoinIntoClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken));

-        public static ILabelSymbol GetDeclaredSymbol(this SemanticModel semanticModel, LabeledStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ISymbol GetDeclaredSymbol(this SemanticModel semanticModel, MemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static INamespaceSymbol GetDeclaredSymbol(this SemanticModel semanticModel, NamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static IParameterSymbol GetDeclaredSymbol(this SemanticModel semanticModel, ParameterSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static IPropertySymbol GetDeclaredSymbol(this SemanticModel semanticModel, PropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static IRangeVariableSymbol GetDeclaredSymbol(this SemanticModel semanticModel, QueryClauseSyntax queryClause, CancellationToken cancellationToken = default(CancellationToken));

-        public static IRangeVariableSymbol GetDeclaredSymbol(this SemanticModel semanticModel, QueryContinuationSyntax node, CancellationToken cancellationToken = default(CancellationToken));

-        public static ISymbol GetDeclaredSymbol(this SemanticModel semanticModel, SingleVariableDesignationSyntax designationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ILabelSymbol GetDeclaredSymbol(this SemanticModel semanticModel, SwitchLabelSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ISymbol GetDeclaredSymbol(this SemanticModel semanticModel, TupleElementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static INamedTypeSymbol GetDeclaredSymbol(this SemanticModel semanticModel, TupleExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ITypeParameterSymbol GetDeclaredSymbol(this SemanticModel semanticModel, TypeParameterSyntax typeParameter, CancellationToken cancellationToken = default(CancellationToken));

-        public static IAliasSymbol GetDeclaredSymbol(this SemanticModel semanticModel, UsingDirectiveSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ISymbol GetDeclaredSymbol(this SemanticModel semanticModel, VariableDeclaratorSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static DeconstructionInfo GetDeconstructionInfo(this SemanticModel semanticModel, AssignmentExpressionSyntax assignment);

-        public static DeconstructionInfo GetDeconstructionInfo(this SemanticModel semanticModel, ForEachVariableStatementSyntax @foreach);

-        public static DirectiveTriviaSyntax GetFirstDirective(this SyntaxNode node, Func<DirectiveTriviaSyntax, bool> predicate = null);

-        public static ForEachStatementInfo GetForEachStatementInfo(this SemanticModel semanticModel, CommonForEachStatementSyntax forEachStatement);

-        public static ForEachStatementInfo GetForEachStatementInfo(this SemanticModel semanticModel, ForEachStatementSyntax forEachStatement);

-        public static Conversion GetInConversion(this ICompoundAssignmentOperation compoundAssignment);

-        public static ImmutableArray<IPropertySymbol> GetIndexerGroup(this SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken = default(CancellationToken));

-        public static DirectiveTriviaSyntax GetLastDirective(this SyntaxNode node, Func<DirectiveTriviaSyntax, bool> predicate = null);

-        public static ImmutableArray<ISymbol> GetMemberGroup(this SemanticModel semanticModel, AttributeSyntax attribute, CancellationToken cancellationToken = default(CancellationToken));

-        public static ImmutableArray<ISymbol> GetMemberGroup(this SemanticModel semanticModel, ConstructorInitializerSyntax initializer, CancellationToken cancellationToken = default(CancellationToken));

-        public static ImmutableArray<ISymbol> GetMemberGroup(this SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken = default(CancellationToken));

-        public static Conversion GetOutConversion(this ICompoundAssignmentOperation compoundAssignment);

-        public static QueryClauseInfo GetQueryClauseInfo(this SemanticModel semanticModel, QueryClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken));

-        public static IAliasSymbol GetSpeculativeAliasInfo(this SemanticModel semanticModel, int position, IdentifierNameSyntax nameSyntax, SpeculativeBindingOption bindingOption);

-        public static Conversion GetSpeculativeConversion(this SemanticModel semanticModel, int position, ExpressionSyntax expression, SpeculativeBindingOption bindingOption);

-        public static SymbolInfo GetSpeculativeSymbolInfo(this SemanticModel semanticModel, int position, AttributeSyntax attribute);

-        public static SymbolInfo GetSpeculativeSymbolInfo(this SemanticModel semanticModel, int position, ConstructorInitializerSyntax constructorInitializer);

-        public static SymbolInfo GetSpeculativeSymbolInfo(this SemanticModel semanticModel, int position, CrefSyntax expression, SpeculativeBindingOption bindingOption);

-        public static SymbolInfo GetSpeculativeSymbolInfo(this SemanticModel semanticModel, int position, ExpressionSyntax expression, SpeculativeBindingOption bindingOption);

-        public static TypeInfo GetSpeculativeTypeInfo(this SemanticModel semanticModel, int position, ExpressionSyntax expression, SpeculativeBindingOption bindingOption);

-        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, AttributeSyntax attributeSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, ConstructorInitializerSyntax constructorInitializer, CancellationToken cancellationToken = default(CancellationToken));

-        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, CrefSyntax crefSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken = default(CancellationToken));

-        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, OrderingSyntax node, CancellationToken cancellationToken = default(CancellationToken));

-        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken));

-        public static TypeInfo GetTypeInfo(this SemanticModel semanticModel, AttributeSyntax attributeSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static TypeInfo GetTypeInfo(this SemanticModel semanticModel, ConstructorInitializerSyntax constructorInitializer, CancellationToken cancellationToken = default(CancellationToken));

-        public static TypeInfo GetTypeInfo(this SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken = default(CancellationToken));

-        public static TypeInfo GetTypeInfo(this SemanticModel semanticModel, SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken));

-        public static SyntaxTokenList Insert(this SyntaxTokenList list, int index, params SyntaxToken[] items);

-        public static bool IsContextualKeyword(this SyntaxToken token);

-        public static bool IsKeyword(this SyntaxToken token);

-        public static bool IsReservedKeyword(this SyntaxToken token);

-        public static bool IsVerbatimIdentifier(this SyntaxToken token);

-        public static bool IsVerbatimStringLiteral(this SyntaxToken token);

-        public static SyntaxKind Kind(this SyntaxNode node);

-        public static SyntaxKind Kind(this SyntaxNodeOrToken nodeOrToken);

-        public static SyntaxKind Kind(this SyntaxToken token);

-        public static SyntaxKind Kind(this SyntaxTrivia trivia);

-        public static SyntaxToken ReplaceTrivia(this SyntaxToken token, SyntaxTrivia oldTrivia, SyntaxTrivia newTrivia);

-        public static SyntaxToken ReplaceTrivia(this SyntaxToken token, IEnumerable<SyntaxTrivia> trivia, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia);

-        public static bool TryGetSpeculativeSemanticModel(this SemanticModel semanticModel, int position, ArrowExpressionClauseSyntax expressionBody, out SemanticModel speculativeModel);

-        public static bool TryGetSpeculativeSemanticModel(this SemanticModel semanticModel, int position, AttributeSyntax attribute, out SemanticModel speculativeModel);

-        public static bool TryGetSpeculativeSemanticModel(this SemanticModel semanticModel, int position, ConstructorInitializerSyntax constructorInitializer, out SemanticModel speculativeModel);

-        public static bool TryGetSpeculativeSemanticModel(this SemanticModel semanticModel, int position, CrefSyntax crefSyntax, out SemanticModel speculativeModel);

-        public static bool TryGetSpeculativeSemanticModel(this SemanticModel semanticModel, int position, EqualsValueClauseSyntax initializer, out SemanticModel speculativeModel);

-        public static bool TryGetSpeculativeSemanticModel(this SemanticModel semanticModel, int position, StatementSyntax statement, out SemanticModel speculativeModel);

-        public static bool TryGetSpeculativeSemanticModel(this SemanticModel semanticModel, int position, TypeSyntax type, out SemanticModel speculativeModel, SpeculativeBindingOption bindingOption = SpeculativeBindingOption.BindAsExpression);

-        public static bool TryGetSpeculativeSemanticModelForMethodBody(this SemanticModel semanticModel, int position, AccessorDeclarationSyntax accessor, out SemanticModel speculativeModel);

-        public static bool TryGetSpeculativeSemanticModelForMethodBody(this SemanticModel semanticModel, int position, BaseMethodDeclarationSyntax method, out SemanticModel speculativeModel);

-        public static VarianceKind VarianceKindFromToken(this SyntaxToken node);

-    }
-    public static class CSharpFileSystemExtensions {
 {
-        public static EmitResult Emit(this CSharpCompilation compilation, string outputPath, string pdbPath = null, string xmlDocumentationPath = null, string win32ResourcesPath = null, IEnumerable<ResourceDescription> manifestResources = null, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public sealed class CSharpParseOptions : ParseOptions, IEquatable<CSharpParseOptions> {
 {
-        public CSharpParseOptions(LanguageVersion languageVersion = LanguageVersion.Default, DocumentationMode documentationMode = DocumentationMode.Parse, SourceCodeKind kind = SourceCodeKind.Regular, IEnumerable<string> preprocessorSymbols = null);

-        public static CSharpParseOptions Default { get; }

-        public override IReadOnlyDictionary<string, string> Features { get; }

-        public override string Language { get; }

-        public LanguageVersion LanguageVersion { get; private set; }

-        public override IEnumerable<string> PreprocessorSymbolNames { get; }

-        public LanguageVersion SpecifiedLanguageVersion { get; private set; }

-        protected override ParseOptions CommonWithDocumentationMode(DocumentationMode documentationMode);

-        protected override ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features);

-        public override ParseOptions CommonWithKind(SourceCodeKind kind);

-        public bool Equals(CSharpParseOptions other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public new CSharpParseOptions WithDocumentationMode(DocumentationMode documentationMode);

-        public new CSharpParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features);

-        public new CSharpParseOptions WithKind(SourceCodeKind kind);

-        public CSharpParseOptions WithLanguageVersion(LanguageVersion version);

-        public CSharpParseOptions WithPreprocessorSymbols(IEnumerable<string> preprocessorSymbols);

-        public CSharpParseOptions WithPreprocessorSymbols(ImmutableArray<string> symbols);

-        public CSharpParseOptions WithPreprocessorSymbols(params string[] preprocessorSymbols);

-    }
-    public sealed class CSharpScriptCompilationInfo : ScriptCompilationInfo {
 {
-        public new CSharpCompilation PreviousScriptCompilation { get; }

-        public CSharpScriptCompilationInfo WithPreviousScriptCompilation(CSharpCompilation compilation);

-    }
-    public abstract class CSharpSyntaxNode : SyntaxNode, IFormattable {
 {
-        public override string Language { get; }

-        protected override SyntaxTree SyntaxTreeCore { get; }

-        public abstract void Accept(CSharpSyntaxVisitor visitor);

-        public abstract TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

-        public static SyntaxNode DeserializeFrom(Stream stream, CancellationToken cancellationToken = default(CancellationToken));

-        protected override bool EquivalentToCore(SyntaxNode other);

-        public new SyntaxToken FindToken(int position, bool findInsideTrivia = false);

-        public new SyntaxTrivia FindTrivia(int position, bool findInsideTrivia = false);

-        public new SyntaxTrivia FindTrivia(int position, Func<SyntaxTrivia, bool> stepInto);

-        public new IEnumerable<Diagnostic> GetDiagnostics();

-        public DirectiveTriviaSyntax GetFirstDirective(Func<DirectiveTriviaSyntax, bool> predicate = null);

-        public new SyntaxToken GetFirstToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false);

-        public DirectiveTriviaSyntax GetLastDirective(Func<DirectiveTriviaSyntax, bool> predicate = null);

-        public new SyntaxToken GetLastToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false);

-        public new SyntaxTriviaList GetLeadingTrivia();

-        public new Location GetLocation();

-        public new SyntaxTriviaList GetTrailingTrivia();

-        protected internal override SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore);

-        protected internal override SyntaxNode InsertTokensInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens, bool insertBefore);

-        protected internal override SyntaxNode InsertTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore);

-        protected override bool IsEquivalentToCore(SyntaxNode node, bool topLevel = false);

-        public SyntaxKind Kind();

-        protected internal override SyntaxNode NormalizeWhitespaceCore(string indentation, string eol, bool elasticTrivia);

-        protected internal override SyntaxNode RemoveNodesCore(IEnumerable<SyntaxNode> nodes, SyntaxRemoveOptions options);

-        protected internal override SyntaxNode ReplaceCore<TNode>(IEnumerable<TNode> nodes = null, Func<TNode, TNode, SyntaxNode> computeReplacementNode = null, IEnumerable<SyntaxToken> tokens = null, Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null, IEnumerable<SyntaxTrivia> trivia = null, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null);

-        protected internal override SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes);

-        protected internal override SyntaxNode ReplaceTokenInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens);

-        protected internal override SyntaxNode ReplaceTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia);

-        string System.IFormattable.ToString(string format, IFormatProvider formatProvider);

-    }
-    public abstract class CSharpSyntaxRewriter : CSharpSyntaxVisitor<SyntaxNode> {
 {
-        public CSharpSyntaxRewriter(bool visitIntoStructuredTrivia = false);

-        public virtual bool VisitIntoStructuredTrivia { get; }

-        public override SyntaxNode Visit(SyntaxNode node);

-        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node);

-        public override SyntaxNode VisitAccessorList(AccessorListSyntax node);

-        public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node);

-        public override SyntaxNode VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node);

-        public override SyntaxNode VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node);

-        public override SyntaxNode VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node);

-        public override SyntaxNode VisitArgument(ArgumentSyntax node);

-        public override SyntaxNode VisitArgumentList(ArgumentListSyntax node);

-        public override SyntaxNode VisitArrayCreationExpression(ArrayCreationExpressionSyntax node);

-        public override SyntaxNode VisitArrayRankSpecifier(ArrayRankSpecifierSyntax node);

-        public override SyntaxNode VisitArrayType(ArrayTypeSyntax node);

-        public override SyntaxNode VisitArrowExpressionClause(ArrowExpressionClauseSyntax node);

-        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node);

-        public override SyntaxNode VisitAttribute(AttributeSyntax node);

-        public override SyntaxNode VisitAttributeArgument(AttributeArgumentSyntax node);

-        public override SyntaxNode VisitAttributeArgumentList(AttributeArgumentListSyntax node);

-        public override SyntaxNode VisitAttributeList(AttributeListSyntax node);

-        public override SyntaxNode VisitAttributeTargetSpecifier(AttributeTargetSpecifierSyntax node);

-        public override SyntaxNode VisitAwaitExpression(AwaitExpressionSyntax node);

-        public override SyntaxNode VisitBadDirectiveTrivia(BadDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitBaseExpression(BaseExpressionSyntax node);

-        public override SyntaxNode VisitBaseList(BaseListSyntax node);

-        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node);

-        public override SyntaxNode VisitBlock(BlockSyntax node);

-        public override SyntaxNode VisitBracketedArgumentList(BracketedArgumentListSyntax node);

-        public override SyntaxNode VisitBracketedParameterList(BracketedParameterListSyntax node);

-        public override SyntaxNode VisitBreakStatement(BreakStatementSyntax node);

-        public override SyntaxNode VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node);

-        public override SyntaxNode VisitCaseSwitchLabel(CaseSwitchLabelSyntax node);

-        public override SyntaxNode VisitCastExpression(CastExpressionSyntax node);

-        public override SyntaxNode VisitCatchClause(CatchClauseSyntax node);

-        public override SyntaxNode VisitCatchDeclaration(CatchDeclarationSyntax node);

-        public override SyntaxNode VisitCatchFilterClause(CatchFilterClauseSyntax node);

-        public override SyntaxNode VisitCheckedExpression(CheckedExpressionSyntax node);

-        public override SyntaxNode VisitCheckedStatement(CheckedStatementSyntax node);

-        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node);

-        public override SyntaxNode VisitClassOrStructConstraint(ClassOrStructConstraintSyntax node);

-        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node);

-        public override SyntaxNode VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node);

-        public override SyntaxNode VisitConditionalExpression(ConditionalExpressionSyntax node);

-        public override SyntaxNode VisitConstantPattern(ConstantPatternSyntax node);

-        public override SyntaxNode VisitConstructorConstraint(ConstructorConstraintSyntax node);

-        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node);

-        public override SyntaxNode VisitConstructorInitializer(ConstructorInitializerSyntax node);

-        public override SyntaxNode VisitContinueStatement(ContinueStatementSyntax node);

-        public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node);

-        public override SyntaxNode VisitConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax node);

-        public override SyntaxNode VisitCrefBracketedParameterList(CrefBracketedParameterListSyntax node);

-        public override SyntaxNode VisitCrefParameter(CrefParameterSyntax node);

-        public override SyntaxNode VisitCrefParameterList(CrefParameterListSyntax node);

-        public override SyntaxNode VisitDeclarationExpression(DeclarationExpressionSyntax node);

-        public override SyntaxNode VisitDeclarationPattern(DeclarationPatternSyntax node);

-        public override SyntaxNode VisitDefaultExpression(DefaultExpressionSyntax node);

-        public override SyntaxNode VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node);

-        public override SyntaxNode VisitDefineDirectiveTrivia(DefineDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node);

-        public override SyntaxNode VisitDestructorDeclaration(DestructorDeclarationSyntax node);

-        public override SyntaxNode VisitDiscardDesignation(DiscardDesignationSyntax node);

-        public override SyntaxNode VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax node);

-        public override SyntaxNode VisitDoStatement(DoStatementSyntax node);

-        public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax node);

-        public override SyntaxNode VisitElementBindingExpression(ElementBindingExpressionSyntax node);

-        public override SyntaxNode VisitElifDirectiveTrivia(ElifDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitElseClause(ElseClauseSyntax node);

-        public override SyntaxNode VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitEmptyStatement(EmptyStatementSyntax node);

-        public override SyntaxNode VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node);

-        public override SyntaxNode VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node);

-        public override SyntaxNode VisitEqualsValueClause(EqualsValueClauseSyntax node);

-        public override SyntaxNode VisitErrorDirectiveTrivia(ErrorDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node);

-        public override SyntaxNode VisitEventFieldDeclaration(EventFieldDeclarationSyntax node);

-        public override SyntaxNode VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node);

-        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node);

-        public override SyntaxNode VisitExternAliasDirective(ExternAliasDirectiveSyntax node);

-        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node);

-        public override SyntaxNode VisitFinallyClause(FinallyClauseSyntax node);

-        public override SyntaxNode VisitFixedStatement(FixedStatementSyntax node);

-        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node);

-        public override SyntaxNode VisitForEachVariableStatement(ForEachVariableStatementSyntax node);

-        public override SyntaxNode VisitForStatement(ForStatementSyntax node);

-        public override SyntaxNode VisitFromClause(FromClauseSyntax node);

-        public override SyntaxNode VisitGenericName(GenericNameSyntax node);

-        public override SyntaxNode VisitGlobalStatement(GlobalStatementSyntax node);

-        public override SyntaxNode VisitGotoStatement(GotoStatementSyntax node);

-        public override SyntaxNode VisitGroupClause(GroupClauseSyntax node);

-        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node);

-        public override SyntaxNode VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitIfStatement(IfStatementSyntax node);

-        public override SyntaxNode VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node);

-        public override SyntaxNode VisitImplicitElementAccess(ImplicitElementAccessSyntax node);

-        public override SyntaxNode VisitImplicitStackAllocArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax node);

-        public override SyntaxNode VisitIncompleteMember(IncompleteMemberSyntax node);

-        public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node);

-        public override SyntaxNode VisitIndexerMemberCref(IndexerMemberCrefSyntax node);

-        public override SyntaxNode VisitInitializerExpression(InitializerExpressionSyntax node);

-        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node);

-        public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node);

-        public override SyntaxNode VisitInterpolatedStringText(InterpolatedStringTextSyntax node);

-        public override SyntaxNode VisitInterpolation(InterpolationSyntax node);

-        public override SyntaxNode VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node);

-        public override SyntaxNode VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node);

-        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node);

-        public override SyntaxNode VisitIsPatternExpression(IsPatternExpressionSyntax node);

-        public override SyntaxNode VisitJoinClause(JoinClauseSyntax node);

-        public override SyntaxNode VisitJoinIntoClause(JoinIntoClauseSyntax node);

-        public override SyntaxNode VisitLabeledStatement(LabeledStatementSyntax node);

-        public override SyntaxNode VisitLetClause(LetClauseSyntax node);

-        public override SyntaxNode VisitLineDirectiveTrivia(LineDirectiveTriviaSyntax node);

-        public virtual SyntaxTokenList VisitList(SyntaxTokenList list);

-        public virtual SyntaxTriviaList VisitList(SyntaxTriviaList list);

-        public virtual SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> list) where TNode : SyntaxNode;

-        public virtual SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode;

-        public virtual SyntaxTrivia VisitListElement(SyntaxTrivia element);

-        public virtual TNode VisitListElement<TNode>(TNode node) where TNode : SyntaxNode;

-        public virtual SyntaxToken VisitListSeparator(SyntaxToken separator);

-        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node);

-        public override SyntaxNode VisitLoadDirectiveTrivia(LoadDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node);

-        public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node);

-        public override SyntaxNode VisitLockStatement(LockStatementSyntax node);

-        public override SyntaxNode VisitMakeRefExpression(MakeRefExpressionSyntax node);

-        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node);

-        public override SyntaxNode VisitMemberBindingExpression(MemberBindingExpressionSyntax node);

-        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node);

-        public override SyntaxNode VisitNameColon(NameColonSyntax node);

-        public override SyntaxNode VisitNameEquals(NameEqualsSyntax node);

-        public override SyntaxNode VisitNameMemberCref(NameMemberCrefSyntax node);

-        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node);

-        public override SyntaxNode VisitNullableType(NullableTypeSyntax node);

-        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node);

-        public override SyntaxNode VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node);

-        public override SyntaxNode VisitOmittedTypeArgument(OmittedTypeArgumentSyntax node);

-        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node);

-        public override SyntaxNode VisitOperatorMemberCref(OperatorMemberCrefSyntax node);

-        public override SyntaxNode VisitOrderByClause(OrderByClauseSyntax node);

-        public override SyntaxNode VisitOrdering(OrderingSyntax node);

-        public override SyntaxNode VisitParameter(ParameterSyntax node);

-        public override SyntaxNode VisitParameterList(ParameterListSyntax node);

-        public override SyntaxNode VisitParenthesizedExpression(ParenthesizedExpressionSyntax node);

-        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node);

-        public override SyntaxNode VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node);

-        public override SyntaxNode VisitPointerType(PointerTypeSyntax node);

-        public override SyntaxNode VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node);

-        public override SyntaxNode VisitPragmaChecksumDirectiveTrivia(PragmaChecksumDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitPragmaWarningDirectiveTrivia(PragmaWarningDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node);

-        public override SyntaxNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node);

-        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node);

-        public override SyntaxNode VisitQualifiedCref(QualifiedCrefSyntax node);

-        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node);

-        public override SyntaxNode VisitQueryBody(QueryBodySyntax node);

-        public override SyntaxNode VisitQueryContinuation(QueryContinuationSyntax node);

-        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node);

-        public override SyntaxNode VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitRefExpression(RefExpressionSyntax node);

-        public override SyntaxNode VisitRefType(RefTypeSyntax node);

-        public override SyntaxNode VisitRefTypeExpression(RefTypeExpressionSyntax node);

-        public override SyntaxNode VisitRefValueExpression(RefValueExpressionSyntax node);

-        public override SyntaxNode VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node);

-        public override SyntaxNode VisitSelectClause(SelectClauseSyntax node);

-        public override SyntaxNode VisitShebangDirectiveTrivia(ShebangDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitSimpleBaseType(SimpleBaseTypeSyntax node);

-        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node);

-        public override SyntaxNode VisitSingleVariableDesignation(SingleVariableDesignationSyntax node);

-        public override SyntaxNode VisitSizeOfExpression(SizeOfExpressionSyntax node);

-        public override SyntaxNode VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node);

-        public override SyntaxNode VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node);

-        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node);

-        public override SyntaxNode VisitSwitchSection(SwitchSectionSyntax node);

-        public override SyntaxNode VisitSwitchStatement(SwitchStatementSyntax node);

-        public override SyntaxNode VisitThisExpression(ThisExpressionSyntax node);

-        public override SyntaxNode VisitThrowExpression(ThrowExpressionSyntax node);

-        public override SyntaxNode VisitThrowStatement(ThrowStatementSyntax node);

-        public virtual SyntaxToken VisitToken(SyntaxToken token);

-        public virtual SyntaxTrivia VisitTrivia(SyntaxTrivia trivia);

-        public override SyntaxNode VisitTryStatement(TryStatementSyntax node);

-        public override SyntaxNode VisitTupleElement(TupleElementSyntax node);

-        public override SyntaxNode VisitTupleExpression(TupleExpressionSyntax node);

-        public override SyntaxNode VisitTupleType(TupleTypeSyntax node);

-        public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node);

-        public override SyntaxNode VisitTypeConstraint(TypeConstraintSyntax node);

-        public override SyntaxNode VisitTypeCref(TypeCrefSyntax node);

-        public override SyntaxNode VisitTypeOfExpression(TypeOfExpressionSyntax node);

-        public override SyntaxNode VisitTypeParameter(TypeParameterSyntax node);

-        public override SyntaxNode VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node);

-        public override SyntaxNode VisitTypeParameterList(TypeParameterListSyntax node);

-        public override SyntaxNode VisitUndefDirectiveTrivia(UndefDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitUnsafeStatement(UnsafeStatementSyntax node);

-        public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node);

-        public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node);

-        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node);

-        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node);

-        public override SyntaxNode VisitWarningDirectiveTrivia(WarningDirectiveTriviaSyntax node);

-        public override SyntaxNode VisitWhenClause(WhenClauseSyntax node);

-        public override SyntaxNode VisitWhereClause(WhereClauseSyntax node);

-        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node);

-        public override SyntaxNode VisitXmlCDataSection(XmlCDataSectionSyntax node);

-        public override SyntaxNode VisitXmlComment(XmlCommentSyntax node);

-        public override SyntaxNode VisitXmlCrefAttribute(XmlCrefAttributeSyntax node);

-        public override SyntaxNode VisitXmlElement(XmlElementSyntax node);

-        public override SyntaxNode VisitXmlElementEndTag(XmlElementEndTagSyntax node);

-        public override SyntaxNode VisitXmlElementStartTag(XmlElementStartTagSyntax node);

-        public override SyntaxNode VisitXmlEmptyElement(XmlEmptyElementSyntax node);

-        public override SyntaxNode VisitXmlName(XmlNameSyntax node);

-        public override SyntaxNode VisitXmlNameAttribute(XmlNameAttributeSyntax node);

-        public override SyntaxNode VisitXmlPrefix(XmlPrefixSyntax node);

-        public override SyntaxNode VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax node);

-        public override SyntaxNode VisitXmlText(XmlTextSyntax node);

-        public override SyntaxNode VisitXmlTextAttribute(XmlTextAttributeSyntax node);

-        public override SyntaxNode VisitYieldStatement(YieldStatementSyntax node);

-    }
-    public abstract class CSharpSyntaxTree : SyntaxTree {
 {
-        protected CSharpSyntaxTree();

-        public abstract new CSharpParseOptions Options { get; }

-        protected override ParseOptions OptionsCore { get; }

-        protected T CloneNodeAsRoot<T>(T node) where T : CSharpSyntaxNode;

-        public static SyntaxTree Create(CSharpSyntaxNode root, CSharpParseOptions options = null, string path = "", Encoding encoding = null);

-        public override IList<TextSpan> GetChangedSpans(SyntaxTree oldTree);

-        public override IList<TextChange> GetChanges(SyntaxTree oldTree);

-        public CompilationUnitSyntax GetCompilationUnitRoot(CancellationToken cancellationToken = default(CancellationToken));

-        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxNode node);

-        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxNodeOrToken nodeOrToken);

-        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxToken token);

-        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxTrivia trivia);

-        public override IEnumerable<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public override FileLinePositionSpan GetLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken));

-        public override LineVisibility GetLineVisibility(int position, CancellationToken cancellationToken = default(CancellationToken));

-        public override Location GetLocation(TextSpan span);

-        public override FileLinePositionSpan GetMappedLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken));

-        public abstract new CSharpSyntaxNode GetRoot(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual new Task<CSharpSyntaxNode> GetRootAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected override Task<SyntaxNode> GetRootAsyncCore(CancellationToken cancellationToken);

-        protected override SyntaxNode GetRootCore(CancellationToken cancellationToken);

-        public override bool HasHiddenRegions();

-        public override bool IsEquivalentTo(SyntaxTree tree, bool topLevel = false);

-        public static SyntaxTree ParseText(SourceText text, CSharpParseOptions options = null, string path = "", CancellationToken cancellationToken = default(CancellationToken));

-        public static SyntaxTree ParseText(string text, CSharpParseOptions options = null, string path = "", Encoding encoding = null, CancellationToken cancellationToken = default(CancellationToken));

-        public abstract bool TryGetRoot(out CSharpSyntaxNode root);

-        protected override bool TryGetRootCore(out SyntaxNode root);

-        public override SyntaxTree WithChangedText(SourceText newText);

-    }
-    public abstract class CSharpSyntaxVisitor {
 {
-        protected CSharpSyntaxVisitor();

-        public virtual void DefaultVisit(SyntaxNode node);

-        public virtual void Visit(SyntaxNode node);

-        public virtual void VisitAccessorDeclaration(AccessorDeclarationSyntax node);

-        public virtual void VisitAccessorList(AccessorListSyntax node);

-        public virtual void VisitAliasQualifiedName(AliasQualifiedNameSyntax node);

-        public virtual void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node);

-        public virtual void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node);

-        public virtual void VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node);

-        public virtual void VisitArgument(ArgumentSyntax node);

-        public virtual void VisitArgumentList(ArgumentListSyntax node);

-        public virtual void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node);

-        public virtual void VisitArrayRankSpecifier(ArrayRankSpecifierSyntax node);

-        public virtual void VisitArrayType(ArrayTypeSyntax node);

-        public virtual void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node);

-        public virtual void VisitAssignmentExpression(AssignmentExpressionSyntax node);

-        public virtual void VisitAttribute(AttributeSyntax node);

-        public virtual void VisitAttributeArgument(AttributeArgumentSyntax node);

-        public virtual void VisitAttributeArgumentList(AttributeArgumentListSyntax node);

-        public virtual void VisitAttributeList(AttributeListSyntax node);

-        public virtual void VisitAttributeTargetSpecifier(AttributeTargetSpecifierSyntax node);

-        public virtual void VisitAwaitExpression(AwaitExpressionSyntax node);

-        public virtual void VisitBadDirectiveTrivia(BadDirectiveTriviaSyntax node);

-        public virtual void VisitBaseExpression(BaseExpressionSyntax node);

-        public virtual void VisitBaseList(BaseListSyntax node);

-        public virtual void VisitBinaryExpression(BinaryExpressionSyntax node);

-        public virtual void VisitBlock(BlockSyntax node);

-        public virtual void VisitBracketedArgumentList(BracketedArgumentListSyntax node);

-        public virtual void VisitBracketedParameterList(BracketedParameterListSyntax node);

-        public virtual void VisitBreakStatement(BreakStatementSyntax node);

-        public virtual void VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node);

-        public virtual void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node);

-        public virtual void VisitCastExpression(CastExpressionSyntax node);

-        public virtual void VisitCatchClause(CatchClauseSyntax node);

-        public virtual void VisitCatchDeclaration(CatchDeclarationSyntax node);

-        public virtual void VisitCatchFilterClause(CatchFilterClauseSyntax node);

-        public virtual void VisitCheckedExpression(CheckedExpressionSyntax node);

-        public virtual void VisitCheckedStatement(CheckedStatementSyntax node);

-        public virtual void VisitClassDeclaration(ClassDeclarationSyntax node);

-        public virtual void VisitClassOrStructConstraint(ClassOrStructConstraintSyntax node);

-        public virtual void VisitCompilationUnit(CompilationUnitSyntax node);

-        public virtual void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node);

-        public virtual void VisitConditionalExpression(ConditionalExpressionSyntax node);

-        public virtual void VisitConstantPattern(ConstantPatternSyntax node);

-        public virtual void VisitConstructorConstraint(ConstructorConstraintSyntax node);

-        public virtual void VisitConstructorDeclaration(ConstructorDeclarationSyntax node);

-        public virtual void VisitConstructorInitializer(ConstructorInitializerSyntax node);

-        public virtual void VisitContinueStatement(ContinueStatementSyntax node);

-        public virtual void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node);

-        public virtual void VisitConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax node);

-        public virtual void VisitCrefBracketedParameterList(CrefBracketedParameterListSyntax node);

-        public virtual void VisitCrefParameter(CrefParameterSyntax node);

-        public virtual void VisitCrefParameterList(CrefParameterListSyntax node);

-        public virtual void VisitDeclarationExpression(DeclarationExpressionSyntax node);

-        public virtual void VisitDeclarationPattern(DeclarationPatternSyntax node);

-        public virtual void VisitDefaultExpression(DefaultExpressionSyntax node);

-        public virtual void VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node);

-        public virtual void VisitDefineDirectiveTrivia(DefineDirectiveTriviaSyntax node);

-        public virtual void VisitDelegateDeclaration(DelegateDeclarationSyntax node);

-        public virtual void VisitDestructorDeclaration(DestructorDeclarationSyntax node);

-        public virtual void VisitDiscardDesignation(DiscardDesignationSyntax node);

-        public virtual void VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax node);

-        public virtual void VisitDoStatement(DoStatementSyntax node);

-        public virtual void VisitElementAccessExpression(ElementAccessExpressionSyntax node);

-        public virtual void VisitElementBindingExpression(ElementBindingExpressionSyntax node);

-        public virtual void VisitElifDirectiveTrivia(ElifDirectiveTriviaSyntax node);

-        public virtual void VisitElseClause(ElseClauseSyntax node);

-        public virtual void VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node);

-        public virtual void VisitEmptyStatement(EmptyStatementSyntax node);

-        public virtual void VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node);

-        public virtual void VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node);

-        public virtual void VisitEnumDeclaration(EnumDeclarationSyntax node);

-        public virtual void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node);

-        public virtual void VisitEqualsValueClause(EqualsValueClauseSyntax node);

-        public virtual void VisitErrorDirectiveTrivia(ErrorDirectiveTriviaSyntax node);

-        public virtual void VisitEventDeclaration(EventDeclarationSyntax node);

-        public virtual void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node);

-        public virtual void VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node);

-        public virtual void VisitExpressionStatement(ExpressionStatementSyntax node);

-        public virtual void VisitExternAliasDirective(ExternAliasDirectiveSyntax node);

-        public virtual void VisitFieldDeclaration(FieldDeclarationSyntax node);

-        public virtual void VisitFinallyClause(FinallyClauseSyntax node);

-        public virtual void VisitFixedStatement(FixedStatementSyntax node);

-        public virtual void VisitForEachStatement(ForEachStatementSyntax node);

-        public virtual void VisitForEachVariableStatement(ForEachVariableStatementSyntax node);

-        public virtual void VisitForStatement(ForStatementSyntax node);

-        public virtual void VisitFromClause(FromClauseSyntax node);

-        public virtual void VisitGenericName(GenericNameSyntax node);

-        public virtual void VisitGlobalStatement(GlobalStatementSyntax node);

-        public virtual void VisitGotoStatement(GotoStatementSyntax node);

-        public virtual void VisitGroupClause(GroupClauseSyntax node);

-        public virtual void VisitIdentifierName(IdentifierNameSyntax node);

-        public virtual void VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node);

-        public virtual void VisitIfStatement(IfStatementSyntax node);

-        public virtual void VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node);

-        public virtual void VisitImplicitElementAccess(ImplicitElementAccessSyntax node);

-        public virtual void VisitImplicitStackAllocArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax node);

-        public virtual void VisitIncompleteMember(IncompleteMemberSyntax node);

-        public virtual void VisitIndexerDeclaration(IndexerDeclarationSyntax node);

-        public virtual void VisitIndexerMemberCref(IndexerMemberCrefSyntax node);

-        public virtual void VisitInitializerExpression(InitializerExpressionSyntax node);

-        public virtual void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node);

-        public virtual void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node);

-        public virtual void VisitInterpolatedStringText(InterpolatedStringTextSyntax node);

-        public virtual void VisitInterpolation(InterpolationSyntax node);

-        public virtual void VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node);

-        public virtual void VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node);

-        public virtual void VisitInvocationExpression(InvocationExpressionSyntax node);

-        public virtual void VisitIsPatternExpression(IsPatternExpressionSyntax node);

-        public virtual void VisitJoinClause(JoinClauseSyntax node);

-        public virtual void VisitJoinIntoClause(JoinIntoClauseSyntax node);

-        public virtual void VisitLabeledStatement(LabeledStatementSyntax node);

-        public virtual void VisitLetClause(LetClauseSyntax node);

-        public virtual void VisitLineDirectiveTrivia(LineDirectiveTriviaSyntax node);

-        public virtual void VisitLiteralExpression(LiteralExpressionSyntax node);

-        public virtual void VisitLoadDirectiveTrivia(LoadDirectiveTriviaSyntax node);

-        public virtual void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node);

-        public virtual void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node);

-        public virtual void VisitLockStatement(LockStatementSyntax node);

-        public virtual void VisitMakeRefExpression(MakeRefExpressionSyntax node);

-        public virtual void VisitMemberAccessExpression(MemberAccessExpressionSyntax node);

-        public virtual void VisitMemberBindingExpression(MemberBindingExpressionSyntax node);

-        public virtual void VisitMethodDeclaration(MethodDeclarationSyntax node);

-        public virtual void VisitNameColon(NameColonSyntax node);

-        public virtual void VisitNameEquals(NameEqualsSyntax node);

-        public virtual void VisitNameMemberCref(NameMemberCrefSyntax node);

-        public virtual void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node);

-        public virtual void VisitNullableType(NullableTypeSyntax node);

-        public virtual void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node);

-        public virtual void VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node);

-        public virtual void VisitOmittedTypeArgument(OmittedTypeArgumentSyntax node);

-        public virtual void VisitOperatorDeclaration(OperatorDeclarationSyntax node);

-        public virtual void VisitOperatorMemberCref(OperatorMemberCrefSyntax node);

-        public virtual void VisitOrderByClause(OrderByClauseSyntax node);

-        public virtual void VisitOrdering(OrderingSyntax node);

-        public virtual void VisitParameter(ParameterSyntax node);

-        public virtual void VisitParameterList(ParameterListSyntax node);

-        public virtual void VisitParenthesizedExpression(ParenthesizedExpressionSyntax node);

-        public virtual void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node);

-        public virtual void VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node);

-        public virtual void VisitPointerType(PointerTypeSyntax node);

-        public virtual void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node);

-        public virtual void VisitPragmaChecksumDirectiveTrivia(PragmaChecksumDirectiveTriviaSyntax node);

-        public virtual void VisitPragmaWarningDirectiveTrivia(PragmaWarningDirectiveTriviaSyntax node);

-        public virtual void VisitPredefinedType(PredefinedTypeSyntax node);

-        public virtual void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node);

-        public virtual void VisitPropertyDeclaration(PropertyDeclarationSyntax node);

-        public virtual void VisitQualifiedCref(QualifiedCrefSyntax node);

-        public virtual void VisitQualifiedName(QualifiedNameSyntax node);

-        public virtual void VisitQueryBody(QueryBodySyntax node);

-        public virtual void VisitQueryContinuation(QueryContinuationSyntax node);

-        public virtual void VisitQueryExpression(QueryExpressionSyntax node);

-        public virtual void VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node);

-        public virtual void VisitRefExpression(RefExpressionSyntax node);

-        public virtual void VisitRefType(RefTypeSyntax node);

-        public virtual void VisitRefTypeExpression(RefTypeExpressionSyntax node);

-        public virtual void VisitRefValueExpression(RefValueExpressionSyntax node);

-        public virtual void VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node);

-        public virtual void VisitReturnStatement(ReturnStatementSyntax node);

-        public virtual void VisitSelectClause(SelectClauseSyntax node);

-        public virtual void VisitShebangDirectiveTrivia(ShebangDirectiveTriviaSyntax node);

-        public virtual void VisitSimpleBaseType(SimpleBaseTypeSyntax node);

-        public virtual void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node);

-        public virtual void VisitSingleVariableDesignation(SingleVariableDesignationSyntax node);

-        public virtual void VisitSizeOfExpression(SizeOfExpressionSyntax node);

-        public virtual void VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node);

-        public virtual void VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node);

-        public virtual void VisitStructDeclaration(StructDeclarationSyntax node);

-        public virtual void VisitSwitchSection(SwitchSectionSyntax node);

-        public virtual void VisitSwitchStatement(SwitchStatementSyntax node);

-        public virtual void VisitThisExpression(ThisExpressionSyntax node);

-        public virtual void VisitThrowExpression(ThrowExpressionSyntax node);

-        public virtual void VisitThrowStatement(ThrowStatementSyntax node);

-        public virtual void VisitTryStatement(TryStatementSyntax node);

-        public virtual void VisitTupleElement(TupleElementSyntax node);

-        public virtual void VisitTupleExpression(TupleExpressionSyntax node);

-        public virtual void VisitTupleType(TupleTypeSyntax node);

-        public virtual void VisitTypeArgumentList(TypeArgumentListSyntax node);

-        public virtual void VisitTypeConstraint(TypeConstraintSyntax node);

-        public virtual void VisitTypeCref(TypeCrefSyntax node);

-        public virtual void VisitTypeOfExpression(TypeOfExpressionSyntax node);

-        public virtual void VisitTypeParameter(TypeParameterSyntax node);

-        public virtual void VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node);

-        public virtual void VisitTypeParameterList(TypeParameterListSyntax node);

-        public virtual void VisitUndefDirectiveTrivia(UndefDirectiveTriviaSyntax node);

-        public virtual void VisitUnsafeStatement(UnsafeStatementSyntax node);

-        public virtual void VisitUsingDirective(UsingDirectiveSyntax node);

-        public virtual void VisitUsingStatement(UsingStatementSyntax node);

-        public virtual void VisitVariableDeclaration(VariableDeclarationSyntax node);

-        public virtual void VisitVariableDeclarator(VariableDeclaratorSyntax node);

-        public virtual void VisitWarningDirectiveTrivia(WarningDirectiveTriviaSyntax node);

-        public virtual void VisitWhenClause(WhenClauseSyntax node);

-        public virtual void VisitWhereClause(WhereClauseSyntax node);

-        public virtual void VisitWhileStatement(WhileStatementSyntax node);

-        public virtual void VisitXmlCDataSection(XmlCDataSectionSyntax node);

-        public virtual void VisitXmlComment(XmlCommentSyntax node);

-        public virtual void VisitXmlCrefAttribute(XmlCrefAttributeSyntax node);

-        public virtual void VisitXmlElement(XmlElementSyntax node);

-        public virtual void VisitXmlElementEndTag(XmlElementEndTagSyntax node);

-        public virtual void VisitXmlElementStartTag(XmlElementStartTagSyntax node);

-        public virtual void VisitXmlEmptyElement(XmlEmptyElementSyntax node);

-        public virtual void VisitXmlName(XmlNameSyntax node);

-        public virtual void VisitXmlNameAttribute(XmlNameAttributeSyntax node);

-        public virtual void VisitXmlPrefix(XmlPrefixSyntax node);

-        public virtual void VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax node);

-        public virtual void VisitXmlText(XmlTextSyntax node);

-        public virtual void VisitXmlTextAttribute(XmlTextAttributeSyntax node);

-        public virtual void VisitYieldStatement(YieldStatementSyntax node);

-    }
-    public abstract class CSharpSyntaxVisitor<TResult> {
 {
-        protected CSharpSyntaxVisitor();

-        public virtual TResult DefaultVisit(SyntaxNode node);

-        public virtual TResult Visit(SyntaxNode node);

-        public virtual TResult VisitAccessorDeclaration(AccessorDeclarationSyntax node);

-        public virtual TResult VisitAccessorList(AccessorListSyntax node);

-        public virtual TResult VisitAliasQualifiedName(AliasQualifiedNameSyntax node);

-        public virtual TResult VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node);

-        public virtual TResult VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node);

-        public virtual TResult VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node);

-        public virtual TResult VisitArgument(ArgumentSyntax node);

-        public virtual TResult VisitArgumentList(ArgumentListSyntax node);

-        public virtual TResult VisitArrayCreationExpression(ArrayCreationExpressionSyntax node);

-        public virtual TResult VisitArrayRankSpecifier(ArrayRankSpecifierSyntax node);

-        public virtual TResult VisitArrayType(ArrayTypeSyntax node);

-        public virtual TResult VisitArrowExpressionClause(ArrowExpressionClauseSyntax node);

-        public virtual TResult VisitAssignmentExpression(AssignmentExpressionSyntax node);

-        public virtual TResult VisitAttribute(AttributeSyntax node);

-        public virtual TResult VisitAttributeArgument(AttributeArgumentSyntax node);

-        public virtual TResult VisitAttributeArgumentList(AttributeArgumentListSyntax node);

-        public virtual TResult VisitAttributeList(AttributeListSyntax node);

-        public virtual TResult VisitAttributeTargetSpecifier(AttributeTargetSpecifierSyntax node);

-        public virtual TResult VisitAwaitExpression(AwaitExpressionSyntax node);

-        public virtual TResult VisitBadDirectiveTrivia(BadDirectiveTriviaSyntax node);

-        public virtual TResult VisitBaseExpression(BaseExpressionSyntax node);

-        public virtual TResult VisitBaseList(BaseListSyntax node);

-        public virtual TResult VisitBinaryExpression(BinaryExpressionSyntax node);

-        public virtual TResult VisitBlock(BlockSyntax node);

-        public virtual TResult VisitBracketedArgumentList(BracketedArgumentListSyntax node);

-        public virtual TResult VisitBracketedParameterList(BracketedParameterListSyntax node);

-        public virtual TResult VisitBreakStatement(BreakStatementSyntax node);

-        public virtual TResult VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node);

-        public virtual TResult VisitCaseSwitchLabel(CaseSwitchLabelSyntax node);

-        public virtual TResult VisitCastExpression(CastExpressionSyntax node);

-        public virtual TResult VisitCatchClause(CatchClauseSyntax node);

-        public virtual TResult VisitCatchDeclaration(CatchDeclarationSyntax node);

-        public virtual TResult VisitCatchFilterClause(CatchFilterClauseSyntax node);

-        public virtual TResult VisitCheckedExpression(CheckedExpressionSyntax node);

-        public virtual TResult VisitCheckedStatement(CheckedStatementSyntax node);

-        public virtual TResult VisitClassDeclaration(ClassDeclarationSyntax node);

-        public virtual TResult VisitClassOrStructConstraint(ClassOrStructConstraintSyntax node);

-        public virtual TResult VisitCompilationUnit(CompilationUnitSyntax node);

-        public virtual TResult VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node);

-        public virtual TResult VisitConditionalExpression(ConditionalExpressionSyntax node);

-        public virtual TResult VisitConstantPattern(ConstantPatternSyntax node);

-        public virtual TResult VisitConstructorConstraint(ConstructorConstraintSyntax node);

-        public virtual TResult VisitConstructorDeclaration(ConstructorDeclarationSyntax node);

-        public virtual TResult VisitConstructorInitializer(ConstructorInitializerSyntax node);

-        public virtual TResult VisitContinueStatement(ContinueStatementSyntax node);

-        public virtual TResult VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node);

-        public virtual TResult VisitConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax node);

-        public virtual TResult VisitCrefBracketedParameterList(CrefBracketedParameterListSyntax node);

-        public virtual TResult VisitCrefParameter(CrefParameterSyntax node);

-        public virtual TResult VisitCrefParameterList(CrefParameterListSyntax node);

-        public virtual TResult VisitDeclarationExpression(DeclarationExpressionSyntax node);

-        public virtual TResult VisitDeclarationPattern(DeclarationPatternSyntax node);

-        public virtual TResult VisitDefaultExpression(DefaultExpressionSyntax node);

-        public virtual TResult VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node);

-        public virtual TResult VisitDefineDirectiveTrivia(DefineDirectiveTriviaSyntax node);

-        public virtual TResult VisitDelegateDeclaration(DelegateDeclarationSyntax node);

-        public virtual TResult VisitDestructorDeclaration(DestructorDeclarationSyntax node);

-        public virtual TResult VisitDiscardDesignation(DiscardDesignationSyntax node);

-        public virtual TResult VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax node);

-        public virtual TResult VisitDoStatement(DoStatementSyntax node);

-        public virtual TResult VisitElementAccessExpression(ElementAccessExpressionSyntax node);

-        public virtual TResult VisitElementBindingExpression(ElementBindingExpressionSyntax node);

-        public virtual TResult VisitElifDirectiveTrivia(ElifDirectiveTriviaSyntax node);

-        public virtual TResult VisitElseClause(ElseClauseSyntax node);

-        public virtual TResult VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node);

-        public virtual TResult VisitEmptyStatement(EmptyStatementSyntax node);

-        public virtual TResult VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node);

-        public virtual TResult VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node);

-        public virtual TResult VisitEnumDeclaration(EnumDeclarationSyntax node);

-        public virtual TResult VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node);

-        public virtual TResult VisitEqualsValueClause(EqualsValueClauseSyntax node);

-        public virtual TResult VisitErrorDirectiveTrivia(ErrorDirectiveTriviaSyntax node);

-        public virtual TResult VisitEventDeclaration(EventDeclarationSyntax node);

-        public virtual TResult VisitEventFieldDeclaration(EventFieldDeclarationSyntax node);

-        public virtual TResult VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node);

-        public virtual TResult VisitExpressionStatement(ExpressionStatementSyntax node);

-        public virtual TResult VisitExternAliasDirective(ExternAliasDirectiveSyntax node);

-        public virtual TResult VisitFieldDeclaration(FieldDeclarationSyntax node);

-        public virtual TResult VisitFinallyClause(FinallyClauseSyntax node);

-        public virtual TResult VisitFixedStatement(FixedStatementSyntax node);

-        public virtual TResult VisitForEachStatement(ForEachStatementSyntax node);

-        public virtual TResult VisitForEachVariableStatement(ForEachVariableStatementSyntax node);

-        public virtual TResult VisitForStatement(ForStatementSyntax node);

-        public virtual TResult VisitFromClause(FromClauseSyntax node);

-        public virtual TResult VisitGenericName(GenericNameSyntax node);

-        public virtual TResult VisitGlobalStatement(GlobalStatementSyntax node);

-        public virtual TResult VisitGotoStatement(GotoStatementSyntax node);

-        public virtual TResult VisitGroupClause(GroupClauseSyntax node);

-        public virtual TResult VisitIdentifierName(IdentifierNameSyntax node);

-        public virtual TResult VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node);

-        public virtual TResult VisitIfStatement(IfStatementSyntax node);

-        public virtual TResult VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node);

-        public virtual TResult VisitImplicitElementAccess(ImplicitElementAccessSyntax node);

-        public virtual TResult VisitImplicitStackAllocArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax node);

-        public virtual TResult VisitIncompleteMember(IncompleteMemberSyntax node);

-        public virtual TResult VisitIndexerDeclaration(IndexerDeclarationSyntax node);

-        public virtual TResult VisitIndexerMemberCref(IndexerMemberCrefSyntax node);

-        public virtual TResult VisitInitializerExpression(InitializerExpressionSyntax node);

-        public virtual TResult VisitInterfaceDeclaration(InterfaceDeclarationSyntax node);

-        public virtual TResult VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node);

-        public virtual TResult VisitInterpolatedStringText(InterpolatedStringTextSyntax node);

-        public virtual TResult VisitInterpolation(InterpolationSyntax node);

-        public virtual TResult VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node);

-        public virtual TResult VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node);

-        public virtual TResult VisitInvocationExpression(InvocationExpressionSyntax node);

-        public virtual TResult VisitIsPatternExpression(IsPatternExpressionSyntax node);

-        public virtual TResult VisitJoinClause(JoinClauseSyntax node);

-        public virtual TResult VisitJoinIntoClause(JoinIntoClauseSyntax node);

-        public virtual TResult VisitLabeledStatement(LabeledStatementSyntax node);

-        public virtual TResult VisitLetClause(LetClauseSyntax node);

-        public virtual TResult VisitLineDirectiveTrivia(LineDirectiveTriviaSyntax node);

-        public virtual TResult VisitLiteralExpression(LiteralExpressionSyntax node);

-        public virtual TResult VisitLoadDirectiveTrivia(LoadDirectiveTriviaSyntax node);

-        public virtual TResult VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node);

-        public virtual TResult VisitLocalFunctionStatement(LocalFunctionStatementSyntax node);

-        public virtual TResult VisitLockStatement(LockStatementSyntax node);

-        public virtual TResult VisitMakeRefExpression(MakeRefExpressionSyntax node);

-        public virtual TResult VisitMemberAccessExpression(MemberAccessExpressionSyntax node);

-        public virtual TResult VisitMemberBindingExpression(MemberBindingExpressionSyntax node);

-        public virtual TResult VisitMethodDeclaration(MethodDeclarationSyntax node);

-        public virtual TResult VisitNameColon(NameColonSyntax node);

-        public virtual TResult VisitNameEquals(NameEqualsSyntax node);

-        public virtual TResult VisitNameMemberCref(NameMemberCrefSyntax node);

-        public virtual TResult VisitNamespaceDeclaration(NamespaceDeclarationSyntax node);

-        public virtual TResult VisitNullableType(NullableTypeSyntax node);

-        public virtual TResult VisitObjectCreationExpression(ObjectCreationExpressionSyntax node);

-        public virtual TResult VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node);

-        public virtual TResult VisitOmittedTypeArgument(OmittedTypeArgumentSyntax node);

-        public virtual TResult VisitOperatorDeclaration(OperatorDeclarationSyntax node);

-        public virtual TResult VisitOperatorMemberCref(OperatorMemberCrefSyntax node);

-        public virtual TResult VisitOrderByClause(OrderByClauseSyntax node);

-        public virtual TResult VisitOrdering(OrderingSyntax node);

-        public virtual TResult VisitParameter(ParameterSyntax node);

-        public virtual TResult VisitParameterList(ParameterListSyntax node);

-        public virtual TResult VisitParenthesizedExpression(ParenthesizedExpressionSyntax node);

-        public virtual TResult VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node);

-        public virtual TResult VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node);

-        public virtual TResult VisitPointerType(PointerTypeSyntax node);

-        public virtual TResult VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node);

-        public virtual TResult VisitPragmaChecksumDirectiveTrivia(PragmaChecksumDirectiveTriviaSyntax node);

-        public virtual TResult VisitPragmaWarningDirectiveTrivia(PragmaWarningDirectiveTriviaSyntax node);

-        public virtual TResult VisitPredefinedType(PredefinedTypeSyntax node);

-        public virtual TResult VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node);

-        public virtual TResult VisitPropertyDeclaration(PropertyDeclarationSyntax node);

-        public virtual TResult VisitQualifiedCref(QualifiedCrefSyntax node);

-        public virtual TResult VisitQualifiedName(QualifiedNameSyntax node);

-        public virtual TResult VisitQueryBody(QueryBodySyntax node);

-        public virtual TResult VisitQueryContinuation(QueryContinuationSyntax node);

-        public virtual TResult VisitQueryExpression(QueryExpressionSyntax node);

-        public virtual TResult VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node);

-        public virtual TResult VisitRefExpression(RefExpressionSyntax node);

-        public virtual TResult VisitRefType(RefTypeSyntax node);

-        public virtual TResult VisitRefTypeExpression(RefTypeExpressionSyntax node);

-        public virtual TResult VisitRefValueExpression(RefValueExpressionSyntax node);

-        public virtual TResult VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node);

-        public virtual TResult VisitReturnStatement(ReturnStatementSyntax node);

-        public virtual TResult VisitSelectClause(SelectClauseSyntax node);

-        public virtual TResult VisitShebangDirectiveTrivia(ShebangDirectiveTriviaSyntax node);

-        public virtual TResult VisitSimpleBaseType(SimpleBaseTypeSyntax node);

-        public virtual TResult VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node);

-        public virtual TResult VisitSingleVariableDesignation(SingleVariableDesignationSyntax node);

-        public virtual TResult VisitSizeOfExpression(SizeOfExpressionSyntax node);

-        public virtual TResult VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node);

-        public virtual TResult VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node);

-        public virtual TResult VisitStructDeclaration(StructDeclarationSyntax node);

-        public virtual TResult VisitSwitchSection(SwitchSectionSyntax node);

-        public virtual TResult VisitSwitchStatement(SwitchStatementSyntax node);

-        public virtual TResult VisitThisExpression(ThisExpressionSyntax node);

-        public virtual TResult VisitThrowExpression(ThrowExpressionSyntax node);

-        public virtual TResult VisitThrowStatement(ThrowStatementSyntax node);

-        public virtual TResult VisitTryStatement(TryStatementSyntax node);

-        public virtual TResult VisitTupleElement(TupleElementSyntax node);

-        public virtual TResult VisitTupleExpression(TupleExpressionSyntax node);

-        public virtual TResult VisitTupleType(TupleTypeSyntax node);

-        public virtual TResult VisitTypeArgumentList(TypeArgumentListSyntax node);

-        public virtual TResult VisitTypeConstraint(TypeConstraintSyntax node);

-        public virtual TResult VisitTypeCref(TypeCrefSyntax node);

-        public virtual TResult VisitTypeOfExpression(TypeOfExpressionSyntax node);

-        public virtual TResult VisitTypeParameter(TypeParameterSyntax node);

-        public virtual TResult VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node);

-        public virtual TResult VisitTypeParameterList(TypeParameterListSyntax node);

-        public virtual TResult VisitUndefDirectiveTrivia(UndefDirectiveTriviaSyntax node);

-        public virtual TResult VisitUnsafeStatement(UnsafeStatementSyntax node);

-        public virtual TResult VisitUsingDirective(UsingDirectiveSyntax node);

-        public virtual TResult VisitUsingStatement(UsingStatementSyntax node);

-        public virtual TResult VisitVariableDeclaration(VariableDeclarationSyntax node);

-        public virtual TResult VisitVariableDeclarator(VariableDeclaratorSyntax node);

-        public virtual TResult VisitWarningDirectiveTrivia(WarningDirectiveTriviaSyntax node);

-        public virtual TResult VisitWhenClause(WhenClauseSyntax node);

-        public virtual TResult VisitWhereClause(WhereClauseSyntax node);

-        public virtual TResult VisitWhileStatement(WhileStatementSyntax node);

-        public virtual TResult VisitXmlCDataSection(XmlCDataSectionSyntax node);

-        public virtual TResult VisitXmlComment(XmlCommentSyntax node);

-        public virtual TResult VisitXmlCrefAttribute(XmlCrefAttributeSyntax node);

-        public virtual TResult VisitXmlElement(XmlElementSyntax node);

-        public virtual TResult VisitXmlElementEndTag(XmlElementEndTagSyntax node);

-        public virtual TResult VisitXmlElementStartTag(XmlElementStartTagSyntax node);

-        public virtual TResult VisitXmlEmptyElement(XmlEmptyElementSyntax node);

-        public virtual TResult VisitXmlName(XmlNameSyntax node);

-        public virtual TResult VisitXmlNameAttribute(XmlNameAttributeSyntax node);

-        public virtual TResult VisitXmlPrefix(XmlPrefixSyntax node);

-        public virtual TResult VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax node);

-        public virtual TResult VisitXmlText(XmlTextSyntax node);

-        public virtual TResult VisitXmlTextAttribute(XmlTextAttributeSyntax node);

-        public virtual TResult VisitYieldStatement(YieldStatementSyntax node);

-    }
-    public abstract class CSharpSyntaxWalker : CSharpSyntaxVisitor {
 {
-        protected CSharpSyntaxWalker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node);

-        protected SyntaxWalkerDepth Depth { get; }

-        public override void DefaultVisit(SyntaxNode node);

-        public override void Visit(SyntaxNode node);

-        public virtual void VisitLeadingTrivia(SyntaxToken token);

-        public virtual void VisitToken(SyntaxToken token);

-        public virtual void VisitTrailingTrivia(SyntaxToken token);

-        public virtual void VisitTrivia(SyntaxTrivia trivia);

-    }
-    public struct DeconstructionInfo {
 {
-        public Nullable<Conversion> Conversion { get; }

-        public IMethodSymbol Method { get; }

-        public ImmutableArray<DeconstructionInfo> Nested { get; }

-    }
-    public struct ForEachStatementInfo : IEquatable<ForEachStatementInfo> {
 {
-        public Conversion CurrentConversion { get; }

-        public IPropertySymbol CurrentProperty { get; }

-        public IMethodSymbol DisposeMethod { get; }

-        public Conversion ElementConversion { get; }

-        public ITypeSymbol ElementType { get; }

-        public IMethodSymbol GetEnumeratorMethod { get; }

-        public IMethodSymbol MoveNextMethod { get; }

-        public bool Equals(ForEachStatementInfo other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public enum LanguageVersion {
 {
-        CSharp1 = 1,

-        CSharp2 = 2,

-        CSharp3 = 3,

-        CSharp4 = 4,

-        CSharp5 = 5,

-        CSharp6 = 6,

-        CSharp7 = 7,

-        CSharp7_1 = 701,

-        CSharp7_2 = 702,

-        CSharp7_3 = 703,

-        Default = 0,

-        Latest = 2147483647,

-    }
-    public static class LanguageVersionFacts {
 {
-        public static LanguageVersion MapSpecifiedToEffectiveVersion(this LanguageVersion version);

-        public static string ToDisplayString(this LanguageVersion version);

-        public static bool TryParse(this string version, out LanguageVersion result);

-    }
-    public struct QueryClauseInfo : IEquatable<QueryClauseInfo> {
 {
-        public SymbolInfo CastInfo { get; }

-        public SymbolInfo OperationInfo { get; }

-        public bool Equals(QueryClauseInfo other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public static class SymbolDisplay {
 {
-        public static string FormatLiteral(char c, bool quote);

-        public static string FormatLiteral(string value, bool quote);

-        public static string FormatPrimitive(object obj, bool quoteStrings, bool useHexadecimalNumbers);

-        public static ImmutableArray<SymbolDisplayPart> ToDisplayParts(ISymbol symbol, SymbolDisplayFormat format = null);

-        public static string ToDisplayString(ISymbol symbol, SymbolDisplayFormat format = null);

-        public static ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(ISymbol symbol, SemanticModel semanticModel, int position, SymbolDisplayFormat format = null);

-        public static string ToMinimalDisplayString(ISymbol symbol, SemanticModel semanticModel, int position, SymbolDisplayFormat format = null);

-    }
-    public static class SyntaxExtensions {
 {
-        public static SyntaxToken NormalizeWhitespace(this SyntaxToken token, string indentation, bool elasticTrivia);

-        public static SyntaxToken NormalizeWhitespace(this SyntaxToken token, string indentation = "    ", string eol = "\r\n", bool elasticTrivia = false);

-        public static SyntaxTriviaList NormalizeWhitespace(this SyntaxTriviaList list, string indentation, bool elasticTrivia);

-        public static SyntaxTriviaList NormalizeWhitespace(this SyntaxTriviaList list, string indentation = "    ", string eol = "\r\n", bool elasticTrivia = false);

-        public static SyntaxTriviaList ToSyntaxTriviaList(this IEnumerable<SyntaxTrivia> sequence);

-        public static IndexerDeclarationSyntax Update(this IndexerDeclarationSyntax syntax, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken thisKeyword, BracketedParameterListSyntax parameterList, AccessorListSyntax accessorList);

-        public static MethodDeclarationSyntax Update(this MethodDeclarationSyntax syntax, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax block, SyntaxToken semicolonToken);

-        public static OperatorDeclarationSyntax Update(this OperatorDeclarationSyntax syntax, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken operatorKeyword, SyntaxToken operatorToken, ParameterListSyntax parameterList, BlockSyntax block, SyntaxToken semicolonToken);

-        public static SimpleNameSyntax WithIdentifier(this SimpleNameSyntax simpleName, SyntaxToken identifier);

-    }
-    public static class SyntaxFactory {
 {
-        public static SyntaxTrivia CarriageReturn { get; }

-        public static SyntaxTrivia CarriageReturnLineFeed { get; }

-        public static SyntaxTrivia ElasticCarriageReturn { get; }

-        public static SyntaxTrivia ElasticCarriageReturnLineFeed { get; }

-        public static SyntaxTrivia ElasticLineFeed { get; }

-        public static SyntaxTrivia ElasticMarker { get; }

-        public static SyntaxTrivia ElasticSpace { get; }

-        public static SyntaxTrivia ElasticTab { get; }

-        public static SyntaxTrivia LineFeed { get; }

-        public static SyntaxTrivia Space { get; }

-        public static SyntaxTrivia Tab { get; }

-        public static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind);

-        public static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, BlockSyntax body);

-        public static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, ArrowExpressionClauseSyntax expressionBody);

-        public static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, BlockSyntax body);

-        public static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody);

-        public static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, BlockSyntax body, SyntaxToken semicolonToken);

-        public static AccessorListSyntax AccessorList(SyntaxList<AccessorDeclarationSyntax> accessors = default(SyntaxList<AccessorDeclarationSyntax>));

-        public static AccessorListSyntax AccessorList(SyntaxToken openBraceToken, SyntaxList<AccessorDeclarationSyntax> accessors, SyntaxToken closeBraceToken);

-        public static AliasQualifiedNameSyntax AliasQualifiedName(IdentifierNameSyntax alias, SimpleNameSyntax name);

-        public static AliasQualifiedNameSyntax AliasQualifiedName(IdentifierNameSyntax alias, SyntaxToken colonColonToken, SimpleNameSyntax name);

-        public static AliasQualifiedNameSyntax AliasQualifiedName(string alias, SimpleNameSyntax name);

-        public static AnonymousMethodExpressionSyntax AnonymousMethodExpression();

-        public static AnonymousMethodExpressionSyntax AnonymousMethodExpression(CSharpSyntaxNode body);

-        public static AnonymousMethodExpressionSyntax AnonymousMethodExpression(ParameterListSyntax parameterList, CSharpSyntaxNode body);

-        public static AnonymousMethodExpressionSyntax AnonymousMethodExpression(SyntaxToken asyncKeyword, SyntaxToken delegateKeyword, ParameterListSyntax parameterList, CSharpSyntaxNode body);

-        public static AnonymousObjectCreationExpressionSyntax AnonymousObjectCreationExpression(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> initializers = default(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax>));

-        public static AnonymousObjectCreationExpressionSyntax AnonymousObjectCreationExpression(SyntaxToken newKeyword, SyntaxToken openBraceToken, SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> initializers, SyntaxToken closeBraceToken);

-        public static AnonymousObjectMemberDeclaratorSyntax AnonymousObjectMemberDeclarator(ExpressionSyntax expression);

-        public static AnonymousObjectMemberDeclaratorSyntax AnonymousObjectMemberDeclarator(NameEqualsSyntax nameEquals, ExpressionSyntax expression);

-        public static bool AreEquivalent(SyntaxNode oldNode, SyntaxNode newNode, bool topLevel);

-        public static bool AreEquivalent(SyntaxNode oldNode, SyntaxNode newNode, Func<SyntaxKind, bool> ignoreChildNode = null);

-        public static bool AreEquivalent(SyntaxToken oldToken, SyntaxToken newToken);

-        public static bool AreEquivalent(SyntaxTokenList oldList, SyntaxTokenList newList);

-        public static bool AreEquivalent(SyntaxTree oldTree, SyntaxTree newTree, bool topLevel);

-        public static bool AreEquivalent<TNode>(SeparatedSyntaxList<TNode> oldList, SeparatedSyntaxList<TNode> newList, bool topLevel) where TNode : SyntaxNode;

-        public static bool AreEquivalent<TNode>(SeparatedSyntaxList<TNode> oldList, SeparatedSyntaxList<TNode> newList, Func<SyntaxKind, bool> ignoreChildNode = null) where TNode : SyntaxNode;

-        public static bool AreEquivalent<TNode>(SyntaxList<TNode> oldList, SyntaxList<TNode> newList, bool topLevel) where TNode : CSharpSyntaxNode;

-        public static bool AreEquivalent<TNode>(SyntaxList<TNode> oldList, SyntaxList<TNode> newList, Func<SyntaxKind, bool> ignoreChildNode = null) where TNode : SyntaxNode;

-        public static ArgumentSyntax Argument(ExpressionSyntax expression);

-        public static ArgumentSyntax Argument(NameColonSyntax nameColon, SyntaxToken refKindKeyword, ExpressionSyntax expression);

-        public static ArgumentListSyntax ArgumentList(SeparatedSyntaxList<ArgumentSyntax> arguments = default(SeparatedSyntaxList<ArgumentSyntax>));

-        public static ArgumentListSyntax ArgumentList(SyntaxToken openParenToken, SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxToken closeParenToken);

-        public static ArrayCreationExpressionSyntax ArrayCreationExpression(ArrayTypeSyntax type);

-        public static ArrayCreationExpressionSyntax ArrayCreationExpression(ArrayTypeSyntax type, InitializerExpressionSyntax initializer);

-        public static ArrayCreationExpressionSyntax ArrayCreationExpression(SyntaxToken newKeyword, ArrayTypeSyntax type, InitializerExpressionSyntax initializer);

-        public static ArrayRankSpecifierSyntax ArrayRankSpecifier(SeparatedSyntaxList<ExpressionSyntax> sizes = default(SeparatedSyntaxList<ExpressionSyntax>));

-        public static ArrayRankSpecifierSyntax ArrayRankSpecifier(SyntaxToken openBracketToken, SeparatedSyntaxList<ExpressionSyntax> sizes, SyntaxToken closeBracketToken);

-        public static ArrayTypeSyntax ArrayType(TypeSyntax elementType);

-        public static ArrayTypeSyntax ArrayType(TypeSyntax elementType, SyntaxList<ArrayRankSpecifierSyntax> rankSpecifiers);

-        public static ArrowExpressionClauseSyntax ArrowExpressionClause(ExpressionSyntax expression);

-        public static ArrowExpressionClauseSyntax ArrowExpressionClause(SyntaxToken arrowToken, ExpressionSyntax expression);

-        public static AssignmentExpressionSyntax AssignmentExpression(SyntaxKind kind, ExpressionSyntax left, ExpressionSyntax right);

-        public static AssignmentExpressionSyntax AssignmentExpression(SyntaxKind kind, ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right);

-        public static AttributeSyntax Attribute(NameSyntax name);

-        public static AttributeSyntax Attribute(NameSyntax name, AttributeArgumentListSyntax argumentList);

-        public static AttributeArgumentSyntax AttributeArgument(ExpressionSyntax expression);

-        public static AttributeArgumentSyntax AttributeArgument(NameEqualsSyntax nameEquals, NameColonSyntax nameColon, ExpressionSyntax expression);

-        public static AttributeArgumentListSyntax AttributeArgumentList(SeparatedSyntaxList<AttributeArgumentSyntax> arguments = default(SeparatedSyntaxList<AttributeArgumentSyntax>));

-        public static AttributeArgumentListSyntax AttributeArgumentList(SyntaxToken openParenToken, SeparatedSyntaxList<AttributeArgumentSyntax> arguments, SyntaxToken closeParenToken);

-        public static AttributeListSyntax AttributeList(AttributeTargetSpecifierSyntax target, SeparatedSyntaxList<AttributeSyntax> attributes);

-        public static AttributeListSyntax AttributeList(SeparatedSyntaxList<AttributeSyntax> attributes = default(SeparatedSyntaxList<AttributeSyntax>));

-        public static AttributeListSyntax AttributeList(SyntaxToken openBracketToken, AttributeTargetSpecifierSyntax target, SeparatedSyntaxList<AttributeSyntax> attributes, SyntaxToken closeBracketToken);

-        public static AttributeTargetSpecifierSyntax AttributeTargetSpecifier(SyntaxToken identifier);

-        public static AttributeTargetSpecifierSyntax AttributeTargetSpecifier(SyntaxToken identifier, SyntaxToken colonToken);

-        public static AwaitExpressionSyntax AwaitExpression(ExpressionSyntax expression);

-        public static AwaitExpressionSyntax AwaitExpression(SyntaxToken awaitKeyword, ExpressionSyntax expression);

-        public static BadDirectiveTriviaSyntax BadDirectiveTrivia(SyntaxToken hashToken, SyntaxToken identifier, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static BadDirectiveTriviaSyntax BadDirectiveTrivia(SyntaxToken identifier, bool isActive);

-        public static SyntaxToken BadToken(SyntaxTriviaList leading, string text, SyntaxTriviaList trailing);

-        public static BaseExpressionSyntax BaseExpression();

-        public static BaseExpressionSyntax BaseExpression(SyntaxToken token);

-        public static BaseListSyntax BaseList(SeparatedSyntaxList<BaseTypeSyntax> types = default(SeparatedSyntaxList<BaseTypeSyntax>));

-        public static BaseListSyntax BaseList(SyntaxToken colonToken, SeparatedSyntaxList<BaseTypeSyntax> types);

-        public static BinaryExpressionSyntax BinaryExpression(SyntaxKind kind, ExpressionSyntax left, ExpressionSyntax right);

-        public static BinaryExpressionSyntax BinaryExpression(SyntaxKind kind, ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right);

-        public static BlockSyntax Block(params StatementSyntax[] statements);

-        public static BlockSyntax Block(SyntaxList<StatementSyntax> statements = default(SyntaxList<StatementSyntax>));

-        public static BlockSyntax Block(SyntaxToken openBraceToken, SyntaxList<StatementSyntax> statements, SyntaxToken closeBraceToken);

-        public static BlockSyntax Block(IEnumerable<StatementSyntax> statements);

-        public static BracketedArgumentListSyntax BracketedArgumentList(SeparatedSyntaxList<ArgumentSyntax> arguments = default(SeparatedSyntaxList<ArgumentSyntax>));

-        public static BracketedArgumentListSyntax BracketedArgumentList(SyntaxToken openBracketToken, SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxToken closeBracketToken);

-        public static BracketedParameterListSyntax BracketedParameterList(SeparatedSyntaxList<ParameterSyntax> parameters = default(SeparatedSyntaxList<ParameterSyntax>));

-        public static BracketedParameterListSyntax BracketedParameterList(SyntaxToken openBracketToken, SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeBracketToken);

-        public static BreakStatementSyntax BreakStatement();

-        public static BreakStatementSyntax BreakStatement(SyntaxToken breakKeyword, SyntaxToken semicolonToken);

-        public static CasePatternSwitchLabelSyntax CasePatternSwitchLabel(PatternSyntax pattern, WhenClauseSyntax whenClause, SyntaxToken colonToken);

-        public static CasePatternSwitchLabelSyntax CasePatternSwitchLabel(PatternSyntax pattern, SyntaxToken colonToken);

-        public static CasePatternSwitchLabelSyntax CasePatternSwitchLabel(SyntaxToken keyword, PatternSyntax pattern, WhenClauseSyntax whenClause, SyntaxToken colonToken);

-        public static CaseSwitchLabelSyntax CaseSwitchLabel(ExpressionSyntax value);

-        public static CaseSwitchLabelSyntax CaseSwitchLabel(ExpressionSyntax value, SyntaxToken colonToken);

-        public static CaseSwitchLabelSyntax CaseSwitchLabel(SyntaxToken keyword, ExpressionSyntax value, SyntaxToken colonToken);

-        public static CastExpressionSyntax CastExpression(TypeSyntax type, ExpressionSyntax expression);

-        public static CastExpressionSyntax CastExpression(SyntaxToken openParenToken, TypeSyntax type, SyntaxToken closeParenToken, ExpressionSyntax expression);

-        public static CatchClauseSyntax CatchClause();

-        public static CatchClauseSyntax CatchClause(CatchDeclarationSyntax declaration, CatchFilterClauseSyntax filter, BlockSyntax block);

-        public static CatchClauseSyntax CatchClause(SyntaxToken catchKeyword, CatchDeclarationSyntax declaration, CatchFilterClauseSyntax filter, BlockSyntax block);

-        public static CatchDeclarationSyntax CatchDeclaration(TypeSyntax type);

-        public static CatchDeclarationSyntax CatchDeclaration(TypeSyntax type, SyntaxToken identifier);

-        public static CatchDeclarationSyntax CatchDeclaration(SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken closeParenToken);

-        public static CatchFilterClauseSyntax CatchFilterClause(ExpressionSyntax filterExpression);

-        public static CatchFilterClauseSyntax CatchFilterClause(SyntaxToken whenKeyword, SyntaxToken openParenToken, ExpressionSyntax filterExpression, SyntaxToken closeParenToken);

-        public static CheckedExpressionSyntax CheckedExpression(SyntaxKind kind, ExpressionSyntax expression);

-        public static CheckedExpressionSyntax CheckedExpression(SyntaxKind kind, SyntaxToken keyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken);

-        public static CheckedStatementSyntax CheckedStatement(SyntaxKind kind, BlockSyntax block = null);

-        public static CheckedStatementSyntax CheckedStatement(SyntaxKind kind, SyntaxToken keyword, BlockSyntax block);

-        public static ClassDeclarationSyntax ClassDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxList<MemberDeclarationSyntax> members);

-        public static ClassDeclarationSyntax ClassDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public static ClassDeclarationSyntax ClassDeclaration(SyntaxToken identifier);

-        public static ClassDeclarationSyntax ClassDeclaration(string identifier);

-        public static ClassOrStructConstraintSyntax ClassOrStructConstraint(SyntaxKind kind);

-        public static ClassOrStructConstraintSyntax ClassOrStructConstraint(SyntaxKind kind, SyntaxToken classOrStructKeyword);

-        public static SyntaxTrivia Comment(string text);

-        public static CompilationUnitSyntax CompilationUnit();

-        public static CompilationUnitSyntax CompilationUnit(SyntaxList<ExternAliasDirectiveSyntax> externs, SyntaxList<UsingDirectiveSyntax> usings, SyntaxList<AttributeListSyntax> attributeLists, SyntaxList<MemberDeclarationSyntax> members);

-        public static CompilationUnitSyntax CompilationUnit(SyntaxList<ExternAliasDirectiveSyntax> externs, SyntaxList<UsingDirectiveSyntax> usings, SyntaxList<AttributeListSyntax> attributeLists, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken endOfFileToken);

-        public static ConditionalAccessExpressionSyntax ConditionalAccessExpression(ExpressionSyntax expression, ExpressionSyntax whenNotNull);

-        public static ConditionalAccessExpressionSyntax ConditionalAccessExpression(ExpressionSyntax expression, SyntaxToken operatorToken, ExpressionSyntax whenNotNull);

-        public static ConditionalExpressionSyntax ConditionalExpression(ExpressionSyntax condition, ExpressionSyntax whenTrue, ExpressionSyntax whenFalse);

-        public static ConditionalExpressionSyntax ConditionalExpression(ExpressionSyntax condition, SyntaxToken questionToken, ExpressionSyntax whenTrue, SyntaxToken colonToken, ExpressionSyntax whenFalse);

-        public static ConstantPatternSyntax ConstantPattern(ExpressionSyntax expression);

-        public static ConstructorConstraintSyntax ConstructorConstraint();

-        public static ConstructorConstraintSyntax ConstructorConstraint(SyntaxToken newKeyword, SyntaxToken openParenToken, SyntaxToken closeParenToken);

-        public static ConstructorDeclarationSyntax ConstructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, ConstructorInitializerSyntax initializer, ArrowExpressionClauseSyntax expressionBody);

-        public static ConstructorDeclarationSyntax ConstructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, ConstructorInitializerSyntax initializer, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static ConstructorDeclarationSyntax ConstructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, ConstructorInitializerSyntax initializer, BlockSyntax body);

-        public static ConstructorDeclarationSyntax ConstructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, ConstructorInitializerSyntax initializer, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody);

-        public static ConstructorDeclarationSyntax ConstructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, ConstructorInitializerSyntax initializer, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static ConstructorDeclarationSyntax ConstructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, ConstructorInitializerSyntax initializer, BlockSyntax body, SyntaxToken semicolonToken);

-        public static ConstructorDeclarationSyntax ConstructorDeclaration(SyntaxToken identifier);

-        public static ConstructorDeclarationSyntax ConstructorDeclaration(string identifier);

-        public static ConstructorInitializerSyntax ConstructorInitializer(SyntaxKind kind, ArgumentListSyntax argumentList = null);

-        public static ConstructorInitializerSyntax ConstructorInitializer(SyntaxKind kind, SyntaxToken colonToken, SyntaxToken thisOrBaseKeyword, ArgumentListSyntax argumentList);

-        public static ContinueStatementSyntax ContinueStatement();

-        public static ContinueStatementSyntax ContinueStatement(SyntaxToken continueKeyword, SyntaxToken semicolonToken);

-        public static ConversionOperatorDeclarationSyntax ConversionOperatorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken implicitOrExplicitKeyword, TypeSyntax type, ParameterListSyntax parameterList, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody);

-        public static ConversionOperatorDeclarationSyntax ConversionOperatorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken implicitOrExplicitKeyword, SyntaxToken operatorKeyword, TypeSyntax type, ParameterListSyntax parameterList, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static ConversionOperatorDeclarationSyntax ConversionOperatorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken implicitOrExplicitKeyword, SyntaxToken operatorKeyword, TypeSyntax type, ParameterListSyntax parameterList, BlockSyntax body, SyntaxToken semicolonToken);

-        public static ConversionOperatorDeclarationSyntax ConversionOperatorDeclaration(SyntaxToken implicitOrExplicitKeyword, TypeSyntax type);

-        public static ConversionOperatorMemberCrefSyntax ConversionOperatorMemberCref(SyntaxToken implicitOrExplicitKeyword, TypeSyntax type);

-        public static ConversionOperatorMemberCrefSyntax ConversionOperatorMemberCref(SyntaxToken implicitOrExplicitKeyword, TypeSyntax type, CrefParameterListSyntax parameters);

-        public static ConversionOperatorMemberCrefSyntax ConversionOperatorMemberCref(SyntaxToken implicitOrExplicitKeyword, SyntaxToken operatorKeyword, TypeSyntax type, CrefParameterListSyntax parameters);

-        public static CrefBracketedParameterListSyntax CrefBracketedParameterList(SeparatedSyntaxList<CrefParameterSyntax> parameters = default(SeparatedSyntaxList<CrefParameterSyntax>));

-        public static CrefBracketedParameterListSyntax CrefBracketedParameterList(SyntaxToken openBracketToken, SeparatedSyntaxList<CrefParameterSyntax> parameters, SyntaxToken closeBracketToken);

-        public static CrefParameterSyntax CrefParameter(TypeSyntax type);

-        public static CrefParameterSyntax CrefParameter(SyntaxToken refKindKeyword, TypeSyntax type);

-        public static CrefParameterListSyntax CrefParameterList(SeparatedSyntaxList<CrefParameterSyntax> parameters = default(SeparatedSyntaxList<CrefParameterSyntax>));

-        public static CrefParameterListSyntax CrefParameterList(SyntaxToken openParenToken, SeparatedSyntaxList<CrefParameterSyntax> parameters, SyntaxToken closeParenToken);

-        public static DeclarationExpressionSyntax DeclarationExpression(TypeSyntax type, VariableDesignationSyntax designation);

-        public static DeclarationPatternSyntax DeclarationPattern(TypeSyntax type, VariableDesignationSyntax designation);

-        public static DefaultExpressionSyntax DefaultExpression(TypeSyntax type);

-        public static DefaultExpressionSyntax DefaultExpression(SyntaxToken keyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken closeParenToken);

-        public static DefaultSwitchLabelSyntax DefaultSwitchLabel();

-        public static DefaultSwitchLabelSyntax DefaultSwitchLabel(SyntaxToken colonToken);

-        public static DefaultSwitchLabelSyntax DefaultSwitchLabel(SyntaxToken keyword, SyntaxToken colonToken);

-        public static DefineDirectiveTriviaSyntax DefineDirectiveTrivia(SyntaxToken hashToken, SyntaxToken defineKeyword, SyntaxToken name, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static DefineDirectiveTriviaSyntax DefineDirectiveTrivia(SyntaxToken name, bool isActive);

-        public static DefineDirectiveTriviaSyntax DefineDirectiveTrivia(string name, bool isActive);

-        public static DelegateDeclarationSyntax DelegateDeclaration(TypeSyntax returnType, SyntaxToken identifier);

-        public static DelegateDeclarationSyntax DelegateDeclaration(TypeSyntax returnType, string identifier);

-        public static DelegateDeclarationSyntax DelegateDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses);

-        public static DelegateDeclarationSyntax DelegateDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken delegateKeyword, TypeSyntax returnType, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken semicolonToken);

-        public static DestructorDeclarationSyntax DestructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, ArrowExpressionClauseSyntax expressionBody);

-        public static DestructorDeclarationSyntax DestructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, BlockSyntax body);

-        public static DestructorDeclarationSyntax DestructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, ParameterListSyntax parameterList, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody);

-        public static DestructorDeclarationSyntax DestructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken tildeToken, SyntaxToken identifier, ParameterListSyntax parameterList, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static DestructorDeclarationSyntax DestructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken tildeToken, SyntaxToken identifier, ParameterListSyntax parameterList, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static DestructorDeclarationSyntax DestructorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken tildeToken, SyntaxToken identifier, ParameterListSyntax parameterList, BlockSyntax body, SyntaxToken semicolonToken);

-        public static DestructorDeclarationSyntax DestructorDeclaration(SyntaxToken identifier);

-        public static DestructorDeclarationSyntax DestructorDeclaration(string identifier);

-        public static SyntaxTrivia DisabledText(string text);

-        public static DiscardDesignationSyntax DiscardDesignation();

-        public static DiscardDesignationSyntax DiscardDesignation(SyntaxToken underscoreToken);

-        public static DocumentationCommentTriviaSyntax DocumentationComment(params XmlNodeSyntax[] content);

-        public static SyntaxTrivia DocumentationCommentExterior(string text);

-        public static DocumentationCommentTriviaSyntax DocumentationCommentTrivia(SyntaxKind kind, SyntaxList<XmlNodeSyntax> content = default(SyntaxList<XmlNodeSyntax>));

-        public static DocumentationCommentTriviaSyntax DocumentationCommentTrivia(SyntaxKind kind, SyntaxList<XmlNodeSyntax> content, SyntaxToken endOfComment);

-        public static DoStatementSyntax DoStatement(StatementSyntax statement, ExpressionSyntax condition);

-        public static DoStatementSyntax DoStatement(SyntaxToken doKeyword, StatementSyntax statement, SyntaxToken whileKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, SyntaxToken semicolonToken);

-        public static SyntaxTrivia ElasticEndOfLine(string text);

-        public static SyntaxTrivia ElasticWhitespace(string text);

-        public static ElementAccessExpressionSyntax ElementAccessExpression(ExpressionSyntax expression);

-        public static ElementAccessExpressionSyntax ElementAccessExpression(ExpressionSyntax expression, BracketedArgumentListSyntax argumentList);

-        public static ElementBindingExpressionSyntax ElementBindingExpression();

-        public static ElementBindingExpressionSyntax ElementBindingExpression(BracketedArgumentListSyntax argumentList);

-        public static ElifDirectiveTriviaSyntax ElifDirectiveTrivia(ExpressionSyntax condition, bool isActive, bool branchTaken, bool conditionValue);

-        public static ElifDirectiveTriviaSyntax ElifDirectiveTrivia(SyntaxToken hashToken, SyntaxToken elifKeyword, ExpressionSyntax condition, SyntaxToken endOfDirectiveToken, bool isActive, bool branchTaken, bool conditionValue);

-        public static ElseClauseSyntax ElseClause(StatementSyntax statement);

-        public static ElseClauseSyntax ElseClause(SyntaxToken elseKeyword, StatementSyntax statement);

-        public static ElseDirectiveTriviaSyntax ElseDirectiveTrivia(SyntaxToken hashToken, SyntaxToken elseKeyword, SyntaxToken endOfDirectiveToken, bool isActive, bool branchTaken);

-        public static ElseDirectiveTriviaSyntax ElseDirectiveTrivia(bool isActive, bool branchTaken);

-        public static EmptyStatementSyntax EmptyStatement();

-        public static EmptyStatementSyntax EmptyStatement(SyntaxToken semicolonToken);

-        public static EndIfDirectiveTriviaSyntax EndIfDirectiveTrivia(SyntaxToken hashToken, SyntaxToken endIfKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static EndIfDirectiveTriviaSyntax EndIfDirectiveTrivia(bool isActive);

-        public static SyntaxTrivia EndOfLine(string text);

-        public static SyntaxTrivia EndOfLine(string text, bool elastic);

-        public static EndRegionDirectiveTriviaSyntax EndRegionDirectiveTrivia(SyntaxToken hashToken, SyntaxToken endRegionKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static EndRegionDirectiveTriviaSyntax EndRegionDirectiveTrivia(bool isActive);

-        public static EnumDeclarationSyntax EnumDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, BaseListSyntax baseList, SeparatedSyntaxList<EnumMemberDeclarationSyntax> members);

-        public static EnumDeclarationSyntax EnumDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken enumKeyword, SyntaxToken identifier, BaseListSyntax baseList, SyntaxToken openBraceToken, SeparatedSyntaxList<EnumMemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public static EnumDeclarationSyntax EnumDeclaration(SyntaxToken identifier);

-        public static EnumDeclarationSyntax EnumDeclaration(string identifier);

-        public static EnumMemberDeclarationSyntax EnumMemberDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxToken identifier, EqualsValueClauseSyntax equalsValue);

-        public static EnumMemberDeclarationSyntax EnumMemberDeclaration(SyntaxToken identifier);

-        public static EnumMemberDeclarationSyntax EnumMemberDeclaration(string identifier);

-        public static EqualsValueClauseSyntax EqualsValueClause(ExpressionSyntax value);

-        public static EqualsValueClauseSyntax EqualsValueClause(SyntaxToken equalsToken, ExpressionSyntax value);

-        public static ErrorDirectiveTriviaSyntax ErrorDirectiveTrivia(SyntaxToken hashToken, SyntaxToken errorKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static ErrorDirectiveTriviaSyntax ErrorDirectiveTrivia(bool isActive);

-        public static EventDeclarationSyntax EventDeclaration(TypeSyntax type, SyntaxToken identifier);

-        public static EventDeclarationSyntax EventDeclaration(TypeSyntax type, string identifier);

-        public static EventDeclarationSyntax EventDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList);

-        public static EventDeclarationSyntax EventDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken eventKeyword, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList);

-        public static EventFieldDeclarationSyntax EventFieldDeclaration(VariableDeclarationSyntax declaration);

-        public static EventFieldDeclarationSyntax EventFieldDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, VariableDeclarationSyntax declaration);

-        public static EventFieldDeclarationSyntax EventFieldDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken eventKeyword, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken);

-        public static ExplicitInterfaceSpecifierSyntax ExplicitInterfaceSpecifier(NameSyntax name);

-        public static ExplicitInterfaceSpecifierSyntax ExplicitInterfaceSpecifier(NameSyntax name, SyntaxToken dotToken);

-        public static ExpressionStatementSyntax ExpressionStatement(ExpressionSyntax expression);

-        public static ExpressionStatementSyntax ExpressionStatement(ExpressionSyntax expression, SyntaxToken semicolonToken);

-        public static ExternAliasDirectiveSyntax ExternAliasDirective(SyntaxToken identifier);

-        public static ExternAliasDirectiveSyntax ExternAliasDirective(SyntaxToken externKeyword, SyntaxToken aliasKeyword, SyntaxToken identifier, SyntaxToken semicolonToken);

-        public static ExternAliasDirectiveSyntax ExternAliasDirective(string identifier);

-        public static FieldDeclarationSyntax FieldDeclaration(VariableDeclarationSyntax declaration);

-        public static FieldDeclarationSyntax FieldDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, VariableDeclarationSyntax declaration);

-        public static FieldDeclarationSyntax FieldDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken);

-        public static FinallyClauseSyntax FinallyClause(BlockSyntax block = null);

-        public static FinallyClauseSyntax FinallyClause(SyntaxToken finallyKeyword, BlockSyntax block);

-        public static FixedStatementSyntax FixedStatement(VariableDeclarationSyntax declaration, StatementSyntax statement);

-        public static FixedStatementSyntax FixedStatement(SyntaxToken fixedKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, SyntaxToken closeParenToken, StatementSyntax statement);

-        public static ForEachStatementSyntax ForEachStatement(TypeSyntax type, SyntaxToken identifier, ExpressionSyntax expression, StatementSyntax statement);

-        public static ForEachStatementSyntax ForEachStatement(TypeSyntax type, string identifier, ExpressionSyntax expression, StatementSyntax statement);

-        public static ForEachStatementSyntax ForEachStatement(SyntaxToken forEachKeyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement);

-        public static ForEachVariableStatementSyntax ForEachVariableStatement(ExpressionSyntax variable, ExpressionSyntax expression, StatementSyntax statement);

-        public static ForEachVariableStatementSyntax ForEachVariableStatement(SyntaxToken forEachKeyword, SyntaxToken openParenToken, ExpressionSyntax variable, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement);

-        public static ForStatementSyntax ForStatement(StatementSyntax statement);

-        public static ForStatementSyntax ForStatement(VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, ExpressionSyntax condition, SeparatedSyntaxList<ExpressionSyntax> incrementors, StatementSyntax statement);

-        public static ForStatementSyntax ForStatement(SyntaxToken forKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, SyntaxToken firstSemicolonToken, ExpressionSyntax condition, SyntaxToken secondSemicolonToken, SeparatedSyntaxList<ExpressionSyntax> incrementors, SyntaxToken closeParenToken, StatementSyntax statement);

-        public static FromClauseSyntax FromClause(TypeSyntax type, SyntaxToken identifier, ExpressionSyntax expression);

-        public static FromClauseSyntax FromClause(SyntaxToken identifier, ExpressionSyntax expression);

-        public static FromClauseSyntax FromClause(SyntaxToken fromKeyword, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression);

-        public static FromClauseSyntax FromClause(string identifier, ExpressionSyntax expression);

-        public static GenericNameSyntax GenericName(SyntaxToken identifier);

-        public static GenericNameSyntax GenericName(SyntaxToken identifier, TypeArgumentListSyntax typeArgumentList);

-        public static GenericNameSyntax GenericName(string identifier);

-        public static ExpressionSyntax GetNonGenericExpression(ExpressionSyntax expression);

-        public static ExpressionSyntax GetStandaloneExpression(ExpressionSyntax expression);

-        public static GlobalStatementSyntax GlobalStatement(StatementSyntax statement);

-        public static GotoStatementSyntax GotoStatement(SyntaxKind kind, ExpressionSyntax expression = null);

-        public static GotoStatementSyntax GotoStatement(SyntaxKind kind, SyntaxToken caseOrDefaultKeyword, ExpressionSyntax expression);

-        public static GotoStatementSyntax GotoStatement(SyntaxKind kind, SyntaxToken gotoKeyword, SyntaxToken caseOrDefaultKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken);

-        public static GroupClauseSyntax GroupClause(ExpressionSyntax groupExpression, ExpressionSyntax byExpression);

-        public static GroupClauseSyntax GroupClause(SyntaxToken groupKeyword, ExpressionSyntax groupExpression, SyntaxToken byKeyword, ExpressionSyntax byExpression);

-        public static SyntaxToken Identifier(SyntaxTriviaList leading, SyntaxKind contextualKind, string text, string valueText, SyntaxTriviaList trailing);

-        public static SyntaxToken Identifier(SyntaxTriviaList leading, string text, SyntaxTriviaList trailing);

-        public static SyntaxToken Identifier(string text);

-        public static IdentifierNameSyntax IdentifierName(SyntaxToken identifier);

-        public static IdentifierNameSyntax IdentifierName(string name);

-        public static IfDirectiveTriviaSyntax IfDirectiveTrivia(ExpressionSyntax condition, bool isActive, bool branchTaken, bool conditionValue);

-        public static IfDirectiveTriviaSyntax IfDirectiveTrivia(SyntaxToken hashToken, SyntaxToken ifKeyword, ExpressionSyntax condition, SyntaxToken endOfDirectiveToken, bool isActive, bool branchTaken, bool conditionValue);

-        public static IfStatementSyntax IfStatement(ExpressionSyntax condition, StatementSyntax statement);

-        public static IfStatementSyntax IfStatement(ExpressionSyntax condition, StatementSyntax statement, ElseClauseSyntax @else);

-        public static IfStatementSyntax IfStatement(SyntaxToken ifKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement, ElseClauseSyntax @else);

-        public static ImplicitArrayCreationExpressionSyntax ImplicitArrayCreationExpression(InitializerExpressionSyntax initializer);

-        public static ImplicitArrayCreationExpressionSyntax ImplicitArrayCreationExpression(SyntaxToken newKeyword, SyntaxToken openBracketToken, SyntaxTokenList commas, SyntaxToken closeBracketToken, InitializerExpressionSyntax initializer);

-        public static ImplicitArrayCreationExpressionSyntax ImplicitArrayCreationExpression(SyntaxTokenList commas, InitializerExpressionSyntax initializer);

-        public static ImplicitElementAccessSyntax ImplicitElementAccess();

-        public static ImplicitElementAccessSyntax ImplicitElementAccess(BracketedArgumentListSyntax argumentList);

-        public static ImplicitStackAllocArrayCreationExpressionSyntax ImplicitStackAllocArrayCreationExpression(InitializerExpressionSyntax initializer);

-        public static ImplicitStackAllocArrayCreationExpressionSyntax ImplicitStackAllocArrayCreationExpression(SyntaxToken stackAllocKeyword, SyntaxToken openBracketToken, SyntaxToken closeBracketToken, InitializerExpressionSyntax initializer);

-        public static IncompleteMemberSyntax IncompleteMember(TypeSyntax type = null);

-        public static IncompleteMemberSyntax IncompleteMember(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type);

-        public static IndexerDeclarationSyntax IndexerDeclaration(TypeSyntax type);

-        public static IndexerDeclarationSyntax IndexerDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, BracketedParameterListSyntax parameterList, AccessorListSyntax accessorList);

-        public static IndexerDeclarationSyntax IndexerDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, BracketedParameterListSyntax parameterList, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody);

-        public static IndexerDeclarationSyntax IndexerDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken thisKeyword, BracketedParameterListSyntax parameterList, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static IndexerMemberCrefSyntax IndexerMemberCref(CrefBracketedParameterListSyntax parameters = null);

-        public static IndexerMemberCrefSyntax IndexerMemberCref(SyntaxToken thisKeyword, CrefBracketedParameterListSyntax parameters);

-        public static InitializerExpressionSyntax InitializerExpression(SyntaxKind kind, SeparatedSyntaxList<ExpressionSyntax> expressions = default(SeparatedSyntaxList<ExpressionSyntax>));

-        public static InitializerExpressionSyntax InitializerExpression(SyntaxKind kind, SyntaxToken openBraceToken, SeparatedSyntaxList<ExpressionSyntax> expressions, SyntaxToken closeBraceToken);

-        public static InterfaceDeclarationSyntax InterfaceDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxList<MemberDeclarationSyntax> members);

-        public static InterfaceDeclarationSyntax InterfaceDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public static InterfaceDeclarationSyntax InterfaceDeclaration(SyntaxToken identifier);

-        public static InterfaceDeclarationSyntax InterfaceDeclaration(string identifier);

-        public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxToken stringStartToken);

-        public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxToken stringStartToken, SyntaxList<InterpolatedStringContentSyntax> contents);

-        public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxToken stringStartToken, SyntaxList<InterpolatedStringContentSyntax> contents, SyntaxToken stringEndToken);

-        public static InterpolatedStringTextSyntax InterpolatedStringText();

-        public static InterpolatedStringTextSyntax InterpolatedStringText(SyntaxToken textToken);

-        public static InterpolationSyntax Interpolation(ExpressionSyntax expression);

-        public static InterpolationSyntax Interpolation(ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause, InterpolationFormatClauseSyntax formatClause);

-        public static InterpolationSyntax Interpolation(SyntaxToken openBraceToken, ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause, InterpolationFormatClauseSyntax formatClause, SyntaxToken closeBraceToken);

-        public static InterpolationAlignmentClauseSyntax InterpolationAlignmentClause(SyntaxToken commaToken, ExpressionSyntax value);

-        public static InterpolationFormatClauseSyntax InterpolationFormatClause(SyntaxToken colonToken);

-        public static InterpolationFormatClauseSyntax InterpolationFormatClause(SyntaxToken colonToken, SyntaxToken formatStringToken);

-        public static InvocationExpressionSyntax InvocationExpression(ExpressionSyntax expression);

-        public static InvocationExpressionSyntax InvocationExpression(ExpressionSyntax expression, ArgumentListSyntax argumentList);

-        public static bool IsCompleteSubmission(SyntaxTree tree);

-        public static IsPatternExpressionSyntax IsPatternExpression(ExpressionSyntax expression, PatternSyntax pattern);

-        public static IsPatternExpressionSyntax IsPatternExpression(ExpressionSyntax expression, SyntaxToken isKeyword, PatternSyntax pattern);

-        public static JoinClauseSyntax JoinClause(TypeSyntax type, SyntaxToken identifier, ExpressionSyntax inExpression, ExpressionSyntax leftExpression, ExpressionSyntax rightExpression, JoinIntoClauseSyntax into);

-        public static JoinClauseSyntax JoinClause(SyntaxToken identifier, ExpressionSyntax inExpression, ExpressionSyntax leftExpression, ExpressionSyntax rightExpression);

-        public static JoinClauseSyntax JoinClause(SyntaxToken joinKeyword, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax inExpression, SyntaxToken onKeyword, ExpressionSyntax leftExpression, SyntaxToken equalsKeyword, ExpressionSyntax rightExpression, JoinIntoClauseSyntax into);

-        public static JoinClauseSyntax JoinClause(string identifier, ExpressionSyntax inExpression, ExpressionSyntax leftExpression, ExpressionSyntax rightExpression);

-        public static JoinIntoClauseSyntax JoinIntoClause(SyntaxToken identifier);

-        public static JoinIntoClauseSyntax JoinIntoClause(SyntaxToken intoKeyword, SyntaxToken identifier);

-        public static JoinIntoClauseSyntax JoinIntoClause(string identifier);

-        public static LabeledStatementSyntax LabeledStatement(SyntaxToken identifier, StatementSyntax statement);

-        public static LabeledStatementSyntax LabeledStatement(SyntaxToken identifier, SyntaxToken colonToken, StatementSyntax statement);

-        public static LabeledStatementSyntax LabeledStatement(string identifier, StatementSyntax statement);

-        public static LetClauseSyntax LetClause(SyntaxToken identifier, ExpressionSyntax expression);

-        public static LetClauseSyntax LetClause(SyntaxToken letKeyword, SyntaxToken identifier, SyntaxToken equalsToken, ExpressionSyntax expression);

-        public static LetClauseSyntax LetClause(string identifier, ExpressionSyntax expression);

-        public static LineDirectiveTriviaSyntax LineDirectiveTrivia(SyntaxToken hashToken, SyntaxToken lineKeyword, SyntaxToken line, SyntaxToken file, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static LineDirectiveTriviaSyntax LineDirectiveTrivia(SyntaxToken line, SyntaxToken file, bool isActive);

-        public static LineDirectiveTriviaSyntax LineDirectiveTrivia(SyntaxToken line, bool isActive);

-        public static SyntaxList<TNode> List<TNode>() where TNode : SyntaxNode;

-        public static SyntaxList<TNode> List<TNode>(IEnumerable<TNode> nodes) where TNode : SyntaxNode;

-        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, char value, SyntaxTriviaList trailing);

-        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, decimal value, SyntaxTriviaList trailing);

-        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, double value, SyntaxTriviaList trailing);

-        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, int value, SyntaxTriviaList trailing);

-        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, long value, SyntaxTriviaList trailing);

-        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, float value, SyntaxTriviaList trailing);

-        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, string value, SyntaxTriviaList trailing);

-        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, uint value, SyntaxTriviaList trailing);

-        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, ulong value, SyntaxTriviaList trailing);

-        public static SyntaxToken Literal(char value);

-        public static SyntaxToken Literal(decimal value);

-        public static SyntaxToken Literal(double value);

-        public static SyntaxToken Literal(int value);

-        public static SyntaxToken Literal(long value);

-        public static SyntaxToken Literal(float value);

-        public static SyntaxToken Literal(string value);

-        public static SyntaxToken Literal(string text, char value);

-        public static SyntaxToken Literal(string text, decimal value);

-        public static SyntaxToken Literal(string text, double value);

-        public static SyntaxToken Literal(string text, int value);

-        public static SyntaxToken Literal(string text, long value);

-        public static SyntaxToken Literal(string text, float value);

-        public static SyntaxToken Literal(string text, string value);

-        public static SyntaxToken Literal(string text, uint value);

-        public static SyntaxToken Literal(string text, ulong value);

-        public static SyntaxToken Literal(uint value);

-        public static SyntaxToken Literal(ulong value);

-        public static LiteralExpressionSyntax LiteralExpression(SyntaxKind kind);

-        public static LiteralExpressionSyntax LiteralExpression(SyntaxKind kind, SyntaxToken token);

-        public static LoadDirectiveTriviaSyntax LoadDirectiveTrivia(SyntaxToken hashToken, SyntaxToken loadKeyword, SyntaxToken file, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static LoadDirectiveTriviaSyntax LoadDirectiveTrivia(SyntaxToken file, bool isActive);

-        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(VariableDeclarationSyntax declaration);

-        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration);

-        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken);

-        public static LocalFunctionStatementSyntax LocalFunctionStatement(TypeSyntax returnType, SyntaxToken identifier);

-        public static LocalFunctionStatementSyntax LocalFunctionStatement(TypeSyntax returnType, string identifier);

-        public static LocalFunctionStatementSyntax LocalFunctionStatement(SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody);

-        public static LocalFunctionStatementSyntax LocalFunctionStatement(SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static LockStatementSyntax LockStatement(ExpressionSyntax expression, StatementSyntax statement);

-        public static LockStatementSyntax LockStatement(SyntaxToken lockKeyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement);

-        public static MakeRefExpressionSyntax MakeRefExpression(ExpressionSyntax expression);

-        public static MakeRefExpressionSyntax MakeRefExpression(SyntaxToken keyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken);

-        public static MemberAccessExpressionSyntax MemberAccessExpression(SyntaxKind kind, ExpressionSyntax expression, SimpleNameSyntax name);

-        public static MemberAccessExpressionSyntax MemberAccessExpression(SyntaxKind kind, ExpressionSyntax expression, SyntaxToken operatorToken, SimpleNameSyntax name);

-        public static MemberBindingExpressionSyntax MemberBindingExpression(SimpleNameSyntax name);

-        public static MemberBindingExpressionSyntax MemberBindingExpression(SyntaxToken operatorToken, SimpleNameSyntax name);

-        public static MethodDeclarationSyntax MethodDeclaration(TypeSyntax returnType, SyntaxToken identifier);

-        public static MethodDeclarationSyntax MethodDeclaration(TypeSyntax returnType, string identifier);

-        public static MethodDeclarationSyntax MethodDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody);

-        public static MethodDeclarationSyntax MethodDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static MethodDeclarationSyntax MethodDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax body, SyntaxToken semicolonToken);

-        public static SyntaxToken MissingToken(SyntaxKind kind);

-        public static SyntaxToken MissingToken(SyntaxTriviaList leading, SyntaxKind kind, SyntaxTriviaList trailing);

-        public static NameColonSyntax NameColon(IdentifierNameSyntax name);

-        public static NameColonSyntax NameColon(IdentifierNameSyntax name, SyntaxToken colonToken);

-        public static NameColonSyntax NameColon(string name);

-        public static NameEqualsSyntax NameEquals(IdentifierNameSyntax name);

-        public static NameEqualsSyntax NameEquals(IdentifierNameSyntax name, SyntaxToken equalsToken);

-        public static NameEqualsSyntax NameEquals(string name);

-        public static NameMemberCrefSyntax NameMemberCref(TypeSyntax name);

-        public static NameMemberCrefSyntax NameMemberCref(TypeSyntax name, CrefParameterListSyntax parameters);

-        public static NamespaceDeclarationSyntax NamespaceDeclaration(NameSyntax name);

-        public static NamespaceDeclarationSyntax NamespaceDeclaration(NameSyntax name, SyntaxList<ExternAliasDirectiveSyntax> externs, SyntaxList<UsingDirectiveSyntax> usings, SyntaxList<MemberDeclarationSyntax> members);

-        public static NamespaceDeclarationSyntax NamespaceDeclaration(SyntaxToken namespaceKeyword, NameSyntax name, SyntaxToken openBraceToken, SyntaxList<ExternAliasDirectiveSyntax> externs, SyntaxList<UsingDirectiveSyntax> usings, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public static SyntaxNodeOrTokenList NodeOrTokenList();

-        public static SyntaxNodeOrTokenList NodeOrTokenList(params SyntaxNodeOrToken[] nodesAndTokens);

-        public static SyntaxNodeOrTokenList NodeOrTokenList(IEnumerable<SyntaxNodeOrToken> nodesAndTokens);

-        public static NullableTypeSyntax NullableType(TypeSyntax elementType);

-        public static NullableTypeSyntax NullableType(TypeSyntax elementType, SyntaxToken questionToken);

-        public static ObjectCreationExpressionSyntax ObjectCreationExpression(TypeSyntax type);

-        public static ObjectCreationExpressionSyntax ObjectCreationExpression(TypeSyntax type, ArgumentListSyntax argumentList, InitializerExpressionSyntax initializer);

-        public static ObjectCreationExpressionSyntax ObjectCreationExpression(SyntaxToken newKeyword, TypeSyntax type, ArgumentListSyntax argumentList, InitializerExpressionSyntax initializer);

-        public static OmittedArraySizeExpressionSyntax OmittedArraySizeExpression();

-        public static OmittedArraySizeExpressionSyntax OmittedArraySizeExpression(SyntaxToken omittedArraySizeExpressionToken);

-        public static OmittedTypeArgumentSyntax OmittedTypeArgument();

-        public static OmittedTypeArgumentSyntax OmittedTypeArgument(SyntaxToken omittedTypeArgumentToken);

-        public static OperatorDeclarationSyntax OperatorDeclaration(TypeSyntax returnType, SyntaxToken operatorToken);

-        public static OperatorDeclarationSyntax OperatorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken operatorToken, ParameterListSyntax parameterList, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody);

-        public static OperatorDeclarationSyntax OperatorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken operatorKeyword, SyntaxToken operatorToken, ParameterListSyntax parameterList, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);

-        public static OperatorDeclarationSyntax OperatorDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken operatorKeyword, SyntaxToken operatorToken, ParameterListSyntax parameterList, BlockSyntax body, SyntaxToken semicolonToken);

-        public static OperatorMemberCrefSyntax OperatorMemberCref(SyntaxToken operatorToken);

-        public static OperatorMemberCrefSyntax OperatorMemberCref(SyntaxToken operatorToken, CrefParameterListSyntax parameters);

-        public static OperatorMemberCrefSyntax OperatorMemberCref(SyntaxToken operatorKeyword, SyntaxToken operatorToken, CrefParameterListSyntax parameters);

-        public static OrderByClauseSyntax OrderByClause(SeparatedSyntaxList<OrderingSyntax> orderings = default(SeparatedSyntaxList<OrderingSyntax>));

-        public static OrderByClauseSyntax OrderByClause(SyntaxToken orderByKeyword, SeparatedSyntaxList<OrderingSyntax> orderings);

-        public static OrderingSyntax Ordering(SyntaxKind kind, ExpressionSyntax expression);

-        public static OrderingSyntax Ordering(SyntaxKind kind, ExpressionSyntax expression, SyntaxToken ascendingOrDescendingKeyword);

-        public static ParameterSyntax Parameter(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, SyntaxToken identifier, EqualsValueClauseSyntax @default);

-        public static ParameterSyntax Parameter(SyntaxToken identifier);

-        public static ParameterListSyntax ParameterList(SeparatedSyntaxList<ParameterSyntax> parameters = default(SeparatedSyntaxList<ParameterSyntax>));

-        public static ParameterListSyntax ParameterList(SyntaxToken openParenToken, SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeParenToken);

-        public static ParenthesizedExpressionSyntax ParenthesizedExpression(ExpressionSyntax expression);

-        public static ParenthesizedExpressionSyntax ParenthesizedExpression(SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken);

-        public static ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(CSharpSyntaxNode body);

-        public static ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(ParameterListSyntax parameterList, CSharpSyntaxNode body);

-        public static ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(SyntaxToken asyncKeyword, ParameterListSyntax parameterList, SyntaxToken arrowToken, CSharpSyntaxNode body);

-        public static ParenthesizedVariableDesignationSyntax ParenthesizedVariableDesignation(SeparatedSyntaxList<VariableDesignationSyntax> variables = default(SeparatedSyntaxList<VariableDesignationSyntax>));

-        public static ParenthesizedVariableDesignationSyntax ParenthesizedVariableDesignation(SyntaxToken openParenToken, SeparatedSyntaxList<VariableDesignationSyntax> variables, SyntaxToken closeParenToken);

-        public static ArgumentListSyntax ParseArgumentList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true);

-        public static AttributeArgumentListSyntax ParseAttributeArgumentList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true);

-        public static BracketedArgumentListSyntax ParseBracketedArgumentList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true);

-        public static BracketedParameterListSyntax ParseBracketedParameterList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true);

-        public static CompilationUnitSyntax ParseCompilationUnit(string text, int offset = 0, CSharpParseOptions options = null);

-        public static ExpressionSyntax ParseExpression(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true);

-        public static SyntaxTriviaList ParseLeadingTrivia(string text, int offset = 0);

-        public static NameSyntax ParseName(string text, int offset = 0, bool consumeFullText = true);

-        public static ParameterListSyntax ParseParameterList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true);

-        public static StatementSyntax ParseStatement(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true);

-        public static SyntaxTree ParseSyntaxTree(SourceText text, ParseOptions options = null, string path = "", CancellationToken cancellationToken = default(CancellationToken));

-        public static SyntaxTree ParseSyntaxTree(string text, ParseOptions options = null, string path = "", Encoding encoding = null, CancellationToken cancellationToken = default(CancellationToken));

-        public static SyntaxToken ParseToken(string text, int offset = 0);

-        public static IEnumerable<SyntaxToken> ParseTokens(string text, int offset = 0, int initialTokenPosition = 0, CSharpParseOptions options = null);

-        public static SyntaxTriviaList ParseTrailingTrivia(string text, int offset = 0);

-        public static TypeSyntax ParseTypeName(string text, int offset = 0, bool consumeFullText = true);

-        public static PointerTypeSyntax PointerType(TypeSyntax elementType);

-        public static PointerTypeSyntax PointerType(TypeSyntax elementType, SyntaxToken asteriskToken);

-        public static PostfixUnaryExpressionSyntax PostfixUnaryExpression(SyntaxKind kind, ExpressionSyntax operand);

-        public static PostfixUnaryExpressionSyntax PostfixUnaryExpression(SyntaxKind kind, ExpressionSyntax operand, SyntaxToken operatorToken);

-        public static PragmaChecksumDirectiveTriviaSyntax PragmaChecksumDirectiveTrivia(SyntaxToken hashToken, SyntaxToken pragmaKeyword, SyntaxToken checksumKeyword, SyntaxToken file, SyntaxToken guid, SyntaxToken bytes, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static PragmaChecksumDirectiveTriviaSyntax PragmaChecksumDirectiveTrivia(SyntaxToken file, SyntaxToken guid, SyntaxToken bytes, bool isActive);

-        public static PragmaWarningDirectiveTriviaSyntax PragmaWarningDirectiveTrivia(SyntaxToken disableOrRestoreKeyword, SeparatedSyntaxList<ExpressionSyntax> errorCodes, bool isActive);

-        public static PragmaWarningDirectiveTriviaSyntax PragmaWarningDirectiveTrivia(SyntaxToken hashToken, SyntaxToken pragmaKeyword, SyntaxToken warningKeyword, SyntaxToken disableOrRestoreKeyword, SeparatedSyntaxList<ExpressionSyntax> errorCodes, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static PragmaWarningDirectiveTriviaSyntax PragmaWarningDirectiveTrivia(SyntaxToken disableOrRestoreKeyword, bool isActive);

-        public static PredefinedTypeSyntax PredefinedType(SyntaxToken keyword);

-        public static PrefixUnaryExpressionSyntax PrefixUnaryExpression(SyntaxKind kind, ExpressionSyntax operand);

-        public static PrefixUnaryExpressionSyntax PrefixUnaryExpression(SyntaxKind kind, SyntaxToken operatorToken, ExpressionSyntax operand);

-        public static SyntaxTrivia PreprocessingMessage(string text);

-        public static PropertyDeclarationSyntax PropertyDeclaration(TypeSyntax type, SyntaxToken identifier);

-        public static PropertyDeclarationSyntax PropertyDeclaration(TypeSyntax type, string identifier);

-        public static PropertyDeclarationSyntax PropertyDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList);

-        public static PropertyDeclarationSyntax PropertyDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, EqualsValueClauseSyntax initializer);

-        public static PropertyDeclarationSyntax PropertyDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, EqualsValueClauseSyntax initializer, SyntaxToken semicolonToken);

-        public static QualifiedCrefSyntax QualifiedCref(TypeSyntax container, MemberCrefSyntax member);

-        public static QualifiedCrefSyntax QualifiedCref(TypeSyntax container, SyntaxToken dotToken, MemberCrefSyntax member);

-        public static QualifiedNameSyntax QualifiedName(NameSyntax left, SimpleNameSyntax right);

-        public static QualifiedNameSyntax QualifiedName(NameSyntax left, SyntaxToken dotToken, SimpleNameSyntax right);

-        public static QueryBodySyntax QueryBody(SelectOrGroupClauseSyntax selectOrGroup);

-        public static QueryBodySyntax QueryBody(SyntaxList<QueryClauseSyntax> clauses, SelectOrGroupClauseSyntax selectOrGroup, QueryContinuationSyntax continuation);

-        public static QueryContinuationSyntax QueryContinuation(SyntaxToken identifier, QueryBodySyntax body);

-        public static QueryContinuationSyntax QueryContinuation(SyntaxToken intoKeyword, SyntaxToken identifier, QueryBodySyntax body);

-        public static QueryContinuationSyntax QueryContinuation(string identifier, QueryBodySyntax body);

-        public static QueryExpressionSyntax QueryExpression(FromClauseSyntax fromClause, QueryBodySyntax body);

-        public static ReferenceDirectiveTriviaSyntax ReferenceDirectiveTrivia(SyntaxToken hashToken, SyntaxToken referenceKeyword, SyntaxToken file, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static ReferenceDirectiveTriviaSyntax ReferenceDirectiveTrivia(SyntaxToken file, bool isActive);

-        public static RefExpressionSyntax RefExpression(ExpressionSyntax expression);

-        public static RefExpressionSyntax RefExpression(SyntaxToken refKeyword, ExpressionSyntax expression);

-        public static RefTypeSyntax RefType(TypeSyntax type);

-        public static RefTypeSyntax RefType(SyntaxToken refKeyword, TypeSyntax type);

-        public static RefTypeSyntax RefType(SyntaxToken refKeyword, SyntaxToken readOnlyKeyword, TypeSyntax type);

-        public static RefTypeExpressionSyntax RefTypeExpression(ExpressionSyntax expression);

-        public static RefTypeExpressionSyntax RefTypeExpression(SyntaxToken keyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken);

-        public static RefValueExpressionSyntax RefValueExpression(ExpressionSyntax expression, TypeSyntax type);

-        public static RefValueExpressionSyntax RefValueExpression(SyntaxToken keyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken comma, TypeSyntax type, SyntaxToken closeParenToken);

-        public static RegionDirectiveTriviaSyntax RegionDirectiveTrivia(SyntaxToken hashToken, SyntaxToken regionKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static RegionDirectiveTriviaSyntax RegionDirectiveTrivia(bool isActive);

-        public static ReturnStatementSyntax ReturnStatement(ExpressionSyntax expression = null);

-        public static ReturnStatementSyntax ReturnStatement(SyntaxToken returnKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken);

-        public static SelectClauseSyntax SelectClause(ExpressionSyntax expression);

-        public static SelectClauseSyntax SelectClause(SyntaxToken selectKeyword, ExpressionSyntax expression);

-        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>() where TNode : SyntaxNode;

-        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>(SyntaxNodeOrTokenList nodesAndTokens) where TNode : SyntaxNode;

-        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>(IEnumerable<SyntaxNodeOrToken> nodesAndTokens) where TNode : SyntaxNode;

-        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>(IEnumerable<TNode> nodes) where TNode : SyntaxNode;

-        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>(IEnumerable<TNode> nodes, IEnumerable<SyntaxToken> separators) where TNode : SyntaxNode;

-        public static ShebangDirectiveTriviaSyntax ShebangDirectiveTrivia(SyntaxToken hashToken, SyntaxToken exclamationToken, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static ShebangDirectiveTriviaSyntax ShebangDirectiveTrivia(bool isActive);

-        public static SimpleBaseTypeSyntax SimpleBaseType(TypeSyntax type);

-        public static SimpleLambdaExpressionSyntax SimpleLambdaExpression(ParameterSyntax parameter, CSharpSyntaxNode body);

-        public static SimpleLambdaExpressionSyntax SimpleLambdaExpression(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, CSharpSyntaxNode body);

-        public static SyntaxList<TNode> SingletonList<TNode>(TNode node) where TNode : SyntaxNode;

-        public static SeparatedSyntaxList<TNode> SingletonSeparatedList<TNode>(TNode node) where TNode : SyntaxNode;

-        public static SingleVariableDesignationSyntax SingleVariableDesignation(SyntaxToken identifier);

-        public static SizeOfExpressionSyntax SizeOfExpression(TypeSyntax type);

-        public static SizeOfExpressionSyntax SizeOfExpression(SyntaxToken keyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken closeParenToken);

-        public static SkippedTokensTriviaSyntax SkippedTokensTrivia();

-        public static SkippedTokensTriviaSyntax SkippedTokensTrivia(SyntaxTokenList tokens);

-        public static StackAllocArrayCreationExpressionSyntax StackAllocArrayCreationExpression(TypeSyntax type);

-        public static StackAllocArrayCreationExpressionSyntax StackAllocArrayCreationExpression(TypeSyntax type, InitializerExpressionSyntax initializer);

-        public static StackAllocArrayCreationExpressionSyntax StackAllocArrayCreationExpression(SyntaxToken stackAllocKeyword, TypeSyntax type);

-        public static StackAllocArrayCreationExpressionSyntax StackAllocArrayCreationExpression(SyntaxToken stackAllocKeyword, TypeSyntax type, InitializerExpressionSyntax initializer);

-        public static StructDeclarationSyntax StructDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxList<MemberDeclarationSyntax> members);

-        public static StructDeclarationSyntax StructDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public static StructDeclarationSyntax StructDeclaration(SyntaxToken identifier);

-        public static StructDeclarationSyntax StructDeclaration(string identifier);

-        public static SwitchSectionSyntax SwitchSection();

-        public static SwitchSectionSyntax SwitchSection(SyntaxList<SwitchLabelSyntax> labels, SyntaxList<StatementSyntax> statements);

-        public static SwitchStatementSyntax SwitchStatement(ExpressionSyntax expression);

-        public static SwitchStatementSyntax SwitchStatement(ExpressionSyntax expression, SyntaxList<SwitchSectionSyntax> sections);

-        public static SwitchStatementSyntax SwitchStatement(SyntaxToken switchKeyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken, SyntaxToken openBraceToken, SyntaxList<SwitchSectionSyntax> sections, SyntaxToken closeBraceToken);

-        public static SyntaxTree SyntaxTree(SyntaxNode root, ParseOptions options = null, string path = "", Encoding encoding = null);

-        public static SyntaxTrivia SyntaxTrivia(SyntaxKind kind, string text);

-        public static ThisExpressionSyntax ThisExpression();

-        public static ThisExpressionSyntax ThisExpression(SyntaxToken token);

-        public static ThrowExpressionSyntax ThrowExpression(ExpressionSyntax expression);

-        public static ThrowExpressionSyntax ThrowExpression(SyntaxToken throwKeyword, ExpressionSyntax expression);

-        public static ThrowStatementSyntax ThrowStatement(ExpressionSyntax expression = null);

-        public static ThrowStatementSyntax ThrowStatement(SyntaxToken throwKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken);

-        public static SyntaxToken Token(SyntaxKind kind);

-        public static SyntaxToken Token(SyntaxTriviaList leading, SyntaxKind kind, SyntaxTriviaList trailing);

-        public static SyntaxToken Token(SyntaxTriviaList leading, SyntaxKind kind, string text, string valueText, SyntaxTriviaList trailing);

-        public static SyntaxTokenList TokenList();

-        public static SyntaxTokenList TokenList(SyntaxToken token);

-        public static SyntaxTokenList TokenList(params SyntaxToken[] tokens);

-        public static SyntaxTokenList TokenList(IEnumerable<SyntaxToken> tokens);

-        public static SyntaxTrivia Trivia(StructuredTriviaSyntax node);

-        public static SyntaxTriviaList TriviaList();

-        public static SyntaxTriviaList TriviaList(SyntaxTrivia trivia);

-        public static SyntaxTriviaList TriviaList(params SyntaxTrivia[] trivias);

-        public static SyntaxTriviaList TriviaList(IEnumerable<SyntaxTrivia> trivias);

-        public static TryStatementSyntax TryStatement(BlockSyntax block, SyntaxList<CatchClauseSyntax> catches, FinallyClauseSyntax @finally);

-        public static TryStatementSyntax TryStatement(SyntaxList<CatchClauseSyntax> catches = default(SyntaxList<CatchClauseSyntax>));

-        public static TryStatementSyntax TryStatement(SyntaxToken tryKeyword, BlockSyntax block, SyntaxList<CatchClauseSyntax> catches, FinallyClauseSyntax @finally);

-        public static TupleElementSyntax TupleElement(TypeSyntax type);

-        public static TupleElementSyntax TupleElement(TypeSyntax type, SyntaxToken identifier);

-        public static TupleExpressionSyntax TupleExpression(SeparatedSyntaxList<ArgumentSyntax> arguments = default(SeparatedSyntaxList<ArgumentSyntax>));

-        public static TupleExpressionSyntax TupleExpression(SyntaxToken openParenToken, SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxToken closeParenToken);

-        public static TupleTypeSyntax TupleType(SeparatedSyntaxList<TupleElementSyntax> elements = default(SeparatedSyntaxList<TupleElementSyntax>));

-        public static TupleTypeSyntax TupleType(SyntaxToken openParenToken, SeparatedSyntaxList<TupleElementSyntax> elements, SyntaxToken closeParenToken);

-        public static TypeArgumentListSyntax TypeArgumentList(SeparatedSyntaxList<TypeSyntax> arguments = default(SeparatedSyntaxList<TypeSyntax>));

-        public static TypeArgumentListSyntax TypeArgumentList(SyntaxToken lessThanToken, SeparatedSyntaxList<TypeSyntax> arguments, SyntaxToken greaterThanToken);

-        public static TypeConstraintSyntax TypeConstraint(TypeSyntax type);

-        public static TypeCrefSyntax TypeCref(TypeSyntax type);

-        public static TypeDeclarationSyntax TypeDeclaration(SyntaxKind kind, SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken);

-        public static TypeDeclarationSyntax TypeDeclaration(SyntaxKind kind, SyntaxToken identifier);

-        public static TypeDeclarationSyntax TypeDeclaration(SyntaxKind kind, string identifier);

-        public static TypeOfExpressionSyntax TypeOfExpression(TypeSyntax type);

-        public static TypeOfExpressionSyntax TypeOfExpression(SyntaxToken keyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken closeParenToken);

-        public static TypeParameterSyntax TypeParameter(SyntaxList<AttributeListSyntax> attributeLists, SyntaxToken varianceKeyword, SyntaxToken identifier);

-        public static TypeParameterSyntax TypeParameter(SyntaxToken identifier);

-        public static TypeParameterSyntax TypeParameter(string identifier);

-        public static TypeParameterConstraintClauseSyntax TypeParameterConstraintClause(IdentifierNameSyntax name);

-        public static TypeParameterConstraintClauseSyntax TypeParameterConstraintClause(IdentifierNameSyntax name, SeparatedSyntaxList<TypeParameterConstraintSyntax> constraints);

-        public static TypeParameterConstraintClauseSyntax TypeParameterConstraintClause(SyntaxToken whereKeyword, IdentifierNameSyntax name, SyntaxToken colonToken, SeparatedSyntaxList<TypeParameterConstraintSyntax> constraints);

-        public static TypeParameterConstraintClauseSyntax TypeParameterConstraintClause(string name);

-        public static TypeParameterListSyntax TypeParameterList(SeparatedSyntaxList<TypeParameterSyntax> parameters = default(SeparatedSyntaxList<TypeParameterSyntax>));

-        public static TypeParameterListSyntax TypeParameterList(SyntaxToken lessThanToken, SeparatedSyntaxList<TypeParameterSyntax> parameters, SyntaxToken greaterThanToken);

-        public static UndefDirectiveTriviaSyntax UndefDirectiveTrivia(SyntaxToken hashToken, SyntaxToken undefKeyword, SyntaxToken name, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static UndefDirectiveTriviaSyntax UndefDirectiveTrivia(SyntaxToken name, bool isActive);

-        public static UndefDirectiveTriviaSyntax UndefDirectiveTrivia(string name, bool isActive);

-        public static UnsafeStatementSyntax UnsafeStatement(BlockSyntax block = null);

-        public static UnsafeStatementSyntax UnsafeStatement(SyntaxToken unsafeKeyword, BlockSyntax block);

-        public static UsingDirectiveSyntax UsingDirective(NameEqualsSyntax alias, NameSyntax name);

-        public static UsingDirectiveSyntax UsingDirective(NameSyntax name);

-        public static UsingDirectiveSyntax UsingDirective(SyntaxToken staticKeyword, NameEqualsSyntax alias, NameSyntax name);

-        public static UsingDirectiveSyntax UsingDirective(SyntaxToken usingKeyword, SyntaxToken staticKeyword, NameEqualsSyntax alias, NameSyntax name, SyntaxToken semicolonToken);

-        public static UsingStatementSyntax UsingStatement(StatementSyntax statement);

-        public static UsingStatementSyntax UsingStatement(VariableDeclarationSyntax declaration, ExpressionSyntax expression, StatementSyntax statement);

-        public static UsingStatementSyntax UsingStatement(SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement);

-        public static VariableDeclarationSyntax VariableDeclaration(TypeSyntax type);

-        public static VariableDeclarationSyntax VariableDeclaration(TypeSyntax type, SeparatedSyntaxList<VariableDeclaratorSyntax> variables);

-        public static VariableDeclaratorSyntax VariableDeclarator(SyntaxToken identifier);

-        public static VariableDeclaratorSyntax VariableDeclarator(SyntaxToken identifier, BracketedArgumentListSyntax argumentList, EqualsValueClauseSyntax initializer);

-        public static VariableDeclaratorSyntax VariableDeclarator(string identifier);

-        public static SyntaxToken VerbatimIdentifier(SyntaxTriviaList leading, string text, string valueText, SyntaxTriviaList trailing);

-        public static WarningDirectiveTriviaSyntax WarningDirectiveTrivia(SyntaxToken hashToken, SyntaxToken warningKeyword, SyntaxToken endOfDirectiveToken, bool isActive);

-        public static WarningDirectiveTriviaSyntax WarningDirectiveTrivia(bool isActive);

-        public static WhenClauseSyntax WhenClause(ExpressionSyntax condition);

-        public static WhenClauseSyntax WhenClause(SyntaxToken whenKeyword, ExpressionSyntax condition);

-        public static WhereClauseSyntax WhereClause(ExpressionSyntax condition);

-        public static WhereClauseSyntax WhereClause(SyntaxToken whereKeyword, ExpressionSyntax condition);

-        public static WhileStatementSyntax WhileStatement(ExpressionSyntax condition, StatementSyntax statement);

-        public static WhileStatementSyntax WhileStatement(SyntaxToken whileKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement);

-        public static SyntaxTrivia Whitespace(string text);

-        public static SyntaxTrivia Whitespace(string text, bool elastic);

-        public static XmlCDataSectionSyntax XmlCDataSection(SyntaxToken startCDataToken, SyntaxTokenList textTokens, SyntaxToken endCDataToken);

-        public static XmlCDataSectionSyntax XmlCDataSection(SyntaxTokenList textTokens = default(SyntaxTokenList));

-        public static XmlCommentSyntax XmlComment(SyntaxToken lessThanExclamationMinusMinusToken, SyntaxTokenList textTokens, SyntaxToken minusMinusGreaterThanToken);

-        public static XmlCommentSyntax XmlComment(SyntaxTokenList textTokens = default(SyntaxTokenList));

-        public static XmlCrefAttributeSyntax XmlCrefAttribute(CrefSyntax cref);

-        public static XmlCrefAttributeSyntax XmlCrefAttribute(CrefSyntax cref, SyntaxKind quoteKind);

-        public static XmlCrefAttributeSyntax XmlCrefAttribute(XmlNameSyntax name, SyntaxToken startQuoteToken, CrefSyntax cref, SyntaxToken endQuoteToken);

-        public static XmlCrefAttributeSyntax XmlCrefAttribute(XmlNameSyntax name, SyntaxToken equalsToken, SyntaxToken startQuoteToken, CrefSyntax cref, SyntaxToken endQuoteToken);

-        public static XmlElementSyntax XmlElement(XmlElementStartTagSyntax startTag, XmlElementEndTagSyntax endTag);

-        public static XmlElementSyntax XmlElement(XmlElementStartTagSyntax startTag, SyntaxList<XmlNodeSyntax> content, XmlElementEndTagSyntax endTag);

-        public static XmlElementSyntax XmlElement(XmlNameSyntax name, SyntaxList<XmlNodeSyntax> content);

-        public static XmlElementSyntax XmlElement(string localName, SyntaxList<XmlNodeSyntax> content);

-        public static XmlElementEndTagSyntax XmlElementEndTag(XmlNameSyntax name);

-        public static XmlElementEndTagSyntax XmlElementEndTag(SyntaxToken lessThanSlashToken, XmlNameSyntax name, SyntaxToken greaterThanToken);

-        public static XmlElementStartTagSyntax XmlElementStartTag(XmlNameSyntax name);

-        public static XmlElementStartTagSyntax XmlElementStartTag(XmlNameSyntax name, SyntaxList<XmlAttributeSyntax> attributes);

-        public static XmlElementStartTagSyntax XmlElementStartTag(SyntaxToken lessThanToken, XmlNameSyntax name, SyntaxList<XmlAttributeSyntax> attributes, SyntaxToken greaterThanToken);

-        public static XmlEmptyElementSyntax XmlEmptyElement(XmlNameSyntax name);

-        public static XmlEmptyElementSyntax XmlEmptyElement(XmlNameSyntax name, SyntaxList<XmlAttributeSyntax> attributes);

-        public static XmlEmptyElementSyntax XmlEmptyElement(SyntaxToken lessThanToken, XmlNameSyntax name, SyntaxList<XmlAttributeSyntax> attributes, SyntaxToken slashGreaterThanToken);

-        public static XmlEmptyElementSyntax XmlEmptyElement(string localName);

-        public static SyntaxToken XmlEntity(SyntaxTriviaList leading, string text, string value, SyntaxTriviaList trailing);

-        public static XmlElementSyntax XmlExampleElement(params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlExampleElement(SyntaxList<XmlNodeSyntax> content);

-        public static XmlElementSyntax XmlExceptionElement(CrefSyntax cref, params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlExceptionElement(CrefSyntax cref, SyntaxList<XmlNodeSyntax> content);

-        public static XmlElementSyntax XmlMultiLineElement(XmlNameSyntax name, SyntaxList<XmlNodeSyntax> content);

-        public static XmlElementSyntax XmlMultiLineElement(string localName, SyntaxList<XmlNodeSyntax> content);

-        public static XmlNameSyntax XmlName(XmlPrefixSyntax prefix, SyntaxToken localName);

-        public static XmlNameSyntax XmlName(SyntaxToken localName);

-        public static XmlNameSyntax XmlName(string localName);

-        public static XmlNameAttributeSyntax XmlNameAttribute(XmlNameSyntax name, SyntaxToken startQuoteToken, IdentifierNameSyntax identifier, SyntaxToken endQuoteToken);

-        public static XmlNameAttributeSyntax XmlNameAttribute(XmlNameSyntax name, SyntaxToken equalsToken, SyntaxToken startQuoteToken, IdentifierNameSyntax identifier, SyntaxToken endQuoteToken);

-        public static XmlNameAttributeSyntax XmlNameAttribute(XmlNameSyntax name, SyntaxToken startQuoteToken, string identifier, SyntaxToken endQuoteToken);

-        public static XmlNameAttributeSyntax XmlNameAttribute(string parameterName);

-        public static XmlTextSyntax XmlNewLine(string text);

-        public static XmlEmptyElementSyntax XmlNullKeywordElement();

-        public static XmlElementSyntax XmlParaElement(params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlParaElement(SyntaxList<XmlNodeSyntax> content);

-        public static XmlElementSyntax XmlParamElement(string parameterName, params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlParamElement(string parameterName, SyntaxList<XmlNodeSyntax> content);

-        public static XmlEmptyElementSyntax XmlParamRefElement(string parameterName);

-        public static XmlElementSyntax XmlPermissionElement(CrefSyntax cref, params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlPermissionElement(CrefSyntax cref, SyntaxList<XmlNodeSyntax> content);

-        public static XmlElementSyntax XmlPlaceholderElement(params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlPlaceholderElement(SyntaxList<XmlNodeSyntax> content);

-        public static XmlPrefixSyntax XmlPrefix(SyntaxToken prefix);

-        public static XmlPrefixSyntax XmlPrefix(SyntaxToken prefix, SyntaxToken colonToken);

-        public static XmlPrefixSyntax XmlPrefix(string prefix);

-        public static XmlEmptyElementSyntax XmlPreliminaryElement();

-        public static XmlProcessingInstructionSyntax XmlProcessingInstruction(XmlNameSyntax name);

-        public static XmlProcessingInstructionSyntax XmlProcessingInstruction(XmlNameSyntax name, SyntaxTokenList textTokens);

-        public static XmlProcessingInstructionSyntax XmlProcessingInstruction(SyntaxToken startProcessingInstructionToken, XmlNameSyntax name, SyntaxTokenList textTokens, SyntaxToken endProcessingInstructionToken);

-        public static XmlElementSyntax XmlRemarksElement(params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlRemarksElement(SyntaxList<XmlNodeSyntax> content);

-        public static XmlElementSyntax XmlReturnsElement(params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlReturnsElement(SyntaxList<XmlNodeSyntax> content);

-        public static XmlEmptyElementSyntax XmlSeeAlsoElement(CrefSyntax cref);

-        public static XmlElementSyntax XmlSeeAlsoElement(Uri linkAddress, SyntaxList<XmlNodeSyntax> linkText);

-        public static XmlEmptyElementSyntax XmlSeeElement(CrefSyntax cref);

-        public static XmlElementSyntax XmlSummaryElement(params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlSummaryElement(SyntaxList<XmlNodeSyntax> content);

-        public static XmlTextSyntax XmlText();

-        public static XmlTextSyntax XmlText(SyntaxTokenList textTokens);

-        public static XmlTextSyntax XmlText(params SyntaxToken[] textTokens);

-        public static XmlTextSyntax XmlText(string value);

-        public static XmlTextAttributeSyntax XmlTextAttribute(XmlNameSyntax name, SyntaxKind quoteKind, SyntaxTokenList textTokens);

-        public static XmlTextAttributeSyntax XmlTextAttribute(XmlNameSyntax name, SyntaxToken startQuoteToken, SyntaxToken endQuoteToken);

-        public static XmlTextAttributeSyntax XmlTextAttribute(XmlNameSyntax name, SyntaxToken equalsToken, SyntaxToken startQuoteToken, SyntaxTokenList textTokens, SyntaxToken endQuoteToken);

-        public static XmlTextAttributeSyntax XmlTextAttribute(XmlNameSyntax name, SyntaxToken startQuoteToken, SyntaxTokenList textTokens, SyntaxToken endQuoteToken);

-        public static XmlTextAttributeSyntax XmlTextAttribute(string name, SyntaxKind quoteKind, SyntaxTokenList textTokens);

-        public static XmlTextAttributeSyntax XmlTextAttribute(string name, params SyntaxToken[] textTokens);

-        public static XmlTextAttributeSyntax XmlTextAttribute(string name, string value);

-        public static SyntaxToken XmlTextLiteral(SyntaxTriviaList leading, string text, string value, SyntaxTriviaList trailing);

-        public static SyntaxToken XmlTextLiteral(string value);

-        public static SyntaxToken XmlTextLiteral(string text, string value);

-        public static SyntaxToken XmlTextNewLine(SyntaxTriviaList leading, string text, string value, SyntaxTriviaList trailing);

-        public static SyntaxToken XmlTextNewLine(string text);

-        public static SyntaxToken XmlTextNewLine(string text, bool continueXmlDocumentationComment);

-        public static XmlEmptyElementSyntax XmlThreadSafetyElement();

-        public static XmlEmptyElementSyntax XmlThreadSafetyElement(bool isStatic, bool isInstance);

-        public static XmlElementSyntax XmlValueElement(params XmlNodeSyntax[] content);

-        public static XmlElementSyntax XmlValueElement(SyntaxList<XmlNodeSyntax> content);

-        public static YieldStatementSyntax YieldStatement(SyntaxKind kind, ExpressionSyntax expression = null);

-        public static YieldStatementSyntax YieldStatement(SyntaxKind kind, SyntaxToken yieldKeyword, SyntaxToken returnOrBreakKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken);

-    }
-    public static class SyntaxFacts {
 {
-        public static IEqualityComparer<SyntaxKind> EqualityComparer { get; }

-        public static SyntaxKind GetAccessorDeclarationKind(SyntaxKind keyword);

-        public static SyntaxKind GetAssignmentExpression(SyntaxKind token);

-        public static SyntaxKind GetBaseTypeDeclarationKind(SyntaxKind kind);

-        public static SyntaxKind GetBinaryExpression(SyntaxKind token);

-        public static SyntaxKind GetCheckStatement(SyntaxKind keyword);

-        public static SyntaxKind GetContextualKeywordKind(string text);

-        public static IEnumerable<SyntaxKind> GetContextualKeywordKinds();

-        public static SyntaxKind GetInstanceExpression(SyntaxKind token);

-        public static SyntaxKind GetKeywordKind(string text);

-        public static IEnumerable<SyntaxKind> GetKeywordKinds();

-        public static SyntaxKind GetLiteralExpression(SyntaxKind token);

-        public static SyntaxKind GetOperatorKind(string operatorMetadataName);

-        public static SyntaxKind GetPostfixUnaryExpression(SyntaxKind token);

-        public static SyntaxKind GetPrefixUnaryExpression(SyntaxKind token);

-        public static SyntaxKind GetPreprocessorKeywordKind(string text);

-        public static IEnumerable<SyntaxKind> GetPreprocessorKeywordKinds();

-        public static SyntaxKind GetPrimaryFunction(SyntaxKind keyword);

-        public static IEnumerable<SyntaxKind> GetPunctuationKinds();

-        public static IEnumerable<SyntaxKind> GetReservedKeywordKinds();

-        public static SyntaxKind GetSwitchLabelKind(SyntaxKind keyword);

-        public static string GetText(Accessibility accessibility);

-        public static string GetText(SyntaxKind kind);

-        public static SyntaxKind GetTypeDeclarationKind(SyntaxKind kind);

-        public static bool IsAccessibilityModifier(SyntaxKind kind);

-        public static bool IsAccessorDeclaration(SyntaxKind kind);

-        public static bool IsAccessorDeclarationKeyword(SyntaxKind keyword);

-        public static bool IsAliasQualifier(SyntaxNode node);

-        public static bool IsAnyOverloadableOperator(SyntaxKind kind);

-        public static bool IsAnyToken(SyntaxKind kind);

-        public static bool IsAnyUnaryExpression(SyntaxKind token);

-        public static bool IsAssignmentExpression(SyntaxKind kind);

-        public static bool IsAssignmentExpressionOperatorToken(SyntaxKind token);

-        public static bool IsAttributeName(SyntaxNode node);

-        public static bool IsAttributeTargetSpecifier(SyntaxKind kind);

-        public static bool IsBinaryExpression(SyntaxKind token);

-        public static bool IsBinaryExpressionOperatorToken(SyntaxKind token);

-        public static bool IsContextualKeyword(SyntaxKind kind);

-        public static bool IsDocumentationCommentTrivia(SyntaxKind kind);

-        public static bool IsFixedStatementExpression(SyntaxNode node);

-        public static bool IsGlobalMemberDeclaration(SyntaxKind kind);

-        public static bool IsIdentifierPartCharacter(char ch);

-        public static bool IsIdentifierStartCharacter(char ch);

-        public static bool IsIndexed(ExpressionSyntax node);

-        public static bool IsInNamespaceOrTypeContext(ExpressionSyntax node);

-        public static bool IsInstanceExpression(SyntaxKind token);

-        public static bool IsInTypeOnlyContext(ExpressionSyntax node);

-        public static bool IsInvoked(ExpressionSyntax node);

-        public static bool IsKeywordKind(SyntaxKind kind);

-        public static bool IsLambdaBody(SyntaxNode node);

-        public static bool IsLanguagePunctuation(SyntaxKind kind);

-        public static bool IsLiteralExpression(SyntaxKind token);

-        public static bool IsName(SyntaxKind kind);

-        public static bool IsNamedArgumentName(SyntaxNode node);

-        public static bool IsNamespaceAliasQualifier(ExpressionSyntax node);

-        public static bool IsNamespaceMemberDeclaration(SyntaxKind kind);

-        public static bool IsNewLine(char ch);

-        public static bool IsOverloadableBinaryOperator(SyntaxKind kind);

-        public static bool IsOverloadableUnaryOperator(SyntaxKind kind);

-        public static bool IsPostfixUnaryExpression(SyntaxKind token);

-        public static bool IsPostfixUnaryExpressionToken(SyntaxKind token);

-        public static bool IsPredefinedType(SyntaxKind kind);

-        public static bool IsPrefixUnaryExpression(SyntaxKind token);

-        public static bool IsPrefixUnaryExpressionOperatorToken(SyntaxKind token);

-        public static bool IsPreprocessorDirective(SyntaxKind kind);

-        public static bool IsPreprocessorKeyword(SyntaxKind kind);

-        public static bool IsPreprocessorPunctuation(SyntaxKind kind);

-        public static bool IsPrimaryFunction(SyntaxKind keyword);

-        public static bool IsPunctuation(SyntaxKind kind);

-        public static bool IsPunctuationOrKeyword(SyntaxKind kind);

-        public static bool IsQueryContextualKeyword(SyntaxKind kind);

-        public static bool IsReservedKeyword(SyntaxKind kind);

-        public static bool IsReservedTupleElementName(string elementName);

-        public static bool IsTrivia(SyntaxKind kind);

-        public static bool IsTypeDeclaration(SyntaxKind kind);

-        public static bool IsTypeParameterVarianceKeyword(SyntaxKind kind);

-        public static bool IsTypeSyntax(SyntaxKind kind);

-        public static bool IsUnaryOperatorDeclarationToken(SyntaxKind token);

-        public static bool IsValidIdentifier(string name);

-        public static bool IsWhitespace(char ch);

-        public static string TryGetInferredMemberName(this SyntaxNode syntax);

-    }
-    public enum SyntaxKind : ushort {
 {
-        AbstractKeyword = (ushort)8356,

-        AccessorList = (ushort)8895,

-        AddAccessorDeclaration = (ushort)8898,

-        AddAssignmentExpression = (ushort)8715,

-        AddExpression = (ushort)8668,

-        AddKeyword = (ushort)8419,

-        AddressOfExpression = (ushort)8737,

-        AliasKeyword = (ushort)8407,

-        AliasQualifiedName = (ushort)8620,

-        AmpersandAmpersandToken = (ushort)8261,

-        AmpersandEqualsToken = (ushort)8279,

-        AmpersandToken = (ushort)8198,

-        AndAssignmentExpression = (ushort)8720,

-        AnonymousMethodExpression = (ushort)8641,

-        AnonymousObjectCreationExpression = (ushort)8650,

-        AnonymousObjectMemberDeclarator = (ushort)8647,

-        ArgListExpression = (ushort)8748,

-        ArgListKeyword = (ushort)8366,

-        Argument = (ushort)8638,

-        ArgumentList = (ushort)8636,

-        ArrayCreationExpression = (ushort)8651,

-        ArrayInitializerExpression = (ushort)8646,

-        ArrayRankSpecifier = (ushort)8623,

-        ArrayType = (ushort)8622,

-        ArrowExpressionClause = (ushort)8917,

-        AscendingKeyword = (ushort)8432,

-        AscendingOrdering = (ushort)8782,

-        AsExpression = (ushort)8687,

-        AsKeyword = (ushort)8364,

-        AssemblyKeyword = (ushort)8409,

-        AsteriskEqualsToken = (ushort)8277,

-        AsteriskToken = (ushort)8199,

-        AsyncKeyword = (ushort)8435,

-        Attribute = (ushort)8849,

-        AttributeArgument = (ushort)8851,

-        AttributeArgumentList = (ushort)8850,

-        AttributeList = (ushort)8847,

-        AttributeTargetSpecifier = (ushort)8848,

-        AwaitExpression = (ushort)8740,

-        AwaitKeyword = (ushort)8436,

-        BackslashToken = (ushort)8210,

-        BadDirectiveTrivia = (ushort)8562,

-        BadToken = (ushort)8507,

-        BarBarToken = (ushort)8260,

-        BarEqualsToken = (ushort)8278,

-        BarToken = (ushort)8209,

-        BaseConstructorInitializer = (ushort)8889,

-        BaseExpression = (ushort)8747,

-        BaseKeyword = (ushort)8371,

-        BaseList = (ushort)8864,

-        BitwiseAndExpression = (ushort)8678,

-        BitwiseNotExpression = (ushort)8732,

-        BitwiseOrExpression = (ushort)8677,

-        Block = (ushort)8792,

-        BoolKeyword = (ushort)8304,

-        BracketedArgumentList = (ushort)8637,

-        BracketedParameterList = (ushort)8907,

-        BreakKeyword = (ushort)8339,

-        BreakStatement = (ushort)8803,

-        ByKeyword = (ushort)8427,

-        ByteKeyword = (ushort)8305,

-        CaretEqualsToken = (ushort)8282,

-        CaretToken = (ushort)8197,

-        CaseKeyword = (ushort)8332,

-        CasePatternSwitchLabel = (ushort)9009,

-        CaseSwitchLabel = (ushort)8823,

-        CastExpression = (ushort)8640,

-        CatchClause = (ushort)8826,

-        CatchDeclaration = (ushort)8827,

-        CatchFilterClause = (ushort)8828,

-        CatchKeyword = (ushort)8335,

-        CharacterLiteralExpression = (ushort)8751,

-        CharacterLiteralToken = (ushort)8510,

-        CharKeyword = (ushort)8317,

-        CheckedExpression = (ushort)8762,

-        CheckedKeyword = (ushort)8379,

-        CheckedStatement = (ushort)8815,

-        ChecksumKeyword = (ushort)8478,

-        ClassConstraint = (ushort)8868,

-        ClassDeclaration = (ushort)8855,

-        ClassKeyword = (ushort)8374,

-        CloseBraceToken = (ushort)8206,

-        CloseBracketToken = (ushort)8208,

-        CloseParenToken = (ushort)8201,

-        CoalesceExpression = (ushort)8688,

-        CollectionInitializerExpression = (ushort)8645,

-        ColonColonToken = (ushort)8264,

-        ColonToken = (ushort)8211,

-        CommaToken = (ushort)8216,

-        CompilationUnit = (ushort)8840,

-        ComplexElementInitializerExpression = (ushort)8648,

-        ConditionalAccessExpression = (ushort)8691,

-        ConditionalExpression = (ushort)8633,

-        ConflictMarkerTrivia = (ushort)8564,

-        ConstantPattern = (ushort)9002,

-        ConstKeyword = (ushort)8350,

-        ConstructorConstraint = (ushort)8867,

-        ConstructorDeclaration = (ushort)8878,

-        ContinueKeyword = (ushort)8340,

-        ContinueStatement = (ushort)8804,

-        ConversionOperatorDeclaration = (ushort)8877,

-        ConversionOperatorMemberCref = (ushort)8602,

-        CrefBracketedParameterList = (ushort)8604,

-        CrefParameter = (ushort)8605,

-        CrefParameterList = (ushort)8603,

-        DecimalKeyword = (ushort)8315,

-        DeclarationExpression = (ushort)9040,

-        DeclarationPattern = (ushort)9000,

-        DefaultExpression = (ushort)8764,

-        DefaultKeyword = (ushort)8333,

-        DefaultLiteralExpression = (ushort)8755,

-        DefaultSwitchLabel = (ushort)8824,

-        DefineDirectiveTrivia = (ushort)8554,

-        DefineKeyword = (ushort)8471,

-        DelegateDeclaration = (ushort)8859,

-        DelegateKeyword = (ushort)8378,

-        DescendingKeyword = (ushort)8433,

-        DescendingOrdering = (ushort)8783,

-        DestructorDeclaration = (ushort)8891,

-        DisabledTextTrivia = (ushort)8546,

-        DisableKeyword = (ushort)8479,

-        DiscardDesignation = (ushort)9014,

-        DivideAssignmentExpression = (ushort)8718,

-        DivideExpression = (ushort)8671,

-        DocumentationCommentExteriorTrivia = (ushort)8543,

-        DoKeyword = (ushort)8330,

-        DollarToken = (ushort)8195,

-        DoStatement = (ushort)8810,

-        DotToken = (ushort)8218,

-        DoubleKeyword = (ushort)8313,

-        DoubleQuoteToken = (ushort)8213,

-        ElementAccessExpression = (ushort)8635,

-        ElementBindingExpression = (ushort)8708,

-        ElifDirectiveTrivia = (ushort)8549,

-        ElifKeyword = (ushort)8467,

-        ElseClause = (ushort)8820,

-        ElseDirectiveTrivia = (ushort)8550,

-        ElseKeyword = (ushort)8326,

-        EmptyStatement = (ushort)8798,

-        EndIfDirectiveTrivia = (ushort)8551,

-        EndIfKeyword = (ushort)8468,

-        EndOfDirectiveToken = (ushort)8494,

-        EndOfDocumentationCommentToken = (ushort)8495,

-        EndOfFileToken = (ushort)8496,

-        EndOfLineTrivia = (ushort)8539,

-        EndRegionDirectiveTrivia = (ushort)8553,

-        EndRegionKeyword = (ushort)8470,

-        EnumDeclaration = (ushort)8858,

-        EnumKeyword = (ushort)8377,

-        EnumMemberDeclaration = (ushort)8872,

-        EqualsEqualsToken = (ushort)8268,

-        EqualsExpression = (ushort)8680,

-        EqualsGreaterThanToken = (ushort)8269,

-        EqualsKeyword = (ushort)8431,

-        EqualsToken = (ushort)8204,

-        EqualsValueClause = (ushort)8796,

-        ErrorDirectiveTrivia = (ushort)8556,

-        ErrorKeyword = (ushort)8474,

-        EventDeclaration = (ushort)8893,

-        EventFieldDeclaration = (ushort)8874,

-        EventKeyword = (ushort)8358,

-        ExclamationEqualsToken = (ushort)8267,

-        ExclamationToken = (ushort)8194,

-        ExclusiveOrAssignmentExpression = (ushort)8721,

-        ExclusiveOrExpression = (ushort)8679,

-        ExplicitInterfaceSpecifier = (ushort)8871,

-        ExplicitKeyword = (ushort)8383,

-        ExpressionStatement = (ushort)8797,

-        ExternAliasDirective = (ushort)8844,

-        ExternKeyword = (ushort)8359,

-        FalseKeyword = (ushort)8324,

-        FalseLiteralExpression = (ushort)8753,

-        FieldDeclaration = (ushort)8873,

-        FieldKeyword = (ushort)8412,

-        FinallyClause = (ushort)8829,

-        FinallyKeyword = (ushort)8336,

-        FixedKeyword = (ushort)8351,

-        FixedStatement = (ushort)8814,

-        FloatKeyword = (ushort)8314,

-        ForEachKeyword = (ushort)8329,

-        ForEachStatement = (ushort)8812,

-        ForEachVariableStatement = (ushort)8929,

-        ForKeyword = (ushort)8328,

-        ForStatement = (ushort)8811,

-        FromClause = (ushort)8776,

-        FromKeyword = (ushort)8422,

-        GenericName = (ushort)8618,

-        GetAccessorDeclaration = (ushort)8896,

-        GetKeyword = (ushort)8417,

-        GlobalKeyword = (ushort)8408,

-        GlobalStatement = (ushort)8841,

-        GotoCaseStatement = (ushort)8801,

-        GotoDefaultStatement = (ushort)8802,

-        GotoKeyword = (ushort)8338,

-        GotoStatement = (ushort)8800,

-        GreaterThanEqualsToken = (ushort)8273,

-        GreaterThanExpression = (ushort)8684,

-        GreaterThanGreaterThanEqualsToken = (ushort)8275,

-        GreaterThanGreaterThanToken = (ushort)8274,

-        GreaterThanOrEqualExpression = (ushort)8685,

-        GreaterThanToken = (ushort)8217,

-        GroupClause = (ushort)8785,

-        GroupKeyword = (ushort)8423,

-        HashToken = (ushort)8220,

-        HiddenKeyword = (ushort)8477,

-        IdentifierName = (ushort)8616,

-        IdentifierToken = (ushort)8508,

-        IfDirectiveTrivia = (ushort)8548,

-        IfKeyword = (ushort)8325,

-        IfStatement = (ushort)8819,

-        ImplicitArrayCreationExpression = (ushort)8652,

-        ImplicitElementAccess = (ushort)8656,

-        ImplicitKeyword = (ushort)8384,

-        ImplicitStackAllocArrayCreationExpression = (ushort)9053,

-        IncompleteMember = (ushort)8916,

-        IndexerDeclaration = (ushort)8894,

-        IndexerMemberCref = (ushort)8600,

-        InKeyword = (ushort)8362,

-        InterfaceDeclaration = (ushort)8857,

-        InterfaceKeyword = (ushort)8376,

-        InternalKeyword = (ushort)8345,

-        InterpolatedStringEndToken = (ushort)8483,

-        InterpolatedStringExpression = (ushort)8655,

-        InterpolatedStringStartToken = (ushort)8482,

-        InterpolatedStringText = (ushort)8919,

-        InterpolatedStringTextToken = (ushort)8517,

-        InterpolatedStringToken = (ushort)8515,

-        InterpolatedVerbatimStringStartToken = (ushort)8484,

-        Interpolation = (ushort)8918,

-        InterpolationAlignmentClause = (ushort)8920,

-        InterpolationFormatClause = (ushort)8921,

-        IntKeyword = (ushort)8309,

-        IntoKeyword = (ushort)8425,

-        InvocationExpression = (ushort)8634,

-        IsExpression = (ushort)8686,

-        IsKeyword = (ushort)8363,

-        IsPatternExpression = (ushort)8657,

-        JoinClause = (ushort)8778,

-        JoinIntoClause = (ushort)8779,

-        JoinKeyword = (ushort)8424,

-        LabeledStatement = (ushort)8799,

-        LeftShiftAssignmentExpression = (ushort)8723,

-        LeftShiftExpression = (ushort)8673,

-        LessThanEqualsToken = (ushort)8270,

-        LessThanExpression = (ushort)8682,

-        LessThanLessThanEqualsToken = (ushort)8272,

-        LessThanLessThanToken = (ushort)8271,

-        LessThanOrEqualExpression = (ushort)8683,

-        LessThanSlashToken = (ushort)8233,

-        LessThanToken = (ushort)8215,

-        LetClause = (ushort)8777,

-        LetKeyword = (ushort)8426,

-        LineDirectiveTrivia = (ushort)8558,

-        LineKeyword = (ushort)8475,

-        List = (ushort)1,

-        LoadDirectiveTrivia = (ushort)8923,

-        LoadKeyword = (ushort)8485,

-        LocalDeclarationStatement = (ushort)8793,

-        LocalFunctionStatement = (ushort)8830,

-        LockKeyword = (ushort)8337,

-        LockStatement = (ushort)8818,

-        LogicalAndExpression = (ushort)8676,

-        LogicalNotExpression = (ushort)8733,

-        LogicalOrExpression = (ushort)8675,

-        LongKeyword = (ushort)8311,

-        MakeRefExpression = (ushort)8765,

-        MakeRefKeyword = (ushort)8367,

-        MemberBindingExpression = (ushort)8707,

-        MethodDeclaration = (ushort)8875,

-        MethodKeyword = (ushort)8413,

-        MinusEqualsToken = (ushort)8281,

-        MinusGreaterThanToken = (ushort)8266,

-        MinusMinusToken = (ushort)8262,

-        MinusToken = (ushort)8202,

-        ModuleKeyword = (ushort)8410,

-        ModuloAssignmentExpression = (ushort)8719,

-        ModuloExpression = (ushort)8672,

-        MultiLineCommentTrivia = (ushort)8542,

-        MultiLineDocumentationCommentTrivia = (ushort)8545,

-        MultiplyAssignmentExpression = (ushort)8717,

-        MultiplyExpression = (ushort)8670,

-        NameColon = (ushort)8639,

-        NameEquals = (ushort)8852,

-        NameMemberCref = (ushort)8599,

-        NameOfKeyword = (ushort)8434,

-        NamespaceDeclaration = (ushort)8842,

-        NamespaceKeyword = (ushort)8372,

-        NewKeyword = (ushort)8354,

-        None = (ushort)0,

-        NotEqualsExpression = (ushort)8681,

-        NullableType = (ushort)8625,

-        NullKeyword = (ushort)8322,

-        NullLiteralExpression = (ushort)8754,

-        NumericLiteralExpression = (ushort)8749,

-        NumericLiteralToken = (ushort)8509,

-        ObjectCreationExpression = (ushort)8649,

-        ObjectInitializerExpression = (ushort)8644,

-        ObjectKeyword = (ushort)8319,

-        OmittedArraySizeExpression = (ushort)8654,

-        OmittedArraySizeExpressionToken = (ushort)8493,

-        OmittedTypeArgument = (ushort)8626,

-        OmittedTypeArgumentToken = (ushort)8492,

-        OnKeyword = (ushort)8430,

-        OpenBraceToken = (ushort)8205,

-        OpenBracketToken = (ushort)8207,

-        OpenParenToken = (ushort)8200,

-        OperatorDeclaration = (ushort)8876,

-        OperatorKeyword = (ushort)8382,

-        OperatorMemberCref = (ushort)8601,

-        OrAssignmentExpression = (ushort)8722,

-        OrderByClause = (ushort)8781,

-        OrderByKeyword = (ushort)8429,

-        OutKeyword = (ushort)8361,

-        OverrideKeyword = (ushort)8355,

-        Parameter = (ushort)8908,

-        ParameterList = (ushort)8906,

-        ParamKeyword = (ushort)8414,

-        ParamsKeyword = (ushort)8365,

-        ParenthesizedExpression = (ushort)8632,

-        ParenthesizedLambdaExpression = (ushort)8643,

-        ParenthesizedVariableDesignation = (ushort)8928,

-        PartialKeyword = (ushort)8406,

-        PercentEqualsToken = (ushort)8283,

-        PercentToken = (ushort)8196,

-        PlusEqualsToken = (ushort)8280,

-        PlusPlusToken = (ushort)8263,

-        PlusToken = (ushort)8203,

-        PointerIndirectionExpression = (ushort)8736,

-        PointerMemberAccessExpression = (ushort)8690,

-        PointerType = (ushort)8624,

-        PostDecrementExpression = (ushort)8739,

-        PostIncrementExpression = (ushort)8738,

-        PragmaChecksumDirectiveTrivia = (ushort)8560,

-        PragmaKeyword = (ushort)8476,

-        PragmaWarningDirectiveTrivia = (ushort)8559,

-        PreDecrementExpression = (ushort)8735,

-        PredefinedType = (ushort)8621,

-        PreIncrementExpression = (ushort)8734,

-        PreprocessingMessageTrivia = (ushort)8547,

-        PrivateKeyword = (ushort)8344,

-        PropertyDeclaration = (ushort)8892,

-        PropertyKeyword = (ushort)8415,

-        ProtectedKeyword = (ushort)8346,

-        PublicKeyword = (ushort)8343,

-        QualifiedCref = (ushort)8598,

-        QualifiedName = (ushort)8617,

-        QueryBody = (ushort)8775,

-        QueryContinuation = (ushort)8786,

-        QueryExpression = (ushort)8774,

-        QuestionQuestionToken = (ushort)8265,

-        QuestionToken = (ushort)8219,

-        ReadOnlyKeyword = (ushort)8348,

-        ReferenceDirectiveTrivia = (ushort)8561,

-        ReferenceKeyword = (ushort)8481,

-        RefExpression = (ushort)9050,

-        RefKeyword = (ushort)8360,

-        RefType = (ushort)9051,

-        RefTypeExpression = (ushort)8767,

-        RefTypeKeyword = (ushort)8368,

-        RefValueExpression = (ushort)8766,

-        RefValueKeyword = (ushort)8369,

-        RegionDirectiveTrivia = (ushort)8552,

-        RegionKeyword = (ushort)8469,

-        RemoveAccessorDeclaration = (ushort)8899,

-        RemoveKeyword = (ushort)8420,

-        RestoreKeyword = (ushort)8480,

-        ReturnKeyword = (ushort)8341,

-        ReturnStatement = (ushort)8805,

-        RightShiftAssignmentExpression = (ushort)8724,

-        RightShiftExpression = (ushort)8674,

-        SByteKeyword = (ushort)8306,

-        SealedKeyword = (ushort)8349,

-        SelectClause = (ushort)8784,

-        SelectKeyword = (ushort)8428,

-        SemicolonToken = (ushort)8212,

-        SetAccessorDeclaration = (ushort)8897,

-        SetKeyword = (ushort)8418,

-        ShebangDirectiveTrivia = (ushort)8922,

-        ShortKeyword = (ushort)8307,

-        SimpleAssignmentExpression = (ushort)8714,

-        SimpleBaseType = (ushort)8865,

-        SimpleLambdaExpression = (ushort)8642,

-        SimpleMemberAccessExpression = (ushort)8689,

-        SingleLineCommentTrivia = (ushort)8541,

-        SingleLineDocumentationCommentTrivia = (ushort)8544,

-        SingleQuoteToken = (ushort)8214,

-        SingleVariableDesignation = (ushort)8927,

-        SizeOfExpression = (ushort)8761,

-        SizeOfKeyword = (ushort)8321,

-        SkippedTokensTrivia = (ushort)8563,

-        SlashEqualsToken = (ushort)8276,

-        SlashGreaterThanToken = (ushort)8232,

-        SlashToken = (ushort)8221,

-        StackAllocArrayCreationExpression = (ushort)8653,

-        StackAllocKeyword = (ushort)8352,

-        StaticKeyword = (ushort)8347,

-        StringKeyword = (ushort)8316,

-        StringLiteralExpression = (ushort)8750,

-        StringLiteralToken = (ushort)8511,

-        StructConstraint = (ushort)8869,

-        StructDeclaration = (ushort)8856,

-        StructKeyword = (ushort)8375,

-        SubtractAssignmentExpression = (ushort)8716,

-        SubtractExpression = (ushort)8669,

-        SwitchKeyword = (ushort)8331,

-        SwitchSection = (ushort)8822,

-        SwitchStatement = (ushort)8821,

-        ThisConstructorInitializer = (ushort)8890,

-        ThisExpression = (ushort)8746,

-        ThisKeyword = (ushort)8370,

-        ThrowExpression = (ushort)9052,

-        ThrowKeyword = (ushort)8342,

-        ThrowStatement = (ushort)8808,

-        TildeToken = (ushort)8193,

-        TrueKeyword = (ushort)8323,

-        TrueLiteralExpression = (ushort)8752,

-        TryKeyword = (ushort)8334,

-        TryStatement = (ushort)8825,

-        TupleElement = (ushort)8925,

-        TupleExpression = (ushort)8926,

-        TupleType = (ushort)8924,

-        TypeArgumentList = (ushort)8619,

-        TypeConstraint = (ushort)8870,

-        TypeCref = (ushort)8597,

-        TypeKeyword = (ushort)8411,

-        TypeOfExpression = (ushort)8760,

-        TypeOfKeyword = (ushort)8320,

-        TypeParameter = (ushort)8910,

-        TypeParameterConstraintClause = (ushort)8866,

-        TypeParameterList = (ushort)8909,

-        TypeVarKeyword = (ushort)8416,

-        UIntKeyword = (ushort)8310,

-        ULongKeyword = (ushort)8312,

-        UnaryMinusExpression = (ushort)8731,

-        UnaryPlusExpression = (ushort)8730,

-        UncheckedExpression = (ushort)8763,

-        UncheckedKeyword = (ushort)8380,

-        UncheckedStatement = (ushort)8816,

-        UndefDirectiveTrivia = (ushort)8555,

-        UndefKeyword = (ushort)8472,

-        UnderscoreToken = (ushort)8491,

-        UnknownAccessorDeclaration = (ushort)8900,

-        UnsafeKeyword = (ushort)8381,

-        UnsafeStatement = (ushort)8817,

-        UShortKeyword = (ushort)8308,

-        UsingDirective = (ushort)8843,

-        UsingKeyword = (ushort)8373,

-        UsingStatement = (ushort)8813,

-        VariableDeclaration = (ushort)8794,

-        VariableDeclarator = (ushort)8795,

-        VirtualKeyword = (ushort)8357,

-        VoidKeyword = (ushort)8318,

-        VolatileKeyword = (ushort)8353,

-        WarningDirectiveTrivia = (ushort)8557,

-        WarningKeyword = (ushort)8473,

-        WhenClause = (ushort)9013,

-        WhenKeyword = (ushort)8437,

-        WhereClause = (ushort)8780,

-        WhereKeyword = (ushort)8421,

-        WhileKeyword = (ushort)8327,

-        WhileStatement = (ushort)8809,

-        WhitespaceTrivia = (ushort)8540,

-        XmlCDataEndToken = (ushort)8237,

-        XmlCDataSection = (ushort)8584,

-        XmlCDataStartToken = (ushort)8236,

-        XmlComment = (ushort)8585,

-        XmlCommentEndToken = (ushort)8235,

-        XmlCommentStartToken = (ushort)8234,

-        XmlCrefAttribute = (ushort)8579,

-        XmlElement = (ushort)8574,

-        XmlElementEndTag = (ushort)8576,

-        XmlElementStartTag = (ushort)8575,

-        XmlEmptyElement = (ushort)8577,

-        XmlEntityLiteralToken = (ushort)8512,

-        XmlName = (ushort)8581,

-        XmlNameAttribute = (ushort)8580,

-        XmlPrefix = (ushort)8582,

-        XmlProcessingInstruction = (ushort)8586,

-        XmlProcessingInstructionEndToken = (ushort)8239,

-        XmlProcessingInstructionStartToken = (ushort)8238,

-        XmlText = (ushort)8583,

-        XmlTextAttribute = (ushort)8578,

-        XmlTextLiteralNewLineToken = (ushort)8514,

-        XmlTextLiteralToken = (ushort)8513,

-        YieldBreakStatement = (ushort)8807,

-        YieldKeyword = (ushort)8405,

-        YieldReturnStatement = (ushort)8806,

-    }
-    public static class TypedConstantExtensions {
 {
-        public static string ToCSharpString(this TypedConstant constant);

-    }
-}
```

