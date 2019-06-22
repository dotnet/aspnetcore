# Microsoft.CodeAnalysis

``` diff
-namespace Microsoft.CodeAnalysis {
 {
-    public enum Accessibility {
 {
-        Friend = 4,

-        Internal = 4,

-        NotApplicable = 0,

-        Private = 1,

-        Protected = 3,

-        ProtectedAndFriend = 2,

-        ProtectedAndInternal = 2,

-        ProtectedOrFriend = 5,

-        ProtectedOrInternal = 5,

-        Public = 6,

-    }
-    public abstract class AdditionalText {
 {
-        protected AdditionalText();

-        public abstract string Path { get; }

-        public abstract SourceText GetText(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public static class AnnotationExtensions {
 {
-        public static TNode WithAdditionalAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : SyntaxNode;

-        public static TNode WithAdditionalAnnotations<TNode>(this TNode node, IEnumerable<SyntaxAnnotation> annotations) where TNode : SyntaxNode;

-        public static TNode WithoutAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : SyntaxNode;

-        public static TNode WithoutAnnotations<TNode>(this TNode node, IEnumerable<SyntaxAnnotation> annotations) where TNode : SyntaxNode;

-        public static TNode WithoutAnnotations<TNode>(this TNode node, string annotationKind) where TNode : SyntaxNode;

-    }
-    public sealed class AssemblyIdentity : IEquatable<AssemblyIdentity> {
 {
-        public AssemblyIdentity(string name, Version version = null, string cultureName = null, ImmutableArray<byte> publicKeyOrToken = default(ImmutableArray<byte>), bool hasPublicKey = false, bool isRetargetable = false, AssemblyContentType contentType = 0);

-        public AssemblyContentType ContentType { get; }

-        public string CultureName { get; }

-        public AssemblyNameFlags Flags { get; }

-        public bool HasPublicKey { get; }

-        public bool IsRetargetable { get; }

-        public bool IsStrongName { get; }

-        public string Name { get; }

-        public ImmutableArray<byte> PublicKey { get; }

-        public ImmutableArray<byte> PublicKeyToken { get; }

-        public Version Version { get; }

-        public bool Equals(AssemblyIdentity obj);

-        public override bool Equals(object obj);

-        public static AssemblyIdentity FromAssemblyDefinition(Assembly assembly);

-        public string GetDisplayName(bool fullKey = false);

-        public override int GetHashCode();

-        public static bool operator ==(AssemblyIdentity left, AssemblyIdentity right);

-        public static bool operator !=(AssemblyIdentity left, AssemblyIdentity right);

-        public override string ToString();

-        public static bool TryParseDisplayName(string displayName, out AssemblyIdentity identity);

-        public static bool TryParseDisplayName(string displayName, out AssemblyIdentity identity, out AssemblyIdentityParts parts);

-    }
-    public class AssemblyIdentityComparer {
 {
-        public static StringComparer CultureComparer { get; }

-        public static AssemblyIdentityComparer Default { get; }

-        public static StringComparer SimpleNameComparer { get; }

-        public AssemblyIdentityComparer.ComparisonResult Compare(AssemblyIdentity reference, AssemblyIdentity definition);

-        public bool ReferenceMatchesDefinition(AssemblyIdentity reference, AssemblyIdentity definition);

-        public bool ReferenceMatchesDefinition(string referenceDisplayName, AssemblyIdentity definition);

-        public enum ComparisonResult {
 {
-            Equivalent = 1,

-            EquivalentIgnoringVersion = 2,

-            NotEquivalent = 0,

-        }
-    }
-    public enum AssemblyIdentityParts {
 {
-        ContentType = 512,

-        Culture = 32,

-        Name = 1,

-        PublicKey = 64,

-        PublicKeyOrToken = 192,

-        PublicKeyToken = 128,

-        Retargetability = 256,

-        Unknown = 1024,

-        Version = 30,

-        VersionBuild = 8,

-        VersionMajor = 2,

-        VersionMinor = 4,

-        VersionRevision = 16,

-    }
-    public sealed class AssemblyMetadata : Metadata {
 {
-        public override MetadataImageKind Kind { get; }

-        protected override Metadata CommonCopy();

-        public static AssemblyMetadata Create(ModuleMetadata module);

-        public static AssemblyMetadata Create(params ModuleMetadata[] modules);

-        public static AssemblyMetadata Create(IEnumerable<ModuleMetadata> modules);

-        public static AssemblyMetadata Create(ImmutableArray<ModuleMetadata> modules);

-        public static AssemblyMetadata CreateFromFile(string path);

-        public static AssemblyMetadata CreateFromImage(IEnumerable<byte> peImage);

-        public static AssemblyMetadata CreateFromImage(ImmutableArray<byte> peImage);

-        public static AssemblyMetadata CreateFromStream(Stream peStream, bool leaveOpen = false);

-        public static AssemblyMetadata CreateFromStream(Stream peStream, PEStreamOptions options);

-        public override void Dispose();

-        public ImmutableArray<ModuleMetadata> GetModules();

-        public PortableExecutableReference GetReference(DocumentationProvider documentation = null, ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false, string filePath = null, string display = null);

-    }
-    public abstract class AttributeData {
 {
-        protected AttributeData();

-        public SyntaxReference ApplicationSyntaxReference { get; }

-        public INamedTypeSymbol AttributeClass { get; }

-        public IMethodSymbol AttributeConstructor { get; }

-        protected abstract SyntaxReference CommonApplicationSyntaxReference { get; }

-        protected abstract INamedTypeSymbol CommonAttributeClass { get; }

-        protected abstract IMethodSymbol CommonAttributeConstructor { get; }

-        protected internal abstract ImmutableArray<TypedConstant> CommonConstructorArguments { get; }

-        protected internal abstract ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments { get; }

-        public ImmutableArray<TypedConstant> ConstructorArguments { get; }

-        public ImmutableArray<KeyValuePair<string, TypedConstant>> NamedArguments { get; }

-    }
-    public enum CandidateReason {
 {
-        Ambiguous = 15,

-        Inaccessible = 8,

-        LateBound = 14,

-        MemberGroup = 16,

-        None = 0,

-        NotAnAttributeType = 4,

-        NotAnEvent = 2,

-        NotATypeOrNamespace = 1,

-        NotAValue = 9,

-        NotAVariable = 10,

-        NotAWithEventsMember = 3,

-        NotCreatable = 6,

-        NotInvocable = 11,

-        NotReferencable = 7,

-        OverloadResolutionFailure = 13,

-        StaticInstanceMismatch = 12,

-        WrongArity = 5,

-    }
-    public static class CaseInsensitiveComparison {
 {
-        public static StringComparer Comparer { get; }

-        public static int Compare(string left, string right);

-        public static bool EndsWith(string value, string possibleEnd);

-        public static bool Equals(string left, string right);

-        public static int GetHashCode(string value);

-        public static bool StartsWith(string value, string possibleStart);

-        public static char ToLower(char c);

-        public static string ToLower(string value);

-        public static void ToLower(StringBuilder builder);

-    }
-    public struct ChildSyntaxList : IEnumerable, IEnumerable<SyntaxNodeOrToken>, IEquatable<ChildSyntaxList>, IReadOnlyCollection<SyntaxNodeOrToken>, IReadOnlyList<SyntaxNodeOrToken> {
 {
-        public int Count { get; }

-        public SyntaxNodeOrToken this[int index] { get; }

-        public bool Any();

-        public bool Equals(ChildSyntaxList other);

-        public override bool Equals(object obj);

-        public SyntaxNodeOrToken First();

-        public ChildSyntaxList.Enumerator GetEnumerator();

-        public override int GetHashCode();

-        public SyntaxNodeOrToken Last();

-        public static bool operator ==(ChildSyntaxList list1, ChildSyntaxList list2);

-        public static bool operator !=(ChildSyntaxList list1, ChildSyntaxList list2);

-        public ChildSyntaxList.Reversed Reverse();

-        IEnumerator<SyntaxNodeOrToken> System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxNodeOrToken>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public struct Enumerator {
 {
-            public SyntaxNodeOrToken Current { get; }

-            public bool MoveNext();

-            public void Reset();

-        }
-        public struct Reversed : IEnumerable, IEnumerable<SyntaxNodeOrToken>, IEquatable<ChildSyntaxList.Reversed> {
 {
-            public bool Equals(ChildSyntaxList.Reversed other);

-            public override bool Equals(object obj);

-            public ChildSyntaxList.Reversed.Enumerator GetEnumerator();

-            public override int GetHashCode();

-            IEnumerator<SyntaxNodeOrToken> System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxNodeOrToken>.GetEnumerator();

-            IEnumerator System.Collections.IEnumerable.GetEnumerator();

-            public struct Enumerator {
 {
-                public SyntaxNodeOrToken Current { get; }

-                public bool MoveNext();

-                public void Reset();

-            }
-        }
-    }
-    public struct CommandLineAnalyzerReference : IEquatable<CommandLineAnalyzerReference> {
 {
-        public CommandLineAnalyzerReference(string path);

-        public string FilePath { get; }

-        public bool Equals(CommandLineAnalyzerReference other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public abstract class CommandLineArguments {
 {
-        public ImmutableArray<CommandLineSourceFile> AdditionalFiles { get; internal set; }

-        public ImmutableArray<CommandLineAnalyzerReference> AnalyzerReferences { get; internal set; }

-        public string AppConfigPath { get; internal set; }

-        public string BaseDirectory { get; internal set; }

-        public SourceHashAlgorithm ChecksumAlgorithm { get; internal set; }

-        public string CompilationName { get; internal set; }

-        public CompilationOptions CompilationOptions { get; }

-        protected abstract CompilationOptions CompilationOptionsCore { get; }

-        public bool DisplayHelp { get; internal set; }

-        public bool DisplayLangVersions { get; internal set; }

-        public bool DisplayLogo { get; internal set; }

-        public bool DisplayVersion { get; internal set; }

-        public string DocumentationPath { get; internal set; }

-        public ImmutableArray<CommandLineSourceFile> EmbeddedFiles { get; internal set; }

-        public EmitOptions EmitOptions { get; internal set; }

-        public bool EmitPdb { get; internal set; }

-        public Encoding Encoding { get; internal set; }

-        public string ErrorLogPath { get; internal set; }

-        public ImmutableArray<Diagnostic> Errors { get; internal set; }

-        public bool InteractiveMode { get; internal set; }

-        public ImmutableArray<string> KeyFileSearchPaths { get; internal set; }

-        public ImmutableArray<ResourceDescription> ManifestResources { get; internal set; }

-        public ImmutableArray<CommandLineReference> MetadataReferences { get; internal set; }

-        public bool NoWin32Manifest { get; internal set; }

-        public string OutputDirectory { get; internal set; }

-        public string OutputFileName { get; internal set; }

-        public string OutputRefFilePath { get; internal set; }

-        public ParseOptions ParseOptions { get; }

-        protected abstract ParseOptions ParseOptionsCore { get; }

-        public ImmutableArray<KeyValuePair<string, string>> PathMap { get; internal set; }

-        public string PdbPath { get; internal set; }

-        public CultureInfo PreferredUILang { get; internal set; }

-        public bool PrintFullPaths { get; internal set; }

-        public ImmutableArray<string> ReferencePaths { get; internal set; }

-        public bool ReportAnalyzer { get; internal set; }

-        public string RuleSetPath { get; internal set; }

-        public ImmutableArray<string> ScriptArguments { get; internal set; }

-        public ImmutableArray<CommandLineSourceFile> SourceFiles { get; internal set; }

-        public string SourceLink { get; internal set; }

-        public ImmutableArray<string> SourcePaths { get; internal set; }

-        public string TouchedFilesPath { get; internal set; }

-        public bool Utf8Output { get; internal set; }

-        public string Win32Icon { get; internal set; }

-        public string Win32Manifest { get; internal set; }

-        public string Win32ResourceFile { get; internal set; }

-        public IEnumerable<AnalyzerReference> ResolveAnalyzerReferences(IAnalyzerAssemblyLoader analyzerLoader);

-        public IEnumerable<MetadataReference> ResolveMetadataReferences(MetadataReferenceResolver metadataResolver);

-    }
-    public abstract class CommandLineParser {
 {
-        protected abstract string RegularFileExtension { get; }

-        protected abstract string ScriptFileExtension { get; }

-        public CommandLineArguments Parse(IEnumerable<string> args, string baseDirectory, string sdkDirectory, string additionalReferenceDirectories);

-        protected ImmutableArray<KeyValuePair<string, string>> ParsePathMap(string pathMap, IList<Diagnostic> errors);

-        public static IEnumerable<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments);

-    }
-    public struct CommandLineReference : IEquatable<CommandLineReference> {
 {
-        public CommandLineReference(string reference, MetadataReferenceProperties properties);

-        public MetadataReferenceProperties Properties { get; }

-        public string Reference { get; }

-        public bool Equals(CommandLineReference other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public struct CommandLineSourceFile {
 {
-        public CommandLineSourceFile(string path, bool isScript);

-        public bool IsScript { get; }

-        public string Path { get; }

-    }
-    public abstract class Compilation {
 {
-        public IAssemblySymbol Assembly { get; }

-        public string AssemblyName { get; }

-        protected abstract IAssemblySymbol CommonAssembly { get; }

-        protected abstract ITypeSymbol CommonDynamicType { get; }

-        protected abstract INamespaceSymbol CommonGlobalNamespace { get; }

-        protected abstract INamedTypeSymbol CommonObjectType { get; }

-        protected abstract CompilationOptions CommonOptions { get; }

-        protected abstract INamedTypeSymbol CommonScriptClass { get; }

-        protected abstract IModuleSymbol CommonSourceModule { get; }

-        protected abstract IEnumerable<SyntaxTree> CommonSyntaxTrees { get; }

-        public abstract ImmutableArray<MetadataReference> DirectiveReferences { get; }

-        public ITypeSymbol DynamicType { get; }

-        public ImmutableArray<MetadataReference> ExternalReferences { get; }

-        public INamespaceSymbol GlobalNamespace { get; }

-        public abstract bool IsCaseSensitive { get; }

-        public abstract string Language { get; }

-        public INamedTypeSymbol ObjectType { get; }

-        public CompilationOptions Options { get; }

-        public abstract IEnumerable<AssemblyIdentity> ReferencedAssemblyNames { get; }

-        public IEnumerable<MetadataReference> References { get; }

-        public INamedTypeSymbol ScriptClass { get; }

-        public ScriptCompilationInfo ScriptCompilationInfo { get; }

-        public IModuleSymbol SourceModule { get; }

-        public IEnumerable<SyntaxTree> SyntaxTrees { get; }

-        public Compilation AddReferences(params MetadataReference[] references);

-        public Compilation AddReferences(IEnumerable<MetadataReference> references);

-        public Compilation AddSyntaxTrees(params SyntaxTree[] trees);

-        public Compilation AddSyntaxTrees(IEnumerable<SyntaxTree> trees);

-        protected abstract void AppendDefaultVersionResource(Stream resourceStream);

-        protected static void CheckTupleElementLocations(int cardinality, ImmutableArray<Location> elementLocations);

-        protected static ImmutableArray<string> CheckTupleElementNames(int cardinality, ImmutableArray<string> elementNames);

-        public Compilation Clone();

-        protected abstract Compilation CommonAddSyntaxTrees(IEnumerable<SyntaxTree> trees);

-        protected INamedTypeSymbol CommonBindScriptClass();

-        protected abstract Compilation CommonClone();

-        protected abstract bool CommonContainsSyntaxTree(SyntaxTree syntaxTree);

-        protected abstract INamedTypeSymbol CommonCreateAnonymousTypeSymbol(ImmutableArray<ITypeSymbol> memberTypes, ImmutableArray<string> memberNames, ImmutableArray<Location> memberLocations, ImmutableArray<bool> memberIsReadOnly);

-        protected abstract IArrayTypeSymbol CommonCreateArrayTypeSymbol(ITypeSymbol elementType, int rank);

-        protected abstract INamespaceSymbol CommonCreateErrorNamespaceSymbol(INamespaceSymbol container, string name);

-        protected abstract INamedTypeSymbol CommonCreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity);

-        protected abstract IPointerTypeSymbol CommonCreatePointerTypeSymbol(ITypeSymbol elementType);

-        protected abstract INamedTypeSymbol CommonCreateTupleTypeSymbol(INamedTypeSymbol underlyingType, ImmutableArray<string> elementNames, ImmutableArray<Location> elementLocations);

-        protected abstract INamedTypeSymbol CommonCreateTupleTypeSymbol(ImmutableArray<ITypeSymbol> elementTypes, ImmutableArray<string> elementNames, ImmutableArray<Location> elementLocations);

-        protected abstract ISymbol CommonGetAssemblyOrModuleSymbol(MetadataReference reference);

-        protected abstract INamespaceSymbol CommonGetCompilationNamespace(INamespaceSymbol namespaceSymbol);

-        protected abstract IMethodSymbol CommonGetEntryPoint(CancellationToken cancellationToken);

-        protected abstract SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility);

-        protected abstract INamedTypeSymbol CommonGetSpecialType(SpecialType specialType);

-        protected abstract INamedTypeSymbol CommonGetTypeByMetadataName(string metadataName);

-        protected abstract Compilation CommonRemoveAllSyntaxTrees();

-        protected abstract Compilation CommonRemoveSyntaxTrees(IEnumerable<SyntaxTree> trees);

-        protected abstract Compilation CommonReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree);

-        protected abstract Compilation CommonWithAssemblyName(string outputName);

-        protected abstract Compilation CommonWithOptions(CompilationOptions options);

-        protected abstract Compilation CommonWithReferences(IEnumerable<MetadataReference> newReferences);

-        protected abstract Compilation CommonWithScriptCompilationInfo(ScriptCompilationInfo info);

-        public abstract bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken));

-        public bool ContainsSyntaxTree(SyntaxTree syntaxTree);

-        public INamedTypeSymbol CreateAnonymousTypeSymbol(ImmutableArray<ITypeSymbol> memberTypes, ImmutableArray<string> memberNames, ImmutableArray<bool> memberIsReadOnly = default(ImmutableArray<bool>), ImmutableArray<Location> memberLocations = default(ImmutableArray<Location>));

-        public IArrayTypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank = 1);

-        public Stream CreateDefaultWin32Resources(bool versionResource, bool noManifest, Stream manifestContents, Stream iconInIcoFormat);

-        public INamespaceSymbol CreateErrorNamespaceSymbol(INamespaceSymbol container, string name);

-        public INamedTypeSymbol CreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity);

-        public IPointerTypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType);

-        public INamedTypeSymbol CreateTupleTypeSymbol(INamedTypeSymbol underlyingType, ImmutableArray<string> elementNames = default(ImmutableArray<string>), ImmutableArray<Location> elementLocations = default(ImmutableArray<Location>));

-        public INamedTypeSymbol CreateTupleTypeSymbol(ImmutableArray<ITypeSymbol> elementTypes, ImmutableArray<string> elementNames = default(ImmutableArray<string>), ImmutableArray<Location> elementLocations = default(ImmutableArray<Location>));

-        public EmitResult Emit(Stream peStream, Stream pdbStream = null, Stream xmlDocumentationStream = null, Stream win32Resources = null, IEnumerable<ResourceDescription> manifestResources = null, EmitOptions options = null, IMethodSymbol debugEntryPoint = null, Stream sourceLinkStream = null, IEnumerable<EmbeddedText> embeddedTexts = null, Stream metadataPEStream = null, CancellationToken cancellationToken = default(CancellationToken));

-        public EmitResult Emit(Stream peStream, Stream pdbStream, Stream xmlDocumentationStream, Stream win32Resources, IEnumerable<ResourceDescription> manifestResources, EmitOptions options, IMethodSymbol debugEntryPoint, Stream sourceLinkStream, IEnumerable<EmbeddedText> embeddedTexts, CancellationToken cancellationToken);

-        public EmitResult Emit(Stream peStream, Stream pdbStream, Stream xmlDocumentationStream, Stream win32Resources, IEnumerable<ResourceDescription> manifestResources, EmitOptions options, IMethodSymbol debugEntryPoint, CancellationToken cancellationToken);

-        public EmitResult Emit(Stream peStream, Stream pdbStream, Stream xmlDocumentationStream, Stream win32Resources, IEnumerable<ResourceDescription> manifestResources, EmitOptions options, CancellationToken cancellationToken);

-        public EmitDifferenceResult EmitDifference(EmitBaseline baseline, IEnumerable<SemanticEdit> edits, Func<ISymbol, bool> isAddedSymbol, Stream metadataStream, Stream ilStream, Stream pdbStream, ICollection<MethodDefinitionHandle> updatedMethods, CancellationToken cancellationToken = default(CancellationToken));

-        public EmitDifferenceResult EmitDifference(EmitBaseline baseline, IEnumerable<SemanticEdit> edits, Stream metadataStream, Stream ilStream, Stream pdbStream, ICollection<MethodDefinitionHandle> updatedMethods, CancellationToken cancellationToken = default(CancellationToken));

-        public ISymbol GetAssemblyOrModuleSymbol(MetadataReference reference);

-        public INamespaceSymbol GetCompilationNamespace(INamespaceSymbol namespaceSymbol);

-        public abstract ImmutableArray<Diagnostic> GetDeclarationDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public abstract ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public IMethodSymbol GetEntryPoint(CancellationToken cancellationToken);

-        public MetadataReference GetMetadataReference(IAssemblySymbol assemblySymbol);

-        public abstract ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public abstract ImmutableArray<Diagnostic> GetParseDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public static string GetRequiredLanguageVersion(Diagnostic diagnostic);

-        public SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility = false);

-        public INamedTypeSymbol GetSpecialType(SpecialType specialType);

-        public abstract IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken));

-        public INamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName);

-        public ImmutableArray<AssemblyIdentity> GetUnreferencedAssemblyIdentities(Diagnostic diagnostic);

-        public Compilation RemoveAllReferences();

-        public Compilation RemoveAllSyntaxTrees();

-        public Compilation RemoveReferences(params MetadataReference[] references);

-        public Compilation RemoveReferences(IEnumerable<MetadataReference> references);

-        public Compilation RemoveSyntaxTrees(params SyntaxTree[] trees);

-        public Compilation RemoveSyntaxTrees(IEnumerable<SyntaxTree> trees);

-        public Compilation ReplaceReference(MetadataReference oldReference, MetadataReference newReference);

-        public Compilation ReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree);

-        protected static IReadOnlyDictionary<string, string> SyntaxTreeCommonFeatures(IEnumerable<SyntaxTree> trees);

-        public abstract CompilationReference ToMetadataReference(ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false);

-        public Compilation WithAssemblyName(string assemblyName);

-        public Compilation WithOptions(CompilationOptions options);

-        public Compilation WithReferences(params MetadataReference[] newReferences);

-        public Compilation WithReferences(IEnumerable<MetadataReference> newReferences);

-        public Compilation WithScriptCompilationInfo(ScriptCompilationInfo info);

-    }
-    public abstract class CompilationOptions {
 {
-        public AssemblyIdentityComparer AssemblyIdentityComparer { get; protected set; }

-        public bool CheckOverflow { get; protected set; }

-        public bool ConcurrentBuild { get; protected set; }

-        public string CryptoKeyContainer { get; protected set; }

-        public string CryptoKeyFile { get; protected set; }

-        public ImmutableArray<byte> CryptoPublicKey { get; protected set; }

-        public Nullable<bool> DelaySign { get; protected set; }

-        public bool Deterministic { get; protected set; }

-        public ImmutableArray<Diagnostic> Errors { get; }

-        protected internal ImmutableArray<string> Features { get; protected set; }

-        public ReportDiagnostic GeneralDiagnosticOption { get; protected set; }

-        public abstract string Language { get; }

-        public string MainTypeName { get; protected set; }

-        public MetadataImportOptions MetadataImportOptions { get; protected set; }

-        public MetadataReferenceResolver MetadataReferenceResolver { get; protected set; }

-        public string ModuleName { get; protected set; }

-        public OptimizationLevel OptimizationLevel { get; protected set; }

-        public OutputKind OutputKind { get; protected set; }

-        public Platform Platform { get; protected set; }

-        public bool PublicSign { get; protected set; }

-        public bool ReportSuppressedDiagnostics { get; protected set; }

-        public string ScriptClassName { get; protected set; }

-        public SourceReferenceResolver SourceReferenceResolver { get; protected set; }

-        public ImmutableDictionary<string, ReportDiagnostic> SpecificDiagnosticOptions { get; protected set; }

-        public StrongNameProvider StrongNameProvider { get; protected set; }

-        public int WarningLevel { get; protected set; }

-        public XmlReferenceResolver XmlReferenceResolver { get; protected set; }

-        protected abstract CompilationOptions CommonWithAssemblyIdentityComparer(AssemblyIdentityComparer comparer);

-        protected abstract CompilationOptions CommonWithCheckOverflow(bool checkOverflow);

-        protected abstract CompilationOptions CommonWithConcurrentBuild(bool concurrent);

-        protected abstract CompilationOptions CommonWithCryptoKeyContainer(string cryptoKeyContainer);

-        protected abstract CompilationOptions CommonWithCryptoKeyFile(string cryptoKeyFile);

-        protected abstract CompilationOptions CommonWithCryptoPublicKey(ImmutableArray<byte> cryptoPublicKey);

-        protected abstract CompilationOptions CommonWithDelaySign(Nullable<bool> delaySign);

-        protected abstract CompilationOptions CommonWithDeterministic(bool deterministic);

-        protected abstract CompilationOptions CommonWithFeatures(ImmutableArray<string> features);

-        protected abstract CompilationOptions CommonWithGeneralDiagnosticOption(ReportDiagnostic generalDiagnosticOption);

-        protected abstract CompilationOptions CommonWithMainTypeName(string mainTypeName);

-        protected abstract CompilationOptions CommonWithMetadataImportOptions(MetadataImportOptions value);

-        protected abstract CompilationOptions CommonWithMetadataReferenceResolver(MetadataReferenceResolver resolver);

-        protected abstract CompilationOptions CommonWithModuleName(string moduleName);

-        protected abstract CompilationOptions CommonWithOptimizationLevel(OptimizationLevel value);

-        protected abstract CompilationOptions CommonWithOutputKind(OutputKind kind);

-        protected abstract CompilationOptions CommonWithPlatform(Platform platform);

-        protected abstract CompilationOptions CommonWithPublicSign(bool publicSign);

-        protected abstract CompilationOptions CommonWithReportSuppressedDiagnostics(bool reportSuppressedDiagnostics);

-        protected abstract CompilationOptions CommonWithScriptClassName(string scriptClassName);

-        protected abstract CompilationOptions CommonWithSourceReferenceResolver(SourceReferenceResolver resolver);

-        protected abstract CompilationOptions CommonWithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions);

-        protected abstract CompilationOptions CommonWithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> specificDiagnosticOptions);

-        protected abstract CompilationOptions CommonWithStrongNameProvider(StrongNameProvider provider);

-        protected abstract CompilationOptions CommonWithXmlReferenceResolver(XmlReferenceResolver resolver);

-        public abstract override bool Equals(object obj);

-        protected bool EqualsHelper(CompilationOptions other);

-        public abstract override int GetHashCode();

-        protected int GetHashCodeHelper();

-        public static bool operator ==(CompilationOptions left, CompilationOptions right);

-        public static bool operator !=(CompilationOptions left, CompilationOptions right);

-        public CompilationOptions WithAssemblyIdentityComparer(AssemblyIdentityComparer comparer);

-        public CompilationOptions WithConcurrentBuild(bool concurrent);

-        public CompilationOptions WithCryptoKeyContainer(string cryptoKeyContainer);

-        public CompilationOptions WithCryptoKeyFile(string cryptoKeyFile);

-        public CompilationOptions WithCryptoPublicKey(ImmutableArray<byte> cryptoPublicKey);

-        public CompilationOptions WithDelaySign(Nullable<bool> delaySign);

-        public CompilationOptions WithDeterministic(bool deterministic);

-        public CompilationOptions WithGeneralDiagnosticOption(ReportDiagnostic value);

-        public CompilationOptions WithMainTypeName(string mainTypeName);

-        public CompilationOptions WithMetadataImportOptions(MetadataImportOptions value);

-        public CompilationOptions WithMetadataReferenceResolver(MetadataReferenceResolver resolver);

-        public CompilationOptions WithModuleName(string moduleName);

-        public CompilationOptions WithOptimizationLevel(OptimizationLevel value);

-        public CompilationOptions WithOutputKind(OutputKind kind);

-        public CompilationOptions WithOverflowChecks(bool checkOverflow);

-        public CompilationOptions WithPlatform(Platform platform);

-        public CompilationOptions WithPublicSign(bool publicSign);

-        public CompilationOptions WithReportSuppressedDiagnostics(bool value);

-        public CompilationOptions WithScriptClassName(string scriptClassName);

-        public CompilationOptions WithSourceReferenceResolver(SourceReferenceResolver resolver);

-        public CompilationOptions WithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> value);

-        public CompilationOptions WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> value);

-        public CompilationOptions WithStrongNameProvider(StrongNameProvider provider);

-        public CompilationOptions WithXmlReferenceResolver(XmlReferenceResolver resolver);

-    }
-    public abstract class CompilationReference : MetadataReference, IEquatable<CompilationReference> {
 {
-        public Compilation Compilation { get; }

-        public override string Display { get; }

-        public bool Equals(CompilationReference other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public new CompilationReference WithAliases(IEnumerable<string> aliases);

-        public new CompilationReference WithAliases(ImmutableArray<string> aliases);

-        public new CompilationReference WithEmbedInteropTypes(bool value);

-        public new CompilationReference WithProperties(MetadataReferenceProperties properties);

-    }
-    public abstract class ControlFlowAnalysis {
 {
-        protected ControlFlowAnalysis();

-        public abstract bool EndPointIsReachable { get; }

-        public abstract ImmutableArray<SyntaxNode> EntryPoints { get; }

-        public abstract ImmutableArray<SyntaxNode> ExitPoints { get; }

-        public abstract ImmutableArray<SyntaxNode> ReturnStatements { get; }

-        public abstract bool StartPointIsReachable { get; }

-        public abstract bool Succeeded { get; }

-    }
-    public static class CSharpExtensions {
 {
-        public static bool Any(this SyntaxTokenList list, SyntaxKind kind);

-        public static bool Any(this SyntaxTriviaList list, SyntaxKind kind);

-        public static bool Any<TNode>(this SeparatedSyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;

-        public static bool Any<TNode>(this SyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;

-        public static int IndexOf(this SyntaxTokenList list, SyntaxKind kind);

-        public static int IndexOf(this SyntaxTriviaList list, SyntaxKind kind);

-        public static int IndexOf<TNode>(this SeparatedSyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;

-        public static int IndexOf<TNode>(this SyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;

-        public static bool IsKind(this SyntaxNode node, SyntaxKind kind);

-        public static bool IsKind(this SyntaxNodeOrToken nodeOrToken, SyntaxKind kind);

-        public static bool IsKind(this SyntaxToken token, SyntaxKind kind);

-        public static bool IsKind(this SyntaxTrivia trivia, SyntaxKind kind);

-    }
-    public abstract class CustomModifier : ICustomModifier {
 {
-        protected CustomModifier();

-        public abstract bool IsOptional { get; }

-        public abstract INamedTypeSymbol Modifier { get; }

-    }
-    public abstract class DataFlowAnalysis {
 {
-        protected DataFlowAnalysis();

-        public abstract ImmutableArray<ISymbol> AlwaysAssigned { get; }

-        public abstract ImmutableArray<ISymbol> Captured { get; }

-        public abstract ImmutableArray<ISymbol> CapturedInside { get; }

-        public abstract ImmutableArray<ISymbol> CapturedOutside { get; }

-        public abstract ImmutableArray<ISymbol> DataFlowsIn { get; }

-        public abstract ImmutableArray<ISymbol> DataFlowsOut { get; }

-        public abstract ImmutableArray<ISymbol> ReadInside { get; }

-        public abstract ImmutableArray<ISymbol> ReadOutside { get; }

-        public abstract bool Succeeded { get; }

-        public abstract ImmutableArray<ISymbol> UnsafeAddressTaken { get; }

-        public abstract ImmutableArray<ISymbol> VariablesDeclared { get; }

-        public abstract ImmutableArray<ISymbol> WrittenInside { get; }

-        public abstract ImmutableArray<ISymbol> WrittenOutside { get; }

-    }
-    public sealed class DesktopAssemblyIdentityComparer : AssemblyIdentityComparer {
 {
-        public static new DesktopAssemblyIdentityComparer Default { get; }

-        public static DesktopAssemblyIdentityComparer LoadFromXml(Stream input);

-    }
-    public class DesktopStrongNameProvider : StrongNameProvider {
 {
-        public DesktopStrongNameProvider(ImmutableArray<string> keyFileSearchPaths);

-        public DesktopStrongNameProvider(ImmutableArray<string> keyFileSearchPaths = default(ImmutableArray<string>), string tempPath = null);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public abstract class Diagnostic : IEquatable<Diagnostic>, IFormattable {
 {
-        protected Diagnostic();

-        public abstract IReadOnlyList<Location> AdditionalLocations { get; }

-        public virtual DiagnosticSeverity DefaultSeverity { get; }

-        public abstract DiagnosticDescriptor Descriptor { get; }

-        public abstract string Id { get; }

-        public abstract bool IsSuppressed { get; }

-        public bool IsWarningAsError { get; }

-        public abstract Location Location { get; }

-        public virtual ImmutableDictionary<string, string> Properties { get; }

-        public abstract DiagnosticSeverity Severity { get; }

-        public abstract int WarningLevel { get; }

-        public static Diagnostic Create(DiagnosticDescriptor descriptor, Location location, DiagnosticSeverity effectiveSeverity, IEnumerable<Location> additionalLocations, ImmutableDictionary<string, string> properties, params object[] messageArgs);

-        public static Diagnostic Create(DiagnosticDescriptor descriptor, Location location, IEnumerable<Location> additionalLocations, ImmutableDictionary<string, string> properties, params object[] messageArgs);

-        public static Diagnostic Create(DiagnosticDescriptor descriptor, Location location, IEnumerable<Location> additionalLocations, params object[] messageArgs);

-        public static Diagnostic Create(DiagnosticDescriptor descriptor, Location location, ImmutableDictionary<string, string> properties, params object[] messageArgs);

-        public static Diagnostic Create(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs);

-        public static Diagnostic Create(string id, string category, LocalizableString message, DiagnosticSeverity severity, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, int warningLevel, LocalizableString title = null, LocalizableString description = null, string helpLink = null, Location location = null, IEnumerable<Location> additionalLocations = null, IEnumerable<string> customTags = null, ImmutableDictionary<string, string> properties = null);

-        public static Diagnostic Create(string id, string category, LocalizableString message, DiagnosticSeverity severity, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, int warningLevel, bool isSuppressed, LocalizableString title = null, LocalizableString description = null, string helpLink = null, Location location = null, IEnumerable<Location> additionalLocations = null, IEnumerable<string> customTags = null, ImmutableDictionary<string, string> properties = null);

-        public abstract bool Equals(Diagnostic obj);

-        public abstract override bool Equals(object obj);

-        public abstract override int GetHashCode();

-        public abstract string GetMessage(IFormatProvider formatProvider = null);

-        public SuppressionInfo GetSuppressionInfo(Compilation compilation);

-        string System.IFormattable.ToString(string ignored, IFormatProvider formatProvider);

-        public override string ToString();

-    }
-    public sealed class DiagnosticDescriptor : IEquatable<DiagnosticDescriptor> {
 {
-        public DiagnosticDescriptor(string id, LocalizableString title, LocalizableString messageFormat, string category, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, LocalizableString description = null, string helpLinkUri = null, params string[] customTags);

-        public DiagnosticDescriptor(string id, string title, string messageFormat, string category, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, string description = null, string helpLinkUri = null, params string[] customTags);

-        public string Category { get; }

-        public IEnumerable<string> CustomTags { get; }

-        public DiagnosticSeverity DefaultSeverity { get; }

-        public LocalizableString Description { get; }

-        public string HelpLinkUri { get; }

-        public string Id { get; }

-        public bool IsEnabledByDefault { get; }

-        public LocalizableString MessageFormat { get; }

-        public LocalizableString Title { get; }

-        public bool Equals(DiagnosticDescriptor other);

-        public override bool Equals(object obj);

-        public ReportDiagnostic GetEffectiveSeverity(CompilationOptions compilationOptions);

-        public override int GetHashCode();

-    }
-    public class DiagnosticFormatter {
 {
-        public DiagnosticFormatter();

-        public virtual string Format(Diagnostic diagnostic, IFormatProvider formatter = null);

-    }
-    public enum DiagnosticSeverity {
 {
-        Error = 3,

-        Hidden = 0,

-        Info = 1,

-        Warning = 2,

-    }
-    public sealed class DllImportData : IPlatformInvokeInformation {
 {
-        public Nullable<bool> BestFitMapping { get; }

-        public CallingConvention CallingConvention { get; }

-        public CharSet CharacterSet { get; }

-        public string EntryPointName { get; }

-        public bool ExactSpelling { get; }

-        public string ModuleName { get; }

-        public bool SetLastError { get; }

-        public Nullable<bool> ThrowOnUnmappableCharacter { get; }

-    }
-    public static class DocumentationCommentId {
 {
-        public static string CreateDeclarationId(ISymbol symbol);

-        public static string CreateReferenceId(ISymbol symbol);

-        public static ISymbol GetFirstSymbolForDeclarationId(string id, Compilation compilation);

-        public static ISymbol GetFirstSymbolForReferenceId(string id, Compilation compilation);

-        public static ImmutableArray<ISymbol> GetSymbolsForDeclarationId(string id, Compilation compilation);

-        public static ImmutableArray<ISymbol> GetSymbolsForReferenceId(string id, Compilation compilation);

-    }
-    public enum DocumentationMode : byte {
 {
-        Diagnose = (byte)2,

-        None = (byte)0,

-        Parse = (byte)1,

-    }
-    public abstract class DocumentationProvider {
 {
-        protected DocumentationProvider();

-        public static DocumentationProvider Default { get; }

-        public abstract override bool Equals(object obj);

-        protected internal abstract string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken));

-        public abstract override int GetHashCode();

-    }
-    public sealed class EmbeddedText {
 {
-        public ImmutableArray<byte> Checksum { get; }

-        public SourceHashAlgorithm ChecksumAlgorithm { get; }

-        public string FilePath { get; }

-        public static EmbeddedText FromBytes(string filePath, ArraySegment<byte> bytes, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1);

-        public static EmbeddedText FromSource(string filePath, SourceText text);

-        public static EmbeddedText FromStream(string filePath, Stream stream, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1);

-    }
-    public struct FileLinePositionSpan : IEquatable<FileLinePositionSpan> {
 {
-        public FileLinePositionSpan(string path, LinePosition start, LinePosition end);

-        public FileLinePositionSpan(string path, LinePositionSpan span);

-        public LinePosition EndLinePosition { get; }

-        public bool HasMappedPath { get; }

-        public bool IsValid { get; }

-        public string Path { get; }

-        public LinePositionSpan Span { get; }

-        public LinePosition StartLinePosition { get; }

-        public bool Equals(FileLinePositionSpan other);

-        public override bool Equals(object other);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public static class FileSystemExtensions {
 {
-        public static EmitResult Emit(this Compilation compilation, string outputPath, string pdbPath = null, string xmlDocPath = null, string win32ResourcesPath = null, IEnumerable<ResourceDescription> manifestResources = null, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IAliasSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        INamespaceOrTypeSymbol Target { get; }

-    }
-    public interface IAnalyzerAssemblyLoader {
 {
-        void AddDependencyLocation(string fullPath);

-        Assembly LoadFromPath(string fullPath);

-    }
-    public interface IArrayTypeSymbol : IEquatable<ISymbol>, INamespaceOrTypeSymbol, ISymbol, ITypeSymbol {
 {
-        ImmutableArray<CustomModifier> CustomModifiers { get; }

-        ITypeSymbol ElementType { get; }

-        bool IsSZArray { get; }

-        ImmutableArray<int> LowerBounds { get; }

-        int Rank { get; }

-        ImmutableArray<int> Sizes { get; }

-        bool Equals(IArrayTypeSymbol other);

-    }
-    public interface IAssemblySymbol : IEquatable<ISymbol>, ISymbol {
 {
-        INamespaceSymbol GlobalNamespace { get; }

-        AssemblyIdentity Identity { get; }

-        bool IsInteractive { get; }

-        bool MightContainExtensionMethods { get; }

-        IEnumerable<IModuleSymbol> Modules { get; }

-        ICollection<string> NamespaceNames { get; }

-        ICollection<string> TypeNames { get; }

-        AssemblyMetadata GetMetadata();

-        INamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName);

-        bool GivesAccessTo(IAssemblySymbol toAssembly);

-        INamedTypeSymbol ResolveForwardedType(string fullyQualifiedMetadataName);

-    }
-    public interface ICompilationUnitSyntax {
 {
-        SyntaxToken EndOfFileToken { get; }

-    }
-    public interface IDiscardSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        ITypeSymbol Type { get; }

-    }
-    public interface IDynamicTypeSymbol : IEquatable<ISymbol>, INamespaceOrTypeSymbol, ISymbol, ITypeSymbol

-    public interface IErrorTypeSymbol : IEquatable<ISymbol>, INamedTypeSymbol, INamespaceOrTypeSymbol, ISymbol, ITypeSymbol {
 {
-        CandidateReason CandidateReason { get; }

-        ImmutableArray<ISymbol> CandidateSymbols { get; }

-    }
-    public interface IEventSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        IMethodSymbol AddMethod { get; }

-        ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations { get; }

-        bool IsWindowsRuntimeEvent { get; }

-        new IEventSymbol OriginalDefinition { get; }

-        IEventSymbol OverriddenEvent { get; }

-        IMethodSymbol RaiseMethod { get; }

-        IMethodSymbol RemoveMethod { get; }

-        ITypeSymbol Type { get; }

-    }
-    public interface IFieldSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        ISymbol AssociatedSymbol { get; }

-        object ConstantValue { get; }

-        IFieldSymbol CorrespondingTupleField { get; }

-        ImmutableArray<CustomModifier> CustomModifiers { get; }

-        bool HasConstantValue { get; }

-        bool IsConst { get; }

-        bool IsReadOnly { get; }

-        bool IsVolatile { get; }

-        new IFieldSymbol OriginalDefinition { get; }

-        ITypeSymbol Type { get; }

-    }
-    public interface ILabelSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        IMethodSymbol ContainingMethod { get; }

-    }
-    public interface ILocalSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        object ConstantValue { get; }

-        bool HasConstantValue { get; }

-        bool IsConst { get; }

-        bool IsFunctionValue { get; }

-        bool IsRef { get; }

-        RefKind RefKind { get; }

-        ITypeSymbol Type { get; }

-    }
-    public interface IMethodSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        int Arity { get; }

-        INamedTypeSymbol AssociatedAnonymousDelegate { get; }

-        ISymbol AssociatedSymbol { get; }

-        IMethodSymbol ConstructedFrom { get; }

-        ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations { get; }

-        bool HidesBaseMethodsByName { get; }

-        bool IsAsync { get; }

-        bool IsCheckedBuiltin { get; }

-        bool IsExtensionMethod { get; }

-        bool IsGenericMethod { get; }

-        bool IsVararg { get; }

-        MethodKind MethodKind { get; }

-        new IMethodSymbol OriginalDefinition { get; }

-        IMethodSymbol OverriddenMethod { get; }

-        ImmutableArray<IParameterSymbol> Parameters { get; }

-        IMethodSymbol PartialDefinitionPart { get; }

-        IMethodSymbol PartialImplementationPart { get; }

-        ITypeSymbol ReceiverType { get; }

-        IMethodSymbol ReducedFrom { get; }

-        ImmutableArray<CustomModifier> RefCustomModifiers { get; }

-        RefKind RefKind { get; }

-        bool ReturnsByRef { get; }

-        bool ReturnsByRefReadonly { get; }

-        bool ReturnsVoid { get; }

-        ITypeSymbol ReturnType { get; }

-        ImmutableArray<CustomModifier> ReturnTypeCustomModifiers { get; }

-        ImmutableArray<ITypeSymbol> TypeArguments { get; }

-        ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }

-        IMethodSymbol Construct(params ITypeSymbol[] typeArguments);

-        DllImportData GetDllImportData();

-        ImmutableArray<AttributeData> GetReturnTypeAttributes();

-        ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter);

-        IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType);

-    }
-    public interface IModuleSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        INamespaceSymbol GlobalNamespace { get; }

-        ImmutableArray<AssemblyIdentity> ReferencedAssemblies { get; }

-        ImmutableArray<IAssemblySymbol> ReferencedAssemblySymbols { get; }

-        ModuleMetadata GetMetadata();

-        INamespaceSymbol GetModuleNamespace(INamespaceSymbol namespaceSymbol);

-    }
-    public interface INamedTypeSymbol : IEquatable<ISymbol>, INamespaceOrTypeSymbol, ISymbol, ITypeSymbol {
 {
-        int Arity { get; }

-        ISymbol AssociatedSymbol { get; }

-        INamedTypeSymbol ConstructedFrom { get; }

-        ImmutableArray<IMethodSymbol> Constructors { get; }

-        IMethodSymbol DelegateInvokeMethod { get; }

-        INamedTypeSymbol EnumUnderlyingType { get; }

-        ImmutableArray<IMethodSymbol> InstanceConstructors { get; }

-        bool IsComImport { get; }

-        bool IsGenericType { get; }

-        bool IsImplicitClass { get; }

-        bool IsScriptClass { get; }

-        bool IsSerializable { get; }

-        bool IsUnboundGenericType { get; }

-        IEnumerable<string> MemberNames { get; }

-        bool MightContainExtensionMethods { get; }

-        new INamedTypeSymbol OriginalDefinition { get; }

-        ImmutableArray<IMethodSymbol> StaticConstructors { get; }

-        ImmutableArray<IFieldSymbol> TupleElements { get; }

-        INamedTypeSymbol TupleUnderlyingType { get; }

-        ImmutableArray<ITypeSymbol> TypeArguments { get; }

-        ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }

-        INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments);

-        INamedTypeSymbol ConstructUnboundGenericType();

-        ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal);

-    }
-    public interface INamespaceOrTypeSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        bool IsNamespace { get; }

-        bool IsType { get; }

-        ImmutableArray<ISymbol> GetMembers();

-        ImmutableArray<ISymbol> GetMembers(string name);

-        ImmutableArray<INamedTypeSymbol> GetTypeMembers();

-        ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name);

-        ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name, int arity);

-    }
-    public interface INamespaceSymbol : IEquatable<ISymbol>, INamespaceOrTypeSymbol, ISymbol {
 {
-        ImmutableArray<INamespaceSymbol> ConstituentNamespaces { get; }

-        Compilation ContainingCompilation { get; }

-        bool IsGlobalNamespace { get; }

-        NamespaceKind NamespaceKind { get; }

-        new IEnumerable<INamespaceOrTypeSymbol> GetMembers();

-        new IEnumerable<INamespaceOrTypeSymbol> GetMembers(string name);

-        IEnumerable<INamespaceSymbol> GetNamespaceMembers();

-    }
-    public interface IOperation {
 {
-        IEnumerable<IOperation> Children { get; }

-        Optional<object> ConstantValue { get; }

-        bool IsImplicit { get; }

-        OperationKind Kind { get; }

-        string Language { get; }

-        IOperation Parent { get; }

-        SyntaxNode Syntax { get; }

-        ITypeSymbol Type { get; }

-        void Accept(OperationVisitor visitor);

-        TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

-    }
-    public interface IParameterSymbol : IEquatable<ISymbol>, ISymbol {
 {
-        ImmutableArray<CustomModifier> CustomModifiers { get; }

-        object ExplicitDefaultValue { get; }

-        bool HasExplicitDefaultValue { get; }

-        bool IsOptional { get; }

-        bool IsParams { get; }

-        bool IsThis { get; }

-        int Ordinal { get; }

-        new IParameterSymbol OriginalDefinition { get; }

-        ImmutableArray<CustomModifier> RefCustomModifiers { get; }

-        RefKind RefKind { get; }

-        ITypeSymbol Type { get; }

-    }
-    public interface IPointerTypeSymbol : IEquatable<ISymbol>, INamespaceOrTypeSymbol, ISymbol, ITypeSymbol {
 {
-        ImmutableArray<CustomModifier> CustomModifiers { get; }

-        ITypeSymbol PointedAtType { get; }

-    }
-    public interface IPreprocessingSymbol : IEquatable<ISymbol>, ISymbol

-    public interface IPropertySymbol : IEquatable<ISymbol>, ISymbol {
 {
-        ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations { get; }

-        IMethodSymbol GetMethod { get; }

-        bool IsIndexer { get; }

-        bool IsReadOnly { get; }

-        bool IsWithEvents { get; }

-        bool IsWriteOnly { get; }

-        new IPropertySymbol OriginalDefinition { get; }

-        IPropertySymbol OverriddenProperty { get; }

-        ImmutableArray<IParameterSymbol> Parameters { get; }

-        ImmutableArray<CustomModifier> RefCustomModifiers { get; }

-        RefKind RefKind { get; }

-        bool ReturnsByRef { get; }

-        bool ReturnsByRefReadonly { get; }

-        IMethodSymbol SetMethod { get; }

-        ITypeSymbol Type { get; }

-        ImmutableArray<CustomModifier> TypeCustomModifiers { get; }

-    }
-    public interface IRangeVariableSymbol : IEquatable<ISymbol>, ISymbol

-    public interface ISkippedTokensTriviaSyntax {
 {
-        SyntaxTokenList Tokens { get; }

-    }
-    public interface ISourceAssemblySymbol : IAssemblySymbol, IEquatable<ISymbol>, ISymbol {
 {
-        Compilation Compilation { get; }

-    }
-    public interface IStructuredTriviaSyntax {
 {
-        SyntaxTrivia ParentTrivia { get; }

-    }
-    public interface ISymbol : IEquatable<ISymbol> {
 {
-        bool CanBeReferencedByName { get; }

-        IAssemblySymbol ContainingAssembly { get; }

-        IModuleSymbol ContainingModule { get; }

-        INamespaceSymbol ContainingNamespace { get; }

-        ISymbol ContainingSymbol { get; }

-        INamedTypeSymbol ContainingType { get; }

-        Accessibility DeclaredAccessibility { get; }

-        ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get; }

-        bool HasUnsupportedMetadata { get; }

-        bool IsAbstract { get; }

-        bool IsDefinition { get; }

-        bool IsExtern { get; }

-        bool IsImplicitlyDeclared { get; }

-        bool IsOverride { get; }

-        bool IsSealed { get; }

-        bool IsStatic { get; }

-        bool IsVirtual { get; }

-        SymbolKind Kind { get; }

-        string Language { get; }

-        ImmutableArray<Location> Locations { get; }

-        string MetadataName { get; }

-        string Name { get; }

-        ISymbol OriginalDefinition { get; }

-        void Accept(SymbolVisitor visitor);

-        TResult Accept<TResult>(SymbolVisitor<TResult> visitor);

-        ImmutableArray<AttributeData> GetAttributes();

-        string GetDocumentationCommentId();

-        string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken));

-        ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null);

-        string ToDisplayString(SymbolDisplayFormat format = null);

-        ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null);

-        string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null);

-    }
-    public static class ISymbolExtensions {
 {
-        public static IMethodSymbol GetConstructedReducedFrom(this IMethodSymbol method);

-    }
-    public interface ITypeParameterSymbol : IEquatable<ISymbol>, INamespaceOrTypeSymbol, ISymbol, ITypeSymbol {
 {
-        ImmutableArray<ITypeSymbol> ConstraintTypes { get; }

-        IMethodSymbol DeclaringMethod { get; }

-        INamedTypeSymbol DeclaringType { get; }

-        bool HasConstructorConstraint { get; }

-        bool HasReferenceTypeConstraint { get; }

-        bool HasUnmanagedTypeConstraint { get; }

-        bool HasValueTypeConstraint { get; }

-        int Ordinal { get; }

-        new ITypeParameterSymbol OriginalDefinition { get; }

-        ITypeParameterSymbol ReducedFrom { get; }

-        TypeParameterKind TypeParameterKind { get; }

-        VarianceKind Variance { get; }

-    }
-    public interface ITypeSymbol : IEquatable<ISymbol>, INamespaceOrTypeSymbol, ISymbol {
 {
-        ImmutableArray<INamedTypeSymbol> AllInterfaces { get; }

-        INamedTypeSymbol BaseType { get; }

-        ImmutableArray<INamedTypeSymbol> Interfaces { get; }

-        bool IsAnonymousType { get; }

-        bool IsReferenceType { get; }

-        bool IsTupleType { get; }

-        bool IsValueType { get; }

-        new ITypeSymbol OriginalDefinition { get; }

-        SpecialType SpecialType { get; }

-        TypeKind TypeKind { get; }

-        ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember);

-    }
-    public static class LanguageNames {
 {
-        public const string CSharp = "C#";

-        public const string FSharp = "F#";

-        public const string VisualBasic = "Visual Basic";

-    }
-    public enum LineVisibility {
 {
-        BeforeFirstLineDirective = 0,

-        Hidden = 1,

-        Visible = 2,

-    }
-    public sealed class LocalizableResourceString : LocalizableString, IObjectWritable {
 {
-        public LocalizableResourceString(string nameOfLocalizableResource, ResourceManager resourceManager, Type resourceSource);

-        public LocalizableResourceString(string nameOfLocalizableResource, ResourceManager resourceManager, Type resourceSource, params string[] formatArguments);

-        protected override bool AreEqual(object other);

-        protected override int GetHash();

-        protected override string GetText(IFormatProvider formatProvider);

-    }
-    public abstract class LocalizableString : IEquatable<LocalizableString>, IFormattable {
 {
-        protected LocalizableString();

-        public event EventHandler<Exception> OnException;

-        protected abstract bool AreEqual(object other);

-        public bool Equals(LocalizableString other);

-        public sealed override bool Equals(object other);

-        protected abstract int GetHash();

-        public sealed override int GetHashCode();

-        protected abstract string GetText(IFormatProvider formatProvider);

-        public static explicit operator string (LocalizableString localizableResource);

-        public static implicit operator LocalizableString (string fixedResource);

-        string System.IFormattable.ToString(string ignored, IFormatProvider formatProvider);

-        public sealed override string ToString();

-        public string ToString(IFormatProvider formatProvider);

-    }
-    public abstract class Location {
 {
-        public bool IsInMetadata { get; }

-        public bool IsInSource { get; }

-        public abstract LocationKind Kind { get; }

-        public virtual IModuleSymbol MetadataModule { get; }

-        public static Location None { get; }

-        public virtual TextSpan SourceSpan { get; }

-        public virtual SyntaxTree SourceTree { get; }

-        public static Location Create(SyntaxTree syntaxTree, TextSpan textSpan);

-        public static Location Create(string filePath, TextSpan textSpan, LinePositionSpan lineSpan);

-        public abstract override bool Equals(object obj);

-        protected virtual string GetDebuggerDisplay();

-        public abstract override int GetHashCode();

-        public virtual FileLinePositionSpan GetLineSpan();

-        public virtual FileLinePositionSpan GetMappedLineSpan();

-        public static bool operator ==(Location left, Location right);

-        public static bool operator !=(Location left, Location right);

-        public override string ToString();

-    }
-    public enum LocationKind : byte {
 {
-        ExternalFile = (byte)4,

-        MetadataFile = (byte)2,

-        None = (byte)0,

-        SourceFile = (byte)1,

-        XmlFile = (byte)3,

-    }
-    public abstract class Metadata : IDisposable {
 {
-        public MetadataId Id { get; }

-        public abstract MetadataImageKind Kind { get; }

-        protected abstract Metadata CommonCopy();

-        public Metadata Copy();

-        public abstract void Dispose();

-    }
-    public sealed class MetadataId

-    public enum MetadataImageKind : byte {
 {
-        Assembly = (byte)0,

-        Module = (byte)1,

-    }
-    public enum MetadataImportOptions : byte {
 {
-        All = (byte)2,

-        Internal = (byte)1,

-        Public = (byte)0,

-    }
-    public abstract class MetadataReference {
 {
-        protected MetadataReference(MetadataReferenceProperties properties);

-        public virtual string Display { get; }

-        public MetadataReferenceProperties Properties { get; }

-        public static MetadataReference CreateFromAssembly(Assembly assembly);

-        public static MetadataReference CreateFromAssembly(Assembly assembly, MetadataReferenceProperties properties, DocumentationProvider documentation = null);

-        public static PortableExecutableReference CreateFromFile(string path, MetadataReferenceProperties properties = default(MetadataReferenceProperties), DocumentationProvider documentation = null);

-        public static PortableExecutableReference CreateFromImage(IEnumerable<byte> peImage, MetadataReferenceProperties properties = default(MetadataReferenceProperties), DocumentationProvider documentation = null, string filePath = null);

-        public static PortableExecutableReference CreateFromImage(ImmutableArray<byte> peImage, MetadataReferenceProperties properties = default(MetadataReferenceProperties), DocumentationProvider documentation = null, string filePath = null);

-        public static PortableExecutableReference CreateFromStream(Stream peStream, MetadataReferenceProperties properties = default(MetadataReferenceProperties), DocumentationProvider documentation = null, string filePath = null);

-        public MetadataReference WithAliases(IEnumerable<string> aliases);

-        public MetadataReference WithAliases(ImmutableArray<string> aliases);

-        public MetadataReference WithEmbedInteropTypes(bool value);

-        public MetadataReference WithProperties(MetadataReferenceProperties properties);

-    }
-    public struct MetadataReferenceProperties : IEquatable<MetadataReferenceProperties> {
 {
-        public MetadataReferenceProperties(MetadataImageKind kind = MetadataImageKind.Assembly, ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false);

-        public ImmutableArray<string> Aliases { get; }

-        public static MetadataReferenceProperties Assembly { get; }

-        public bool EmbedInteropTypes { get; }

-        public static string GlobalAlias { get; }

-        public MetadataImageKind Kind { get; }

-        public static MetadataReferenceProperties Module { get; }

-        public bool Equals(MetadataReferenceProperties other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(MetadataReferenceProperties left, MetadataReferenceProperties right);

-        public static bool operator !=(MetadataReferenceProperties left, MetadataReferenceProperties right);

-        public MetadataReferenceProperties WithAliases(IEnumerable<string> aliases);

-        public MetadataReferenceProperties WithAliases(ImmutableArray<string> aliases);

-        public MetadataReferenceProperties WithEmbedInteropTypes(bool embedInteropTypes);

-    }
-    public abstract class MetadataReferenceResolver {
 {
-        protected MetadataReferenceResolver();

-        public virtual bool ResolveMissingAssemblies { get; }

-        public abstract override bool Equals(object other);

-        public abstract override int GetHashCode();

-        public virtual PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity);

-        public abstract ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties);

-    }
-    public enum MethodKind {
 {
-        AnonymousFunction = 0,

-        BuiltinOperator = 15,

-        Constructor = 1,

-        Conversion = 2,

-        DeclareMethod = 16,

-        DelegateInvoke = 3,

-        Destructor = 4,

-        EventAdd = 5,

-        EventRaise = 6,

-        EventRemove = 7,

-        ExplicitInterfaceImplementation = 8,

-        LambdaMethod = 0,

-        LocalFunction = 17,

-        Ordinary = 10,

-        PropertyGet = 11,

-        PropertySet = 12,

-        ReducedExtension = 13,

-        SharedConstructor = 14,

-        StaticConstructor = 14,

-        UserDefinedOperator = 9,

-    }
-    public static class ModelExtensions {
 {
-        public static ControlFlowAnalysis AnalyzeControlFlow(this SemanticModel semanticModel, SyntaxNode statement);

-        public static ControlFlowAnalysis AnalyzeControlFlow(this SemanticModel semanticModel, SyntaxNode firstStatement, SyntaxNode lastStatement);

-        public static DataFlowAnalysis AnalyzeDataFlow(this SemanticModel semanticModel, SyntaxNode statementOrExpression);

-        public static DataFlowAnalysis AnalyzeDataFlow(this SemanticModel semanticModel, SyntaxNode firstStatement, SyntaxNode lastStatement);

-        public static IAliasSymbol GetAliasInfo(this SemanticModel semanticModel, SyntaxNode nameSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public static ISymbol GetDeclaredSymbol(this SemanticModel semanticModel, SyntaxNode declaration, CancellationToken cancellationToken = default(CancellationToken));

-        public static ImmutableArray<ISymbol> GetMemberGroup(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));

-        public static IAliasSymbol GetSpeculativeAliasInfo(this SemanticModel semanticModel, int position, SyntaxNode nameSyntax, SpeculativeBindingOption bindingOption);

-        public static SymbolInfo GetSpeculativeSymbolInfo(this SemanticModel semanticModel, int position, SyntaxNode expression, SpeculativeBindingOption bindingOption);

-        public static TypeInfo GetSpeculativeTypeInfo(this SemanticModel semanticModel, int position, SyntaxNode expression, SpeculativeBindingOption bindingOption);

-        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));

-        public static TypeInfo GetTypeInfo(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public sealed class ModuleMetadata : Metadata {
 {
-        public bool IsDisposed { get; }

-        public override MetadataImageKind Kind { get; }

-        public string Name { get; }

-        protected override Metadata CommonCopy();

-        public static ModuleMetadata CreateFromFile(string path);

-        public static ModuleMetadata CreateFromImage(IEnumerable<byte> peImage);

-        public static ModuleMetadata CreateFromImage(ImmutableArray<byte> peImage);

-        public static ModuleMetadata CreateFromImage(IntPtr peImage, int size);

-        public static ModuleMetadata CreateFromMetadata(IntPtr metadata, int size);

-        public static ModuleMetadata CreateFromStream(Stream peStream, bool leaveOpen = false);

-        public static ModuleMetadata CreateFromStream(Stream peStream, PEStreamOptions options);

-        public override void Dispose();

-        public MetadataReader GetMetadataReader();

-        public ImmutableArray<string> GetModuleNames();

-        public Guid GetModuleVersionId();

-        public PortableExecutableReference GetReference(DocumentationProvider documentation = null, string filePath = null, string display = null);

-    }
-    public enum NamespaceKind {
 {
-        Assembly = 2,

-        Compilation = 3,

-        Module = 1,

-    }
-    public enum OperationKind {
 {
-        AddressOf = 64,

-        AnonymousFunction = 35,

-        AnonymousObjectCreation = 49,

-        Argument = 79,

-        ArrayCreation = 38,

-        ArrayElementReference = 23,

-        ArrayInitializer = 76,

-        Await = 41,

-        BinaryOperator = 32,

-        Block = 2,

-        Branch = 7,

-        CaseClause = 82,

-        CatchClause = 80,

-        Coalesce = 34,

-        CollectionElementInitializer = 52,

-        CompoundAssignment = 43,

-        Conditional = 33,

-        ConditionalAccess = 46,

-        ConditionalAccessInstance = 47,

-        ConstantPattern = 85,

-        ConstructorBodyOperation = 89,

-        Conversion = 21,

-        DeclarationExpression = 70,

-        DeclarationPattern = 86,

-        DeconstructionAssignment = 69,

-        Decrement = 68,

-        DefaultValue = 61,

-        DelegateCreation = 60,

-        Discard = 90,

-        DynamicIndexerAccess = 58,

-        DynamicInvocation = 57,

-        DynamicMemberReference = 56,

-        DynamicObjectCreation = 55,

-        Empty = 8,

-        End = 18,

-        EventAssignment = 45,

-        EventReference = 30,

-        ExpressionStatement = 15,

-        FieldInitializer = 72,

-        FieldReference = 26,

-        Increment = 66,

-        InstanceReference = 39,

-        InterpolatedString = 48,

-        InterpolatedStringText = 83,

-        Interpolation = 84,

-        Invalid = 1,

-        Invocation = 22,

-        IsPattern = 65,

-        IsType = 40,

-        Labeled = 6,

-        Literal = 20,

-        LocalFunction = 16,

-        LocalReference = 24,

-        Lock = 11,

-        Loop = 5,

-        MemberInitializer = 51,

-        MethodBodyOperation = 88,

-        MethodReference = 27,

-        NameOf = 53,

-        None = 0,

-        ObjectCreation = 36,

-        ObjectOrCollectionInitializer = 50,

-        OmittedArgument = 71,

-        ParameterInitializer = 75,

-        ParameterReference = 25,

-        Parenthesized = 44,

-        PropertyInitializer = 74,

-        PropertyReference = 28,

-        RaiseEvent = 19,

-        Return = 9,

-        SimpleAssignment = 42,

-        SizeOf = 63,

-        Stop = 17,

-        Switch = 4,

-        SwitchCase = 81,

-        Throw = 67,

-        TranslatedQuery = 59,

-        Try = 12,

-        Tuple = 54,

-        TupleBinaryOperator = 87,

-        TypeOf = 62,

-        TypeParameterObjectCreation = 37,

-        UnaryOperator = 31,

-        Using = 13,

-        VariableDeclaration = 78,

-        VariableDeclarationGroup = 3,

-        VariableDeclarator = 77,

-        VariableInitializer = 73,

-        YieldBreak = 10,

-        YieldReturn = 14,

-    }
-    public enum OptimizationLevel {
 {
-        Debug = 0,

-        Release = 1,

-    }
-    public struct Optional<T> {
 {
-        public Optional(T value);

-        public bool HasValue { get; }

-        public T Value { get; }

-        public static implicit operator Optional<T> (T value);

-        public override string ToString();

-    }
-    public enum OutputKind {
 {
-        ConsoleApplication = 0,

-        DynamicallyLinkedLibrary = 2,

-        NetModule = 3,

-        WindowsApplication = 1,

-        WindowsRuntimeApplication = 5,

-        WindowsRuntimeMetadata = 4,

-    }
-    public abstract class ParseOptions {
 {
-        public DocumentationMode DocumentationMode { get; protected set; }

-        public ImmutableArray<Diagnostic> Errors { get; }

-        public abstract IReadOnlyDictionary<string, string> Features { get; }

-        public SourceCodeKind Kind { get; protected set; }

-        public abstract string Language { get; }

-        public abstract IEnumerable<string> PreprocessorSymbolNames { get; }

-        public SourceCodeKind SpecifiedKind { get; protected set; }

-        protected abstract ParseOptions CommonWithDocumentationMode(DocumentationMode documentationMode);

-        protected abstract ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features);

-        public abstract ParseOptions CommonWithKind(SourceCodeKind kind);

-        public abstract override bool Equals(object obj);

-        protected bool EqualsHelper(ParseOptions other);

-        public abstract override int GetHashCode();

-        protected int GetHashCodeHelper();

-        public static bool operator ==(ParseOptions left, ParseOptions right);

-        public static bool operator !=(ParseOptions left, ParseOptions right);

-        public ParseOptions WithDocumentationMode(DocumentationMode documentationMode);

-        public ParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features);

-        public ParseOptions WithKind(SourceCodeKind kind);

-    }
-    public enum Platform {
 {
-        AnyCpu = 0,

-        AnyCpu32BitPreferred = 4,

-        Arm = 5,

-        Arm64 = 6,

-        Itanium = 3,

-        X64 = 2,

-        X86 = 1,

-    }
-    public abstract class PortableExecutableReference : MetadataReference {
 {
-        protected PortableExecutableReference(MetadataReferenceProperties properties, string fullPath = null, DocumentationProvider initialDocumentation = null);

-        public override string Display { get; }

-        public string FilePath { get; }

-        protected abstract DocumentationProvider CreateDocumentationProvider();

-        public Metadata GetMetadata();

-        public MetadataId GetMetadataId();

-        protected abstract Metadata GetMetadataImpl();

-        public new PortableExecutableReference WithAliases(IEnumerable<string> aliases);

-        public new PortableExecutableReference WithAliases(ImmutableArray<string> aliases);

-        public new PortableExecutableReference WithEmbedInteropTypes(bool value);

-        public new PortableExecutableReference WithProperties(MetadataReferenceProperties properties);

-        protected abstract PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties);

-    }
-    public struct PreprocessingSymbolInfo : IEquatable<PreprocessingSymbolInfo> {
 {
-        public bool IsDefined { get; }

-        public IPreprocessingSymbol Symbol { get; }

-        public bool Equals(PreprocessingSymbolInfo other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public enum RefKind : byte {
 {
-        In = (byte)3,

-        None = (byte)0,

-        Out = (byte)2,

-        Ref = (byte)1,

-        RefReadOnly = (byte)3,

-    }
-    public enum ReportDiagnostic {
 {
-        Default = 0,

-        Error = 1,

-        Hidden = 4,

-        Info = 3,

-        Suppress = 5,

-        Warn = 2,

-    }
-    public sealed class ResourceDescription : IFileReference {
 {
-        public ResourceDescription(string resourceName, Func<Stream> dataProvider, bool isPublic);

-        public ResourceDescription(string resourceName, string fileName, Func<Stream> dataProvider, bool isPublic);

-    }
-    public class RuleSet {
 {
-        public RuleSet(string filePath, ReportDiagnostic generalOption, ImmutableDictionary<string, ReportDiagnostic> specificOptions, ImmutableArray<RuleSetInclude> includes);

-        public string FilePath { get; }

-        public ReportDiagnostic GeneralDiagnosticOption { get; }

-        public ImmutableArray<RuleSetInclude> Includes { get; }

-        public ImmutableDictionary<string, ReportDiagnostic> SpecificDiagnosticOptions { get; }

-        public static ReportDiagnostic GetDiagnosticOptionsFromRulesetFile(string rulesetFileFullPath, out Dictionary<string, ReportDiagnostic> specificDiagnosticOptions);

-        public static ImmutableArray<string> GetEffectiveIncludesFromFile(string filePath);

-        public static RuleSet LoadEffectiveRuleSetFromFile(string filePath);

-        public RuleSet WithEffectiveAction(ReportDiagnostic action);

-    }
-    public class RuleSetInclude {
 {
-        public RuleSetInclude(string includePath, ReportDiagnostic action);

-        public ReportDiagnostic Action { get; }

-        public string IncludePath { get; }

-        public RuleSet LoadRuleSet(RuleSet parent);

-    }
-    public abstract class ScriptCompilationInfo {
 {
-        public Type GlobalsType { get; }

-        public Compilation PreviousScriptCompilation { get; }

-        public Type ReturnType { get; }

-        public ScriptCompilationInfo WithPreviousScriptCompilation(Compilation compilation);

-    }
-    public abstract class SemanticModel {
 {
-        protected SemanticModel();

-        public Compilation Compilation { get; }

-        protected abstract Compilation CompilationCore { get; }

-        public virtual bool IgnoresAccessibility { get; }

-        public abstract bool IsSpeculativeSemanticModel { get; }

-        public abstract string Language { get; }

-        public abstract int OriginalPositionForSpeculation { get; }

-        public SemanticModel ParentModel { get; }

-        protected abstract SemanticModel ParentModelCore { get; }

-        protected abstract SyntaxNode RootCore { get; }

-        public SyntaxTree SyntaxTree { get; }

-        protected abstract SyntaxTree SyntaxTreeCore { get; }

-        protected abstract ControlFlowAnalysis AnalyzeControlFlowCore(SyntaxNode statement);

-        protected abstract ControlFlowAnalysis AnalyzeControlFlowCore(SyntaxNode firstStatement, SyntaxNode lastStatement);

-        protected abstract DataFlowAnalysis AnalyzeDataFlowCore(SyntaxNode statementOrExpression);

-        protected abstract DataFlowAnalysis AnalyzeDataFlowCore(SyntaxNode firstStatement, SyntaxNode lastStatement);

-        protected abstract IAliasSymbol GetAliasInfoCore(SyntaxNode nameSyntax, CancellationToken cancellationToken = default(CancellationToken));

-        public Optional<object> GetConstantValue(SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));

-        protected abstract Optional<object> GetConstantValueCore(SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));

-        public abstract ImmutableArray<Diagnostic> GetDeclarationDiagnostics(Nullable<TextSpan> span = default(Nullable<TextSpan>), CancellationToken cancellationToken = default(CancellationToken));

-        protected abstract ISymbol GetDeclaredSymbolCore(SyntaxNode declaration, CancellationToken cancellationToken = default(CancellationToken));

-        protected abstract ImmutableArray<ISymbol> GetDeclaredSymbolsCore(SyntaxNode declaration, CancellationToken cancellationToken = default(CancellationToken));

-        public abstract ImmutableArray<Diagnostic> GetDiagnostics(Nullable<TextSpan> span = default(Nullable<TextSpan>), CancellationToken cancellationToken = default(CancellationToken));

-        public ISymbol GetEnclosingSymbol(int position, CancellationToken cancellationToken = default(CancellationToken));

-        protected abstract ISymbol GetEnclosingSymbolCore(int position, CancellationToken cancellationToken = default(CancellationToken));

-        protected abstract ImmutableArray<ISymbol> GetMemberGroupCore(SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));

-        public abstract ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(Nullable<TextSpan> span = default(Nullable<TextSpan>), CancellationToken cancellationToken = default(CancellationToken));

-        public IOperation GetOperation(SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));

-        protected abstract IOperation GetOperationCore(SyntaxNode node, CancellationToken cancellationToken);

-        public PreprocessingSymbolInfo GetPreprocessingSymbolInfo(SyntaxNode nameSyntax);

-        protected abstract PreprocessingSymbolInfo GetPreprocessingSymbolInfoCore(SyntaxNode nameSyntax);

-        protected abstract IAliasSymbol GetSpeculativeAliasInfoCore(int position, SyntaxNode nameSyntax, SpeculativeBindingOption bindingOption);

-        protected abstract SymbolInfo GetSpeculativeSymbolInfoCore(int position, SyntaxNode expression, SpeculativeBindingOption bindingOption);

-        protected abstract TypeInfo GetSpeculativeTypeInfoCore(int position, SyntaxNode expression, SpeculativeBindingOption bindingOption);

-        protected abstract SymbolInfo GetSymbolInfoCore(SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));

-        public abstract ImmutableArray<Diagnostic> GetSyntaxDiagnostics(Nullable<TextSpan> span = default(Nullable<TextSpan>), CancellationToken cancellationToken = default(CancellationToken));

-        protected internal virtual SyntaxNode GetTopmostNodeForDiagnosticAnalysis(ISymbol symbol, SyntaxNode declaringSyntax);

-        protected abstract TypeInfo GetTypeInfoCore(SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));

-        public bool IsAccessible(int position, ISymbol symbol);

-        protected abstract bool IsAccessibleCore(int position, ISymbol symbol);

-        public bool IsEventUsableAsField(int position, IEventSymbol eventSymbol);

-        protected abstract bool IsEventUsableAsFieldCore(int position, IEventSymbol eventSymbol);

-        public ImmutableArray<ISymbol> LookupBaseMembers(int position, string name = null);

-        protected abstract ImmutableArray<ISymbol> LookupBaseMembersCore(int position, string name);

-        public ImmutableArray<ISymbol> LookupLabels(int position, string name = null);

-        protected abstract ImmutableArray<ISymbol> LookupLabelsCore(int position, string name);

-        public ImmutableArray<ISymbol> LookupNamespacesAndTypes(int position, INamespaceOrTypeSymbol container = null, string name = null);

-        protected abstract ImmutableArray<ISymbol> LookupNamespacesAndTypesCore(int position, INamespaceOrTypeSymbol container, string name);

-        public ImmutableArray<ISymbol> LookupStaticMembers(int position, INamespaceOrTypeSymbol container = null, string name = null);

-        protected abstract ImmutableArray<ISymbol> LookupStaticMembersCore(int position, INamespaceOrTypeSymbol container, string name);

-        public ImmutableArray<ISymbol> LookupSymbols(int position, INamespaceOrTypeSymbol container = null, string name = null, bool includeReducedExtensionMethods = false);

-        protected abstract ImmutableArray<ISymbol> LookupSymbolsCore(int position, INamespaceOrTypeSymbol container, string name, bool includeReducedExtensionMethods);

-    }
-    public struct SeparatedSyntaxList<TNode> : IEnumerable, IEnumerable<TNode>, IEquatable<SeparatedSyntaxList<TNode>>, IReadOnlyCollection<TNode>, IReadOnlyList<TNode> where TNode : SyntaxNode {
 {
-        public int Count { get; }

-        public TextSpan FullSpan { get; }

-        public int SeparatorCount { get; }

-        public TextSpan Span { get; }

-        public TNode this[int index] { get; }

-        public SeparatedSyntaxList<TNode> Add(TNode node);

-        public SeparatedSyntaxList<TNode> AddRange(IEnumerable<TNode> nodes);

-        public bool Any();

-        public bool Contains(TNode node);

-        public bool Equals(SeparatedSyntaxList<TNode> other);

-        public override bool Equals(object obj);

-        public TNode First();

-        public TNode FirstOrDefault();

-        public SeparatedSyntaxList<TNode>.Enumerator GetEnumerator();

-        public override int GetHashCode();

-        public SyntaxToken GetSeparator(int index);

-        public IEnumerable<SyntaxToken> GetSeparators();

-        public SyntaxNodeOrTokenList GetWithSeparators();

-        public int IndexOf(Func<TNode, bool> predicate);

-        public int IndexOf(TNode node);

-        public SeparatedSyntaxList<TNode> Insert(int index, TNode node);

-        public SeparatedSyntaxList<TNode> InsertRange(int index, IEnumerable<TNode> nodes);

-        public TNode Last();

-        public int LastIndexOf(Func<TNode, bool> predicate);

-        public int LastIndexOf(TNode node);

-        public TNode LastOrDefault();

-        public static bool operator ==(SeparatedSyntaxList<TNode> left, SeparatedSyntaxList<TNode> right);

-        public static implicit operator SeparatedSyntaxList<TNode> (SeparatedSyntaxList<SyntaxNode> nodes);

-        public static implicit operator SeparatedSyntaxList<SyntaxNode> (SeparatedSyntaxList<TNode> nodes);

-        public static bool operator !=(SeparatedSyntaxList<TNode> left, SeparatedSyntaxList<TNode> right);

-        public SeparatedSyntaxList<TNode> Remove(TNode node);

-        public SeparatedSyntaxList<TNode> RemoveAt(int index);

-        public SeparatedSyntaxList<TNode> Replace(TNode nodeInList, TNode newNode);

-        public SeparatedSyntaxList<TNode> ReplaceRange(TNode nodeInList, IEnumerable<TNode> newNodes);

-        public SeparatedSyntaxList<TNode> ReplaceSeparator(SyntaxToken separatorToken, SyntaxToken newSeparator);

-        IEnumerator<TNode> System.Collections.Generic.IEnumerable<TNode>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public string ToFullString();

-        public override string ToString();

-        public struct Enumerator {
 {
-            public TNode Current { get; }

-            public override bool Equals(object obj);

-            public override int GetHashCode();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public enum SourceCodeKind {
 {
-        Interactive = 2,

-        Regular = 0,

-        Script = 1,

-    }
-    public class SourceFileResolver : SourceReferenceResolver, IEquatable<SourceFileResolver> {
 {
-        public SourceFileResolver(IEnumerable<string> searchPaths, string baseDirectory);

-        public SourceFileResolver(ImmutableArray<string> searchPaths, string baseDirectory);

-        public SourceFileResolver(ImmutableArray<string> searchPaths, string baseDirectory, ImmutableArray<KeyValuePair<string, string>> pathMap);

-        public string BaseDirectory { get; }

-        public static SourceFileResolver Default { get; }

-        public ImmutableArray<KeyValuePair<string, string>> PathMap { get; }

-        public ImmutableArray<string> SearchPaths { get; }

-        public bool Equals(SourceFileResolver other);

-        public override bool Equals(object obj);

-        protected virtual bool FileExists(string resolvedPath);

-        public override int GetHashCode();

-        public override string NormalizePath(string path, string baseFilePath);

-        public override Stream OpenRead(string resolvedPath);

-        public override string ResolveReference(string path, string baseFilePath);

-    }
-    public abstract class SourceReferenceResolver {
 {
-        protected SourceReferenceResolver();

-        public abstract override bool Equals(object other);

-        public abstract override int GetHashCode();

-        public abstract string NormalizePath(string path, string baseFilePath);

-        public abstract Stream OpenRead(string resolvedPath);

-        public virtual SourceText ReadText(string resolvedPath);

-        public abstract string ResolveReference(string path, string baseFilePath);

-    }
-    public enum SpecialType : sbyte {
 {
-        Count = (sbyte)43,

-        None = (sbyte)0,

-        System_ArgIterator = (sbyte)37,

-        System_Array = (sbyte)23,

-        System_AsyncCallback = (sbyte)43,

-        System_Boolean = (sbyte)7,

-        System_Byte = (sbyte)10,

-        System_Char = (sbyte)8,

-        System_Collections_Generic_ICollection_T = (sbyte)27,

-        System_Collections_Generic_IEnumerable_T = (sbyte)25,

-        System_Collections_Generic_IEnumerator_T = (sbyte)29,

-        System_Collections_Generic_IList_T = (sbyte)26,

-        System_Collections_Generic_IReadOnlyCollection_T = (sbyte)31,

-        System_Collections_Generic_IReadOnlyList_T = (sbyte)30,

-        System_Collections_IEnumerable = (sbyte)24,

-        System_Collections_IEnumerator = (sbyte)28,

-        System_DateTime = (sbyte)33,

-        System_Decimal = (sbyte)17,

-        System_Delegate = (sbyte)4,

-        System_Double = (sbyte)19,

-        System_Enum = (sbyte)2,

-        System_IAsyncResult = (sbyte)42,

-        System_IDisposable = (sbyte)35,

-        System_Int16 = (sbyte)11,

-        System_Int32 = (sbyte)13,

-        System_Int64 = (sbyte)15,

-        System_IntPtr = (sbyte)21,

-        System_MulticastDelegate = (sbyte)3,

-        System_Nullable_T = (sbyte)32,

-        System_Object = (sbyte)1,

-        System_RuntimeArgumentHandle = (sbyte)38,

-        System_RuntimeFieldHandle = (sbyte)39,

-        System_RuntimeMethodHandle = (sbyte)40,

-        System_RuntimeTypeHandle = (sbyte)41,

-        System_Runtime_CompilerServices_IsVolatile = (sbyte)34,

-        System_SByte = (sbyte)9,

-        System_Single = (sbyte)18,

-        System_String = (sbyte)20,

-        System_TypedReference = (sbyte)36,

-        System_UInt16 = (sbyte)12,

-        System_UInt32 = (sbyte)14,

-        System_UInt64 = (sbyte)16,

-        System_UIntPtr = (sbyte)22,

-        System_ValueType = (sbyte)5,

-        System_Void = (sbyte)6,

-    }
-    public enum SpeculativeBindingOption {
 {
-        BindAsExpression = 0,

-        BindAsTypeOrNamespace = 1,

-    }
-    public abstract class StrongNameProvider {
 {
-        protected StrongNameProvider();

-        public abstract override bool Equals(object other);

-        public abstract override int GetHashCode();

-    }
-    public struct SubsystemVersion : IEquatable<SubsystemVersion> {
 {
-        public bool IsValid { get; }

-        public int Major { get; }

-        public int Minor { get; }

-        public static SubsystemVersion None { get; }

-        public static SubsystemVersion Windows2000 { get; }

-        public static SubsystemVersion Windows7 { get; }

-        public static SubsystemVersion Windows8 { get; }

-        public static SubsystemVersion WindowsVista { get; }

-        public static SubsystemVersion WindowsXP { get; }

-        public static SubsystemVersion Create(int major, int minor);

-        public bool Equals(SubsystemVersion other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        public static bool TryParse(string str, out SubsystemVersion version);

-    }
-    public enum SymbolDisplayDelegateStyle {
 {
-        NameAndParameters = 1,

-        NameAndSignature = 2,

-        NameOnly = 0,

-    }
-    public enum SymbolDisplayExtensionMethodStyle {
 {
-        Default = 0,

-        InstanceMethod = 1,

-        StaticMethod = 2,

-    }
-    public static class SymbolDisplayExtensions {
 {
-        public static string ToDisplayString(this ImmutableArray<SymbolDisplayPart> parts);

-    }
-    public class SymbolDisplayFormat {
 {
-        public SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle globalNamespaceStyle = SymbolDisplayGlobalNamespaceStyle.Omitted, SymbolDisplayTypeQualificationStyle typeQualificationStyle = SymbolDisplayTypeQualificationStyle.NameOnly, SymbolDisplayGenericsOptions genericsOptions = SymbolDisplayGenericsOptions.None, SymbolDisplayMemberOptions memberOptions = SymbolDisplayMemberOptions.None, SymbolDisplayDelegateStyle delegateStyle = SymbolDisplayDelegateStyle.NameOnly, SymbolDisplayExtensionMethodStyle extensionMethodStyle = SymbolDisplayExtensionMethodStyle.Default, SymbolDisplayParameterOptions parameterOptions = SymbolDisplayParameterOptions.None, SymbolDisplayPropertyStyle propertyStyle = SymbolDisplayPropertyStyle.NameOnly, SymbolDisplayLocalOptions localOptions = SymbolDisplayLocalOptions.None, SymbolDisplayKindOptions kindOptions = SymbolDisplayKindOptions.None, SymbolDisplayMiscellaneousOptions miscellaneousOptions = SymbolDisplayMiscellaneousOptions.None);

-        public static SymbolDisplayFormat CSharpErrorMessageFormat { get; }

-        public static SymbolDisplayFormat CSharpShortErrorMessageFormat { get; }

-        public SymbolDisplayDelegateStyle DelegateStyle { get; }

-        public SymbolDisplayExtensionMethodStyle ExtensionMethodStyle { get; }

-        public static SymbolDisplayFormat FullyQualifiedFormat { get; }

-        public SymbolDisplayGenericsOptions GenericsOptions { get; }

-        public SymbolDisplayGlobalNamespaceStyle GlobalNamespaceStyle { get; }

-        public SymbolDisplayKindOptions KindOptions { get; }

-        public SymbolDisplayLocalOptions LocalOptions { get; }

-        public SymbolDisplayMemberOptions MemberOptions { get; }

-        public static SymbolDisplayFormat MinimallyQualifiedFormat { get; }

-        public SymbolDisplayMiscellaneousOptions MiscellaneousOptions { get; }

-        public SymbolDisplayParameterOptions ParameterOptions { get; }

-        public SymbolDisplayPropertyStyle PropertyStyle { get; }

-        public SymbolDisplayTypeQualificationStyle TypeQualificationStyle { get; }

-        public static SymbolDisplayFormat VisualBasicErrorMessageFormat { get; }

-        public static SymbolDisplayFormat VisualBasicShortErrorMessageFormat { get; }

-        public SymbolDisplayFormat AddGenericsOptions(SymbolDisplayGenericsOptions options);

-        public SymbolDisplayFormat AddKindOptions(SymbolDisplayKindOptions options);

-        public SymbolDisplayFormat AddLocalOptions(SymbolDisplayLocalOptions options);

-        public SymbolDisplayFormat AddMemberOptions(SymbolDisplayMemberOptions options);

-        public SymbolDisplayFormat AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions options);

-        public SymbolDisplayFormat AddParameterOptions(SymbolDisplayParameterOptions options);

-        public SymbolDisplayFormat RemoveGenericsOptions(SymbolDisplayGenericsOptions options);

-        public SymbolDisplayFormat RemoveKindOptions(SymbolDisplayKindOptions options);

-        public SymbolDisplayFormat RemoveLocalOptions(SymbolDisplayLocalOptions options);

-        public SymbolDisplayFormat RemoveMemberOptions(SymbolDisplayMemberOptions options);

-        public SymbolDisplayFormat RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions options);

-        public SymbolDisplayFormat RemoveParameterOptions(SymbolDisplayParameterOptions options);

-        public SymbolDisplayFormat WithGenericsOptions(SymbolDisplayGenericsOptions options);

-        public SymbolDisplayFormat WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle style);

-        public SymbolDisplayFormat WithKindOptions(SymbolDisplayKindOptions options);

-        public SymbolDisplayFormat WithLocalOptions(SymbolDisplayLocalOptions options);

-        public SymbolDisplayFormat WithMemberOptions(SymbolDisplayMemberOptions options);

-        public SymbolDisplayFormat WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions options);

-        public SymbolDisplayFormat WithParameterOptions(SymbolDisplayParameterOptions options);

-    }
-    public enum SymbolDisplayGenericsOptions {
 {
-        IncludeTypeConstraints = 2,

-        IncludeTypeParameters = 1,

-        IncludeVariance = 4,

-        None = 0,

-    }
-    public enum SymbolDisplayGlobalNamespaceStyle {
 {
-        Included = 2,

-        Omitted = 0,

-        OmittedAsContaining = 1,

-    }
-    public enum SymbolDisplayKindOptions {
 {
-        IncludeMemberKeyword = 4,

-        IncludeNamespaceKeyword = 1,

-        IncludeTypeKeyword = 2,

-        None = 0,

-    }
-    public enum SymbolDisplayLocalOptions {
 {
-        IncludeConstantValue = 2,

-        IncludeRef = 4,

-        IncludeType = 1,

-        None = 0,

-    }
-    public enum SymbolDisplayMemberOptions {
 {
-        IncludeAccessibility = 4,

-        IncludeConstantValue = 64,

-        IncludeContainingType = 32,

-        IncludeExplicitInterface = 8,

-        IncludeModifiers = 2,

-        IncludeParameters = 16,

-        IncludeRef = 128,

-        IncludeType = 1,

-        None = 0,

-    }
-    public enum SymbolDisplayMiscellaneousOptions {
 {
-        EscapeKeywordIdentifiers = 2,

-        ExpandNullable = 32,

-        None = 0,

-        RemoveAttributeSuffix = 16,

-        UseAsterisksInMultiDimensionalArrays = 4,

-        UseErrorTypeSymbolName = 8,

-        UseSpecialTypes = 1,

-    }
-    public enum SymbolDisplayParameterOptions {
 {
-        IncludeDefaultValue = 16,

-        IncludeExtensionThis = 1,

-        IncludeName = 8,

-        IncludeOptionalBrackets = 32,

-        IncludeParamsRefOut = 2,

-        IncludeType = 4,

-        None = 0,

-    }
-    public struct SymbolDisplayPart {
 {
-        public SymbolDisplayPart(SymbolDisplayPartKind kind, ISymbol symbol, string text);

-        public SymbolDisplayPartKind Kind { get; }

-        public ISymbol Symbol { get; }

-        public override string ToString();

-    }
-    public enum SymbolDisplayPartKind {
 {
-        AliasName = 0,

-        AnonymousTypeIndicator = 24,

-        AssemblyName = 1,

-        ClassName = 2,

-        DelegateName = 3,

-        EnumName = 4,

-        ErrorTypeName = 5,

-        EventName = 6,

-        FieldName = 7,

-        InterfaceName = 8,

-        Keyword = 9,

-        LabelName = 10,

-        LineBreak = 11,

-        LocalName = 14,

-        MethodName = 15,

-        ModuleName = 16,

-        NamespaceName = 17,

-        NumericLiteral = 12,

-        Operator = 18,

-        ParameterName = 19,

-        PropertyName = 20,

-        Punctuation = 21,

-        RangeVariableName = 27,

-        Space = 22,

-        StringLiteral = 13,

-        StructName = 23,

-        Text = 25,

-        TypeParameterName = 26,

-    }
-    public enum SymbolDisplayPropertyStyle {
 {
-        NameOnly = 0,

-        ShowReadWriteDescriptor = 1,

-    }
-    public enum SymbolDisplayTypeQualificationStyle {
 {
-        NameAndContainingTypes = 1,

-        NameAndContainingTypesAndNamespaces = 2,

-        NameOnly = 0,

-    }
-    public enum SymbolFilter {
 {
-        All = 7,

-        Member = 4,

-        Namespace = 1,

-        None = 0,

-        Type = 2,

-        TypeAndMember = 6,

-    }
-    public struct SymbolInfo : IEquatable<SymbolInfo> {
 {
-        public CandidateReason CandidateReason { get; }

-        public ImmutableArray<ISymbol> CandidateSymbols { get; }

-        public ISymbol Symbol { get; }

-        public bool Equals(SymbolInfo other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public enum SymbolKind {
 {
-        Alias = 0,

-        ArrayType = 1,

-        Assembly = 2,

-        Discard = 19,

-        DynamicType = 3,

-        ErrorType = 4,

-        Event = 5,

-        Field = 6,

-        Label = 7,

-        Local = 8,

-        Method = 9,

-        NamedType = 11,

-        Namespace = 12,

-        NetModule = 10,

-        Parameter = 13,

-        PointerType = 14,

-        Preprocessing = 18,

-        Property = 15,

-        RangeVariable = 16,

-        TypeParameter = 17,

-    }
-    public abstract class SymbolVisitor {
 {
-        protected SymbolVisitor();

-        public virtual void DefaultVisit(ISymbol symbol);

-        public virtual void Visit(ISymbol symbol);

-        public virtual void VisitAlias(IAliasSymbol symbol);

-        public virtual void VisitArrayType(IArrayTypeSymbol symbol);

-        public virtual void VisitAssembly(IAssemblySymbol symbol);

-        public virtual void VisitDiscard(IDiscardSymbol symbol);

-        public virtual void VisitDynamicType(IDynamicTypeSymbol symbol);

-        public virtual void VisitEvent(IEventSymbol symbol);

-        public virtual void VisitField(IFieldSymbol symbol);

-        public virtual void VisitLabel(ILabelSymbol symbol);

-        public virtual void VisitLocal(ILocalSymbol symbol);

-        public virtual void VisitMethod(IMethodSymbol symbol);

-        public virtual void VisitModule(IModuleSymbol symbol);

-        public virtual void VisitNamedType(INamedTypeSymbol symbol);

-        public virtual void VisitNamespace(INamespaceSymbol symbol);

-        public virtual void VisitParameter(IParameterSymbol symbol);

-        public virtual void VisitPointerType(IPointerTypeSymbol symbol);

-        public virtual void VisitProperty(IPropertySymbol symbol);

-        public virtual void VisitRangeVariable(IRangeVariableSymbol symbol);

-        public virtual void VisitTypeParameter(ITypeParameterSymbol symbol);

-    }
-    public abstract class SymbolVisitor<TResult> {
 {
-        protected SymbolVisitor();

-        public virtual TResult DefaultVisit(ISymbol symbol);

-        public virtual TResult Visit(ISymbol symbol);

-        public virtual TResult VisitAlias(IAliasSymbol symbol);

-        public virtual TResult VisitArrayType(IArrayTypeSymbol symbol);

-        public virtual TResult VisitAssembly(IAssemblySymbol symbol);

-        public virtual TResult VisitDiscard(IDiscardSymbol symbol);

-        public virtual TResult VisitDynamicType(IDynamicTypeSymbol symbol);

-        public virtual TResult VisitEvent(IEventSymbol symbol);

-        public virtual TResult VisitField(IFieldSymbol symbol);

-        public virtual TResult VisitLabel(ILabelSymbol symbol);

-        public virtual TResult VisitLocal(ILocalSymbol symbol);

-        public virtual TResult VisitMethod(IMethodSymbol symbol);

-        public virtual TResult VisitModule(IModuleSymbol symbol);

-        public virtual TResult VisitNamedType(INamedTypeSymbol symbol);

-        public virtual TResult VisitNamespace(INamespaceSymbol symbol);

-        public virtual TResult VisitParameter(IParameterSymbol symbol);

-        public virtual TResult VisitPointerType(IPointerTypeSymbol symbol);

-        public virtual TResult VisitProperty(IPropertySymbol symbol);

-        public virtual TResult VisitRangeVariable(IRangeVariableSymbol symbol);

-        public virtual TResult VisitTypeParameter(ITypeParameterSymbol symbol);

-    }
-    public sealed class SyntaxAnnotation : IEquatable<SyntaxAnnotation>, IObjectWritable {
 {
-        public SyntaxAnnotation();

-        public SyntaxAnnotation(string kind);

-        public SyntaxAnnotation(string kind, string data);

-        public string Data { get; }

-        public static SyntaxAnnotation ElasticAnnotation { get; }

-        public string Kind { get; }

-        public bool Equals(SyntaxAnnotation other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(SyntaxAnnotation left, SyntaxAnnotation right);

-        public static bool operator !=(SyntaxAnnotation left, SyntaxAnnotation right);

-    }
-    public struct SyntaxList<TNode> : IEnumerable, IEnumerable<TNode>, IEquatable<SyntaxList<TNode>>, IReadOnlyCollection<TNode>, IReadOnlyList<TNode> where TNode : SyntaxNode {
 {
-        public SyntaxList(IEnumerable<TNode> nodes);

-        public SyntaxList(TNode node);

-        public int Count { get; }

-        public TextSpan FullSpan { get; }

-        public TextSpan Span { get; }

-        public TNode this[int index] { get; }

-        public SyntaxList<TNode> Add(TNode node);

-        public SyntaxList<TNode> AddRange(IEnumerable<TNode> nodes);

-        public bool Any();

-        public bool Equals(SyntaxList<TNode> other);

-        public override bool Equals(object obj);

-        public TNode First();

-        public TNode FirstOrDefault();

-        public SyntaxList<TNode>.Enumerator GetEnumerator();

-        public override int GetHashCode();

-        public int IndexOf(Func<TNode, bool> predicate);

-        public int IndexOf(TNode node);

-        public SyntaxList<TNode> Insert(int index, TNode node);

-        public SyntaxList<TNode> InsertRange(int index, IEnumerable<TNode> nodes);

-        public TNode Last();

-        public int LastIndexOf(Func<TNode, bool> predicate);

-        public int LastIndexOf(TNode node);

-        public TNode LastOrDefault();

-        public static bool operator ==(SyntaxList<TNode> left, SyntaxList<TNode> right);

-        public static implicit operator SyntaxList<TNode> (SyntaxList<SyntaxNode> nodes);

-        public static implicit operator SyntaxList<SyntaxNode> (SyntaxList<TNode> nodes);

-        public static bool operator !=(SyntaxList<TNode> left, SyntaxList<TNode> right);

-        public SyntaxList<TNode> Remove(TNode node);

-        public SyntaxList<TNode> RemoveAt(int index);

-        public SyntaxList<TNode> Replace(TNode nodeInList, TNode newNode);

-        public SyntaxList<TNode> ReplaceRange(TNode nodeInList, IEnumerable<TNode> newNodes);

-        IEnumerator<TNode> System.Collections.Generic.IEnumerable<TNode>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public string ToFullString();

-        public override string ToString();

-        public struct Enumerator {
 {
-            public TNode Current { get; }

-            public override bool Equals(object obj);

-            public override int GetHashCode();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public abstract class SyntaxNode {
 {
-        public bool ContainsAnnotations { get; }

-        public bool ContainsDiagnostics { get; }

-        public bool ContainsDirectives { get; }

-        public bool ContainsSkippedText { get; }

-        public TextSpan FullSpan { get; }

-        public bool HasLeadingTrivia { get; }

-        public bool HasStructuredTrivia { get; }

-        public bool HasTrailingTrivia { get; }

-        public bool IsMissing { get; }

-        public bool IsStructuredTrivia { get; }

-        protected string KindText { get; }

-        public abstract string Language { get; }

-        public SyntaxNode Parent { get; }

-        public virtual SyntaxTrivia ParentTrivia { get; }

-        public int RawKind { get; }

-        public TextSpan Span { get; }

-        public int SpanStart { get; }

-        public SyntaxTree SyntaxTree { get; }

-        protected abstract SyntaxTree SyntaxTreeCore { get; }

-        public IEnumerable<SyntaxNode> Ancestors(bool ascendOutOfTrivia = true);

-        public IEnumerable<SyntaxNode> AncestorsAndSelf(bool ascendOutOfTrivia = true);

-        public IEnumerable<SyntaxNode> ChildNodes();

-        public ChildSyntaxList ChildNodesAndTokens();

-        public virtual SyntaxNodeOrToken ChildThatContainsPosition(int position);

-        public IEnumerable<SyntaxToken> ChildTokens();

-        public bool Contains(SyntaxNode node);

-        public T CopyAnnotationsTo<T>(T node) where T : SyntaxNode;

-        public IEnumerable<SyntaxNode> DescendantNodes(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxNode> DescendantNodes(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxNode> DescendantNodesAndSelf(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxNode> DescendantNodesAndSelf(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokens(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokens(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensAndSelf(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensAndSelf(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxToken> DescendantTokens(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxToken> DescendantTokens(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxTrivia> DescendantTrivia(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        public IEnumerable<SyntaxTrivia> DescendantTrivia(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false);

-        protected virtual bool EquivalentToCore(SyntaxNode other);

-        public SyntaxNode FindNode(TextSpan span, bool findInsideTrivia = false, bool getInnermostNodeForTie = false);

-        public SyntaxToken FindToken(int position, bool findInsideTrivia = false);

-        protected virtual SyntaxToken FindTokenCore(int position, bool findInsideTrivia);

-        protected virtual SyntaxToken FindTokenCore(int position, Func<SyntaxTrivia, bool> stepInto);

-        public SyntaxTrivia FindTrivia(int position, bool findInsideTrivia = false);

-        public SyntaxTrivia FindTrivia(int position, Func<SyntaxTrivia, bool> stepInto);

-        protected virtual SyntaxTrivia FindTriviaCore(int position, bool findInsideTrivia);

-        public TNode FirstAncestorOrSelf<TNode>(Func<TNode, bool> predicate = null, bool ascendOutOfTrivia = true) where TNode : SyntaxNode;

-        public IEnumerable<SyntaxNode> GetAnnotatedNodes(SyntaxAnnotation syntaxAnnotation);

-        public IEnumerable<SyntaxNode> GetAnnotatedNodes(string annotationKind);

-        public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens(SyntaxAnnotation annotation);

-        public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens(string annotationKind);

-        public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens(params string[] annotationKinds);

-        public IEnumerable<SyntaxToken> GetAnnotatedTokens(SyntaxAnnotation syntaxAnnotation);

-        public IEnumerable<SyntaxToken> GetAnnotatedTokens(string annotationKind);

-        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(SyntaxAnnotation annotation);

-        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(string annotationKind);

-        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(params string[] annotationKinds);

-        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds);

-        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind);

-        public IEnumerable<Diagnostic> GetDiagnostics();

-        public SyntaxToken GetFirstToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false);

-        public SyntaxToken GetLastToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false);

-        public SyntaxTriviaList GetLeadingTrivia();

-        public Location GetLocation();

-        protected T GetRed<T>(ref T field, int slot) where T : SyntaxNode;

-        protected T GetRedAtZero<T>(ref T field) where T : SyntaxNode;

-        public SyntaxReference GetReference();

-        public SourceText GetText(Encoding encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1);

-        public SyntaxTriviaList GetTrailingTrivia();

-        public bool HasAnnotation(SyntaxAnnotation annotation);

-        public bool HasAnnotations(IEnumerable<string> annotationKinds);

-        public bool HasAnnotations(string annotationKind);

-        protected internal abstract SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore);

-        protected internal abstract SyntaxNode InsertTokensInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens, bool insertBefore);

-        protected internal abstract SyntaxNode InsertTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore);

-        public bool IsEquivalentTo(SyntaxNode other);

-        public bool IsEquivalentTo(SyntaxNode node, bool topLevel = false);

-        protected abstract bool IsEquivalentToCore(SyntaxNode node, bool topLevel = false);

-        public bool IsPartOfStructuredTrivia();

-        protected internal abstract SyntaxNode NormalizeWhitespaceCore(string indentation, string eol, bool elasticTrivia);

-        protected internal abstract SyntaxNode RemoveNodesCore(IEnumerable<SyntaxNode> nodes, SyntaxRemoveOptions options);

-        protected internal abstract SyntaxNode ReplaceCore<TNode>(IEnumerable<TNode> nodes = null, Func<TNode, TNode, SyntaxNode> computeReplacementNode = null, IEnumerable<SyntaxToken> tokens = null, Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null, IEnumerable<SyntaxTrivia> trivia = null, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null) where TNode : SyntaxNode;

-        protected internal abstract SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes);

-        protected internal abstract SyntaxNode ReplaceTokenInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens);

-        protected internal abstract SyntaxNode ReplaceTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia);

-        public virtual void SerializeTo(Stream stream, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual string ToFullString();

-        public override string ToString();

-        public virtual void WriteTo(TextWriter writer);

-    }
-    public static class SyntaxNodeExtensions {
 {
-        public static TNode GetCurrentNode<TNode>(this SyntaxNode root, TNode node) where TNode : SyntaxNode;

-        public static IEnumerable<TNode> GetCurrentNodes<TNode>(this SyntaxNode root, IEnumerable<TNode> nodes) where TNode : SyntaxNode;

-        public static IEnumerable<TNode> GetCurrentNodes<TNode>(this SyntaxNode root, TNode node) where TNode : SyntaxNode;

-        public static TRoot InsertNodesAfter<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes) where TRoot : SyntaxNode;

-        public static TRoot InsertNodesBefore<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes) where TRoot : SyntaxNode;

-        public static TRoot InsertTokensAfter<TRoot>(this TRoot root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens) where TRoot : SyntaxNode;

-        public static TRoot InsertTokensBefore<TRoot>(this TRoot root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens) where TRoot : SyntaxNode;

-        public static TRoot InsertTriviaAfter<TRoot>(this TRoot root, SyntaxTrivia trivia, IEnumerable<SyntaxTrivia> newTrivia) where TRoot : SyntaxNode;

-        public static TRoot InsertTriviaBefore<TRoot>(this TRoot root, SyntaxTrivia trivia, IEnumerable<SyntaxTrivia> newTrivia) where TRoot : SyntaxNode;

-        public static TNode NormalizeWhitespace<TNode>(this TNode node, string indentation, bool elasticTrivia) where TNode : SyntaxNode;

-        public static TNode NormalizeWhitespace<TNode>(this TNode node, string indentation = "    ", string eol = "\r\n", bool elasticTrivia = false) where TNode : SyntaxNode;

-        public static TRoot RemoveNode<TRoot>(this TRoot root, SyntaxNode node, SyntaxRemoveOptions options) where TRoot : SyntaxNode;

-        public static TRoot RemoveNodes<TRoot>(this TRoot root, IEnumerable<SyntaxNode> nodes, SyntaxRemoveOptions options) where TRoot : SyntaxNode;

-        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, SyntaxNode newNode) where TRoot : SyntaxNode;

-        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, IEnumerable<SyntaxNode> newNodes) where TRoot : SyntaxNode;

-        public static TRoot ReplaceNodes<TRoot, TNode>(this TRoot root, IEnumerable<TNode> nodes, Func<TNode, TNode, SyntaxNode> computeReplacementNode) where TRoot : SyntaxNode where TNode : SyntaxNode;

-        public static TRoot ReplaceSyntax<TRoot>(this TRoot root, IEnumerable<SyntaxNode> nodes, Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode, IEnumerable<SyntaxToken> tokens, Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken, IEnumerable<SyntaxTrivia> trivia, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia) where TRoot : SyntaxNode;

-        public static TRoot ReplaceToken<TRoot>(this TRoot root, SyntaxToken oldToken, SyntaxToken newToken) where TRoot : SyntaxNode;

-        public static TRoot ReplaceToken<TRoot>(this TRoot root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens) where TRoot : SyntaxNode;

-        public static TRoot ReplaceTokens<TRoot>(this TRoot root, IEnumerable<SyntaxToken> tokens, Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken) where TRoot : SyntaxNode;

-        public static TRoot ReplaceTrivia<TRoot>(this TRoot root, SyntaxTrivia trivia, SyntaxTrivia newTrivia) where TRoot : SyntaxNode;

-        public static TRoot ReplaceTrivia<TRoot>(this TRoot root, SyntaxTrivia oldTrivia, IEnumerable<SyntaxTrivia> newTrivia) where TRoot : SyntaxNode;

-        public static TRoot ReplaceTrivia<TRoot>(this TRoot root, IEnumerable<SyntaxTrivia> trivia, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia) where TRoot : SyntaxNode;

-        public static TRoot TrackNodes<TRoot>(this TRoot root, params SyntaxNode[] nodes) where TRoot : SyntaxNode;

-        public static TRoot TrackNodes<TRoot>(this TRoot root, IEnumerable<SyntaxNode> nodes) where TRoot : SyntaxNode;

-        public static TSyntax WithLeadingTrivia<TSyntax>(this TSyntax node, SyntaxTriviaList trivia) where TSyntax : SyntaxNode;

-        public static TSyntax WithLeadingTrivia<TSyntax>(this TSyntax node, params SyntaxTrivia[] trivia) where TSyntax : SyntaxNode;

-        public static TSyntax WithLeadingTrivia<TSyntax>(this TSyntax node, IEnumerable<SyntaxTrivia> trivia) where TSyntax : SyntaxNode;

-        public static TSyntax WithoutLeadingTrivia<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode;

-        public static TSyntax WithoutTrailingTrivia<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode;

-        public static SyntaxToken WithoutTrivia(this SyntaxToken token);

-        public static TSyntax WithoutTrivia<TSyntax>(this TSyntax syntax) where TSyntax : SyntaxNode;

-        public static TSyntax WithTrailingTrivia<TSyntax>(this TSyntax node, SyntaxTriviaList trivia) where TSyntax : SyntaxNode;

-        public static TSyntax WithTrailingTrivia<TSyntax>(this TSyntax node, params SyntaxTrivia[] trivia) where TSyntax : SyntaxNode;

-        public static TSyntax WithTrailingTrivia<TSyntax>(this TSyntax node, IEnumerable<SyntaxTrivia> trivia) where TSyntax : SyntaxNode;

-        public static TSyntax WithTriviaFrom<TSyntax>(this TSyntax syntax, SyntaxNode node) where TSyntax : SyntaxNode;

-    }
-    public struct SyntaxNodeOrToken : IEquatable<SyntaxNodeOrToken> {
 {
-        public bool ContainsAnnotations { get; }

-        public bool ContainsDiagnostics { get; }

-        public bool ContainsDirectives { get; }

-        public TextSpan FullSpan { get; }

-        public bool HasLeadingTrivia { get; }

-        public bool HasTrailingTrivia { get; }

-        public bool IsMissing { get; }

-        public bool IsNode { get; }

-        public bool IsToken { get; }

-        public string Language { get; }

-        public SyntaxNode Parent { get; }

-        public int RawKind { get; }

-        public TextSpan Span { get; }

-        public int SpanStart { get; }

-        public SyntaxTree SyntaxTree { get; }

-        public SyntaxNode AsNode();

-        public SyntaxToken AsToken();

-        public ChildSyntaxList ChildNodesAndTokens();

-        public bool Equals(SyntaxNodeOrToken other);

-        public override bool Equals(object obj);

-        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds);

-        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind);

-        public IEnumerable<Diagnostic> GetDiagnostics();

-        public static int GetFirstChildIndexSpanningPosition(SyntaxNode node, int position);

-        public override int GetHashCode();

-        public SyntaxTriviaList GetLeadingTrivia();

-        public Location GetLocation();

-        public SyntaxNodeOrToken GetNextSibling();

-        public SyntaxNodeOrToken GetPreviousSibling();

-        public SyntaxTriviaList GetTrailingTrivia();

-        public bool HasAnnotation(SyntaxAnnotation annotation);

-        public bool HasAnnotations(IEnumerable<string> annotationKinds);

-        public bool HasAnnotations(string annotationKind);

-        public bool IsEquivalentTo(SyntaxNodeOrToken other);

-        public static bool operator ==(SyntaxNodeOrToken left, SyntaxNodeOrToken right);

-        public static explicit operator SyntaxToken (SyntaxNodeOrToken nodeOrToken);

-        public static explicit operator SyntaxNode (SyntaxNodeOrToken nodeOrToken);

-        public static implicit operator SyntaxNodeOrToken (SyntaxNode node);

-        public static implicit operator SyntaxNodeOrToken (SyntaxToken token);

-        public static bool operator !=(SyntaxNodeOrToken left, SyntaxNodeOrToken right);

-        public string ToFullString();

-        public override string ToString();

-        public SyntaxNodeOrToken WithAdditionalAnnotations(params SyntaxAnnotation[] annotations);

-        public SyntaxNodeOrToken WithAdditionalAnnotations(IEnumerable<SyntaxAnnotation> annotations);

-        public SyntaxNodeOrToken WithLeadingTrivia(params SyntaxTrivia[] trivia);

-        public SyntaxNodeOrToken WithLeadingTrivia(IEnumerable<SyntaxTrivia> trivia);

-        public SyntaxNodeOrToken WithoutAnnotations(params SyntaxAnnotation[] annotations);

-        public SyntaxNodeOrToken WithoutAnnotations(IEnumerable<SyntaxAnnotation> annotations);

-        public SyntaxNodeOrToken WithoutAnnotations(string annotationKind);

-        public SyntaxNodeOrToken WithTrailingTrivia(params SyntaxTrivia[] trivia);

-        public SyntaxNodeOrToken WithTrailingTrivia(IEnumerable<SyntaxTrivia> trivia);

-        public void WriteTo(TextWriter writer);

-    }
-    public struct SyntaxNodeOrTokenList : IEnumerable, IEnumerable<SyntaxNodeOrToken>, IEquatable<SyntaxNodeOrTokenList>, IReadOnlyCollection<SyntaxNodeOrToken> {
 {
-        public SyntaxNodeOrTokenList(params SyntaxNodeOrToken[] nodesAndTokens);

-        public SyntaxNodeOrTokenList(IEnumerable<SyntaxNodeOrToken> nodesAndTokens);

-        public int Count { get; }

-        public TextSpan FullSpan { get; }

-        public TextSpan Span { get; }

-        public SyntaxNodeOrToken this[int index] { get; }

-        public SyntaxNodeOrTokenList Add(SyntaxNodeOrToken nodeOrToken);

-        public SyntaxNodeOrTokenList AddRange(IEnumerable<SyntaxNodeOrToken> nodesOrTokens);

-        public bool Any();

-        public bool Equals(SyntaxNodeOrTokenList other);

-        public override bool Equals(object obj);

-        public SyntaxNodeOrToken First();

-        public SyntaxNodeOrToken FirstOrDefault();

-        public SyntaxNodeOrTokenList.Enumerator GetEnumerator();

-        public override int GetHashCode();

-        public int IndexOf(SyntaxNodeOrToken nodeOrToken);

-        public SyntaxNodeOrTokenList Insert(int index, SyntaxNodeOrToken nodeOrToken);

-        public SyntaxNodeOrTokenList InsertRange(int index, IEnumerable<SyntaxNodeOrToken> nodesAndTokens);

-        public SyntaxNodeOrToken Last();

-        public SyntaxNodeOrToken LastOrDefault();

-        public static bool operator ==(SyntaxNodeOrTokenList left, SyntaxNodeOrTokenList right);

-        public static bool operator !=(SyntaxNodeOrTokenList left, SyntaxNodeOrTokenList right);

-        public SyntaxNodeOrTokenList Remove(SyntaxNodeOrToken nodeOrTokenInList);

-        public SyntaxNodeOrTokenList RemoveAt(int index);

-        public SyntaxNodeOrTokenList Replace(SyntaxNodeOrToken nodeOrTokenInList, SyntaxNodeOrToken newNodeOrToken);

-        public SyntaxNodeOrTokenList ReplaceRange(SyntaxNodeOrToken nodeOrTokenInList, IEnumerable<SyntaxNodeOrToken> newNodesAndTokens);

-        IEnumerator<SyntaxNodeOrToken> System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxNodeOrToken>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public string ToFullString();

-        public override string ToString();

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<SyntaxNodeOrToken> {
 {
-            public SyntaxNodeOrToken Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public override bool Equals(object obj);

-            public override int GetHashCode();

-            public bool MoveNext();

-            void System.Collections.IEnumerator.Reset();

-            void System.IDisposable.Dispose();

-        }
-    }
-    public abstract class SyntaxReference {
 {
-        protected SyntaxReference();

-        public abstract TextSpan Span { get; }

-        public abstract SyntaxTree SyntaxTree { get; }

-        public abstract SyntaxNode GetSyntax(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<SyntaxNode> GetSyntaxAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public enum SyntaxRemoveOptions {
 {
-        AddElasticMarker = 32,

-        KeepDirectives = 8,

-        KeepEndOfLine = 16,

-        KeepExteriorTrivia = 3,

-        KeepLeadingTrivia = 1,

-        KeepNoTrivia = 0,

-        KeepTrailingTrivia = 2,

-        KeepUnbalancedDirectives = 4,

-    }
-    public struct SyntaxToken : IEquatable<SyntaxToken> {
 {
-        public bool ContainsAnnotations { get; }

-        public bool ContainsDiagnostics { get; }

-        public bool ContainsDirectives { get; }

-        public TextSpan FullSpan { get; }

-        public bool HasLeadingTrivia { get; }

-        public bool HasStructuredTrivia { get; }

-        public bool HasTrailingTrivia { get; }

-        public bool IsMissing { get; }

-        public string Language { get; }

-        public SyntaxTriviaList LeadingTrivia { get; }

-        public SyntaxNode Parent { get; }

-        public int RawKind { get; }

-        public TextSpan Span { get; }

-        public int SpanStart { get; }

-        public SyntaxTree SyntaxTree { get; }

-        public string Text { get; }

-        public SyntaxTriviaList TrailingTrivia { get; }

-        public object Value { get; }

-        public string ValueText { get; }

-        public SyntaxToken CopyAnnotationsTo(SyntaxToken token);

-        public bool Equals(SyntaxToken other);

-        public override bool Equals(object obj);

-        public IEnumerable<SyntaxTrivia> GetAllTrivia();

-        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds);

-        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind);

-        public IEnumerable<SyntaxAnnotation> GetAnnotations(params string[] annotationKinds);

-        public IEnumerable<Diagnostic> GetDiagnostics();

-        public override int GetHashCode();

-        public Location GetLocation();

-        public SyntaxToken GetNextToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false);

-        public SyntaxToken GetPreviousToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false);

-        public bool HasAnnotation(SyntaxAnnotation annotation);

-        public bool HasAnnotations(string annotationKind);

-        public bool HasAnnotations(params string[] annotationKinds);

-        public bool IsEquivalentTo(SyntaxToken token);

-        public bool IsPartOfStructuredTrivia();

-        public static bool operator ==(SyntaxToken left, SyntaxToken right);

-        public static bool operator !=(SyntaxToken left, SyntaxToken right);

-        public string ToFullString();

-        public override string ToString();

-        public SyntaxToken WithAdditionalAnnotations(params SyntaxAnnotation[] annotations);

-        public SyntaxToken WithAdditionalAnnotations(IEnumerable<SyntaxAnnotation> annotations);

-        public SyntaxToken WithLeadingTrivia(SyntaxTriviaList trivia);

-        public SyntaxToken WithLeadingTrivia(params SyntaxTrivia[] trivia);

-        public SyntaxToken WithLeadingTrivia(IEnumerable<SyntaxTrivia> trivia);

-        public SyntaxToken WithoutAnnotations(params SyntaxAnnotation[] annotations);

-        public SyntaxToken WithoutAnnotations(IEnumerable<SyntaxAnnotation> annotations);

-        public SyntaxToken WithoutAnnotations(string annotationKind);

-        public SyntaxToken WithTrailingTrivia(SyntaxTriviaList trivia);

-        public SyntaxToken WithTrailingTrivia(params SyntaxTrivia[] trivia);

-        public SyntaxToken WithTrailingTrivia(IEnumerable<SyntaxTrivia> trivia);

-        public SyntaxToken WithTriviaFrom(SyntaxToken token);

-        public void WriteTo(TextWriter writer);

-    }
-    public struct SyntaxTokenList : IEnumerable, IEnumerable<SyntaxToken>, IEquatable<SyntaxTokenList>, IReadOnlyCollection<SyntaxToken>, IReadOnlyList<SyntaxToken> {
 {
-        public SyntaxTokenList(SyntaxToken token);

-        public SyntaxTokenList(params SyntaxToken[] tokens);

-        public SyntaxTokenList(IEnumerable<SyntaxToken> tokens);

-        public int Count { get; }

-        public TextSpan FullSpan { get; }

-        public TextSpan Span { get; }

-        public SyntaxToken this[int index] { get; }

-        public SyntaxTokenList Add(SyntaxToken token);

-        public SyntaxTokenList AddRange(IEnumerable<SyntaxToken> tokens);

-        public bool Any();

-        public static SyntaxTokenList Create(SyntaxToken token);

-        public bool Equals(SyntaxTokenList other);

-        public override bool Equals(object obj);

-        public SyntaxToken First();

-        public SyntaxTokenList.Enumerator GetEnumerator();

-        public override int GetHashCode();

-        public int IndexOf(SyntaxToken tokenInList);

-        public SyntaxTokenList Insert(int index, SyntaxToken token);

-        public SyntaxTokenList InsertRange(int index, IEnumerable<SyntaxToken> tokens);

-        public SyntaxToken Last();

-        public static bool operator ==(SyntaxTokenList left, SyntaxTokenList right);

-        public static bool operator !=(SyntaxTokenList left, SyntaxTokenList right);

-        public SyntaxTokenList Remove(SyntaxToken tokenInList);

-        public SyntaxTokenList RemoveAt(int index);

-        public SyntaxTokenList Replace(SyntaxToken tokenInList, SyntaxToken newToken);

-        public SyntaxTokenList ReplaceRange(SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens);

-        public SyntaxTokenList.Reversed Reverse();

-        IEnumerator<SyntaxToken> System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxToken>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public string ToFullString();

-        public override string ToString();

-        public struct Enumerator {
 {
-            public SyntaxToken Current { get; }

-            public override bool Equals(object obj);

-            public override int GetHashCode();

-            public bool MoveNext();

-        }
-        public struct Reversed : IEnumerable, IEnumerable<SyntaxToken>, IEquatable<SyntaxTokenList.Reversed> {
 {
-            public Reversed(SyntaxTokenList list);

-            public bool Equals(SyntaxTokenList.Reversed other);

-            public override bool Equals(object obj);

-            public SyntaxTokenList.Reversed.Enumerator GetEnumerator();

-            public override int GetHashCode();

-            IEnumerator<SyntaxToken> System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxToken>.GetEnumerator();

-            IEnumerator System.Collections.IEnumerable.GetEnumerator();

-            public struct Enumerator {
 {
-                public Enumerator(ref SyntaxTokenList list);

-                public SyntaxToken Current { get; }

-                public override bool Equals(object obj);

-                public override int GetHashCode();

-                public bool MoveNext();

-            }
-        }
-    }
-    public abstract class SyntaxTree {
 {
-        protected SyntaxTree();

-        public abstract Encoding Encoding { get; }

-        public abstract string FilePath { get; }

-        public abstract bool HasCompilationUnitRoot { get; }

-        public abstract int Length { get; }

-        public ParseOptions Options { get; }

-        protected abstract ParseOptions OptionsCore { get; }

-        public abstract IList<TextSpan> GetChangedSpans(SyntaxTree syntaxTree);

-        public abstract IList<TextChange> GetChanges(SyntaxTree oldTree);

-        public abstract IEnumerable<Diagnostic> GetDiagnostics(SyntaxNode node);

-        public abstract IEnumerable<Diagnostic> GetDiagnostics(SyntaxNodeOrToken nodeOrToken);

-        public abstract IEnumerable<Diagnostic> GetDiagnostics(SyntaxToken token);

-        public abstract IEnumerable<Diagnostic> GetDiagnostics(SyntaxTrivia trivia);

-        public abstract IEnumerable<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

-        public abstract FileLinePositionSpan GetLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual LineVisibility GetLineVisibility(int position, CancellationToken cancellationToken = default(CancellationToken));

-        public abstract Location GetLocation(TextSpan span);

-        public abstract FileLinePositionSpan GetMappedLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken));

-        public abstract SyntaxReference GetReference(SyntaxNode node);

-        public SyntaxNode GetRoot(CancellationToken cancellationToken = default(CancellationToken));

-        public Task<SyntaxNode> GetRootAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected abstract Task<SyntaxNode> GetRootAsyncCore(CancellationToken cancellationToken);

-        protected abstract SyntaxNode GetRootCore(CancellationToken cancellationToken);

-        public abstract SourceText GetText(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<SourceText> GetTextAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public abstract bool HasHiddenRegions();

-        public abstract bool IsEquivalentTo(SyntaxTree tree, bool topLevel = false);

-        public override string ToString();

-        public bool TryGetRoot(out SyntaxNode root);

-        protected abstract bool TryGetRootCore(out SyntaxNode root);

-        public abstract bool TryGetText(out SourceText text);

-        public abstract SyntaxTree WithChangedText(SourceText newText);

-        public abstract SyntaxTree WithFilePath(string path);

-        public abstract SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options);

-    }
-    public struct SyntaxTrivia : IEquatable<SyntaxTrivia> {
 {
-        public bool ContainsDiagnostics { get; }

-        public TextSpan FullSpan { get; }

-        public bool HasStructure { get; }

-        public bool IsDirective { get; }

-        public string Language { get; }

-        public int RawKind { get; }

-        public TextSpan Span { get; }

-        public int SpanStart { get; }

-        public SyntaxTree SyntaxTree { get; }

-        public SyntaxToken Token { get; }

-        public SyntaxTrivia CopyAnnotationsTo(SyntaxTrivia trivia);

-        public bool Equals(SyntaxTrivia other);

-        public override bool Equals(object obj);

-        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind);

-        public IEnumerable<SyntaxAnnotation> GetAnnotations(params string[] annotationKinds);

-        public IEnumerable<Diagnostic> GetDiagnostics();

-        public override int GetHashCode();

-        public Location GetLocation();

-        public SyntaxNode GetStructure();

-        public bool HasAnnotation(SyntaxAnnotation annotation);

-        public bool HasAnnotations(string annotationKind);

-        public bool HasAnnotations(params string[] annotationKinds);

-        public bool IsEquivalentTo(SyntaxTrivia trivia);

-        public bool IsPartOfStructuredTrivia();

-        public static bool operator ==(SyntaxTrivia left, SyntaxTrivia right);

-        public static bool operator !=(SyntaxTrivia left, SyntaxTrivia right);

-        public string ToFullString();

-        public override string ToString();

-        public SyntaxTrivia WithAdditionalAnnotations(params SyntaxAnnotation[] annotations);

-        public SyntaxTrivia WithAdditionalAnnotations(IEnumerable<SyntaxAnnotation> annotations);

-        public SyntaxTrivia WithoutAnnotations(params SyntaxAnnotation[] annotations);

-        public SyntaxTrivia WithoutAnnotations(IEnumerable<SyntaxAnnotation> annotations);

-        public SyntaxTrivia WithoutAnnotations(string annotationKind);

-        public void WriteTo(TextWriter writer);

-    }
-    public struct SyntaxTriviaList : IEnumerable, IEnumerable<SyntaxTrivia>, IEquatable<SyntaxTriviaList>, IReadOnlyCollection<SyntaxTrivia>, IReadOnlyList<SyntaxTrivia> {
 {
-        public SyntaxTriviaList(SyntaxTrivia trivia);

-        public SyntaxTriviaList(params SyntaxTrivia[] trivias);

-        public SyntaxTriviaList(IEnumerable<SyntaxTrivia> trivias);

-        public int Count { get; }

-        public static SyntaxTriviaList Empty { get; }

-        public TextSpan FullSpan { get; }

-        public TextSpan Span { get; }

-        public SyntaxTrivia this[int index] { get; }

-        public SyntaxTriviaList Add(SyntaxTrivia trivia);

-        public SyntaxTriviaList AddRange(IEnumerable<SyntaxTrivia> trivia);

-        public bool Any();

-        public static SyntaxTriviaList Create(SyntaxTrivia trivia);

-        public SyntaxTrivia ElementAt(int index);

-        public bool Equals(SyntaxTriviaList other);

-        public override bool Equals(object obj);

-        public SyntaxTrivia First();

-        public SyntaxTriviaList.Enumerator GetEnumerator();

-        public override int GetHashCode();

-        public int IndexOf(SyntaxTrivia triviaInList);

-        public SyntaxTriviaList Insert(int index, SyntaxTrivia trivia);

-        public SyntaxTriviaList InsertRange(int index, IEnumerable<SyntaxTrivia> trivia);

-        public SyntaxTrivia Last();

-        public static bool operator ==(SyntaxTriviaList left, SyntaxTriviaList right);

-        public static bool operator !=(SyntaxTriviaList left, SyntaxTriviaList right);

-        public SyntaxTriviaList Remove(SyntaxTrivia triviaInList);

-        public SyntaxTriviaList RemoveAt(int index);

-        public SyntaxTriviaList Replace(SyntaxTrivia triviaInList, SyntaxTrivia newTrivia);

-        public SyntaxTriviaList ReplaceRange(SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia);

-        public SyntaxTriviaList.Reversed Reverse();

-        IEnumerator<SyntaxTrivia> System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxTrivia>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public string ToFullString();

-        public override string ToString();

-        public struct Enumerator {
 {
-            public SyntaxTrivia Current { get; }

-            public bool MoveNext();

-        }
-        public struct Reversed : IEnumerable, IEnumerable<SyntaxTrivia>, IEquatable<SyntaxTriviaList.Reversed> {
 {
-            public Reversed(SyntaxTriviaList list);

-            public bool Equals(SyntaxTriviaList.Reversed other);

-            public override bool Equals(object obj);

-            public SyntaxTriviaList.Reversed.Enumerator GetEnumerator();

-            public override int GetHashCode();

-            IEnumerator<SyntaxTrivia> System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxTrivia>.GetEnumerator();

-            IEnumerator System.Collections.IEnumerable.GetEnumerator();

-            public struct Enumerator {
 {
-                public Enumerator(ref SyntaxTriviaList list);

-                public SyntaxTrivia Current { get; }

-                public bool MoveNext();

-            }
-        }
-    }
-    public abstract class SyntaxWalker {
 {
-        protected SyntaxWalker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node);

-        protected SyntaxWalkerDepth Depth { get; }

-        public virtual void Visit(SyntaxNode node);

-        protected virtual void VisitToken(SyntaxToken token);

-        protected virtual void VisitTrivia(SyntaxTrivia trivia);

-    }
-    public enum SyntaxWalkerDepth {
 {
-        Node = 0,

-        StructuredTrivia = 3,

-        Token = 1,

-        Trivia = 2,

-    }
-    public struct TypedConstant : IEquatable<TypedConstant> {
 {
-        public bool IsNull { get; }

-        public TypedConstantKind Kind { get; }

-        public ITypeSymbol Type { get; }

-        public object Value { get; }

-        public ImmutableArray<TypedConstant> Values { get; }

-        public bool Equals(TypedConstant other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public enum TypedConstantKind {
 {
-        Array = 4,

-        Enum = 2,

-        Error = 0,

-        Primitive = 1,

-        Type = 3,

-    }
-    public struct TypeInfo : IEquatable<TypeInfo> {
 {
-        public ITypeSymbol ConvertedType { get; }

-        public ITypeSymbol Type { get; }

-        public bool Equals(TypeInfo other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public enum TypeKind : byte {
 {
-        Array = (byte)1,

-        Class = (byte)2,

-        Delegate = (byte)3,

-        Dynamic = (byte)4,

-        Enum = (byte)5,

-        Error = (byte)6,

-        Interface = (byte)7,

-        Module = (byte)8,

-        Pointer = (byte)9,

-        Struct = (byte)10,

-        Structure = (byte)10,

-        Submission = (byte)12,

-        TypeParameter = (byte)11,

-        Unknown = (byte)0,

-    }
-    public enum TypeParameterKind {
 {
-        Cref = 2,

-        Method = 1,

-        Type = 0,

-    }
-    public sealed class UnresolvedMetadataReference : MetadataReference {
 {
-        public override string Display { get; }

-        public string Reference { get; }

-    }
-    public enum VarianceKind : short {
 {
-        In = (short)2,

-        None = (short)0,

-        Out = (short)1,

-    }
-    public static class WellKnownDiagnosticTags {
 {
-        public const string AnalyzerException = "AnalyzerException";

-        public const string Build = "Build";

-        public const string Compiler = "Compiler";

-        public const string EditAndContinue = "EditAndContinue";

-        public const string NotConfigurable = "NotConfigurable";

-        public const string Telemetry = "Telemetry";

-        public const string Unnecessary = "Unnecessary";

-    }
-    public static class WellKnownMemberNames {
 {
-        public const string AdditionOperatorName = "op_Addition";

-        public const string BitwiseAndOperatorName = "op_BitwiseAnd";

-        public const string BitwiseOrOperatorName = "op_BitwiseOr";

-        public const string CollectionInitializerAddMethodName = "Add";

-        public const string ConcatenateOperatorName = "op_Concatenate";

-        public const string CurrentPropertyName = "Current";

-        public const string DeconstructMethodName = "Deconstruct";

-        public const string DecrementOperatorName = "op_Decrement";

-        public const string DefaultScriptClassName = "Script";

-        public const string DelegateBeginInvokeName = "BeginInvoke";

-        public const string DelegateEndInvokeName = "EndInvoke";

-        public const string DelegateInvokeName = "Invoke";

-        public const string DestructorName = "Finalize";

-        public const string DivisionOperatorName = "op_Division";

-        public const string EntryPointMethodName = "Main";

-        public const string EnumBackingFieldName = "value__";

-        public const string EqualityOperatorName = "op_Equality";

-        public const string ExclusiveOrOperatorName = "op_ExclusiveOr";

-        public const string ExplicitConversionName = "op_Explicit";

-        public const string ExponentOperatorName = "op_Exponent";

-        public const string FalseOperatorName = "op_False";

-        public const string GetAwaiter = "GetAwaiter";

-        public const string GetEnumeratorMethodName = "GetEnumerator";

-        public const string GetResult = "GetResult";

-        public const string GreaterThanOperatorName = "op_GreaterThan";

-        public const string GreaterThanOrEqualOperatorName = "op_GreaterThanOrEqual";

-        public const string ImplicitConversionName = "op_Implicit";

-        public const string IncrementOperatorName = "op_Increment";

-        public const string Indexer = "this[]";

-        public const string InequalityOperatorName = "op_Inequality";

-        public const string InstanceConstructorName = ".ctor";

-        public const string IntegerDivisionOperatorName = "op_IntegerDivision";

-        public const string IsCompleted = "IsCompleted";

-        public const string LeftShiftOperatorName = "op_LeftShift";

-        public const string LessThanOperatorName = "op_LessThan";

-        public const string LessThanOrEqualOperatorName = "op_LessThanOrEqual";

-        public const string LikeOperatorName = "op_Like";

-        public const string LogicalAndOperatorName = "op_LogicalAnd";

-        public const string LogicalNotOperatorName = "op_LogicalNot";

-        public const string LogicalOrOperatorName = "op_LogicalOr";

-        public const string ModulusOperatorName = "op_Modulus";

-        public const string MoveNextMethodName = "MoveNext";

-        public const string MultiplyOperatorName = "op_Multiply";

-        public const string ObjectEquals = "Equals";

-        public const string ObjectGetHashCode = "GetHashCode";

-        public const string ObjectToString = "ToString";

-        public const string OnCompleted = "OnCompleted";

-        public const string OnesComplementOperatorName = "op_OnesComplement";

-        public const string RightShiftOperatorName = "op_RightShift";

-        public const string StaticConstructorName = ".cctor";

-        public const string SubtractionOperatorName = "op_Subtraction";

-        public const string TrueOperatorName = "op_True";

-        public const string UnaryNegationOperatorName = "op_UnaryNegation";

-        public const string UnaryPlusOperatorName = "op_UnaryPlus";

-        public const string UnsignedLeftShiftOperatorName = "op_UnsignedLeftShift";

-        public const string UnsignedRightShiftOperatorName = "op_UnsignedRightShift";

-        public const string ValuePropertyName = "Value";

-    }
-    public class XmlFileResolver : XmlReferenceResolver {
 {
-        public XmlFileResolver(string baseDirectory);

-        public string BaseDirectory { get; }

-        public static XmlFileResolver Default { get; }

-        public override bool Equals(object obj);

-        protected virtual bool FileExists(string resolvedPath);

-        public override int GetHashCode();

-        public override Stream OpenRead(string resolvedPath);

-        public override string ResolveReference(string path, string baseFilePath);

-    }
-    public abstract class XmlReferenceResolver {
 {
-        protected XmlReferenceResolver();

-        public abstract override bool Equals(object other);

-        public abstract override int GetHashCode();

-        public abstract Stream OpenRead(string resolvedPath);

-        public abstract string ResolveReference(string path, string baseFilePath);

-    }
-}
```

