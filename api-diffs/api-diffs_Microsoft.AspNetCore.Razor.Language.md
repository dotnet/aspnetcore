# Microsoft.AspNetCore.Razor.Language

``` diff
-namespace Microsoft.AspNetCore.Razor.Language {
 {
-    public abstract class AllowedChildTagDescriptor : IEquatable<AllowedChildTagDescriptor> {
 {
-        protected AllowedChildTagDescriptor();

-        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

-        public string DisplayName { get; protected set; }

-        public bool HasErrors { get; }

-        public string Name { get; protected set; }

-        public bool Equals(AllowedChildTagDescriptor other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public abstract class AllowedChildTagDescriptorBuilder {
 {
-        protected AllowedChildTagDescriptorBuilder();

-        public abstract RazorDiagnosticCollection Diagnostics { get; }

-        public abstract string DisplayName { get; set; }

-        public abstract string Name { get; set; }

-    }
-    public class AssemblyExtension : RazorExtension {
 {
-        public AssemblyExtension(string extensionName, Assembly assembly);

-        public Assembly Assembly { get; }

-        public override string ExtensionName { get; }

-    }
-    public enum AttributeStructure {
 {
-        DoubleQuotes = 0,

-        Minimized = 3,

-        NoQuotes = 2,

-        SingleQuotes = 1,

-    }
-    public abstract class BoundAttributeDescriptor : IEquatable<BoundAttributeDescriptor> {
 {
-        protected BoundAttributeDescriptor(string kind);

-        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

-        public string DisplayName { get; protected set; }

-        public string Documentation { get; protected set; }

-        public bool HasErrors { get; }

-        public bool HasIndexer { get; protected set; }

-        public string IndexerNamePrefix { get; protected set; }

-        public string IndexerTypeName { get; protected set; }

-        public bool IsBooleanProperty { get; protected set; }

-        public bool IsEnum { get; protected set; }

-        public bool IsIndexerBooleanProperty { get; protected set; }

-        public bool IsIndexerStringProperty { get; protected set; }

-        public bool IsStringProperty { get; protected set; }

-        public string Kind { get; }

-        public IReadOnlyDictionary<string, string> Metadata { get; protected set; }

-        public string Name { get; protected set; }

-        public string TypeName { get; protected set; }

-        public bool Equals(BoundAttributeDescriptor other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public abstract class BoundAttributeDescriptorBuilder {
 {
-        protected BoundAttributeDescriptorBuilder();

-        public abstract RazorDiagnosticCollection Diagnostics { get; }

-        public abstract string DisplayName { get; set; }

-        public abstract string Documentation { get; set; }

-        public abstract string IndexerAttributeNamePrefix { get; set; }

-        public abstract string IndexerValueTypeName { get; set; }

-        public abstract bool IsDictionary { get; set; }

-        public abstract bool IsEnum { get; set; }

-        public abstract IDictionary<string, string> Metadata { get; }

-        public abstract string Name { get; set; }

-        public abstract string TypeName { get; set; }

-    }
-    public static class BoundAttributeDescriptorBuilderExtensions {
 {
-        public static void AsDictionary(this BoundAttributeDescriptorBuilder builder, string attributeNamePrefix, string valueTypeName);

-        public static string GetPropertyName(this BoundAttributeDescriptorBuilder builder);

-        public static void SetPropertyName(this BoundAttributeDescriptorBuilder builder, string propertyName);

-    }
-    public static class BoundAttributeDescriptorExtensions {
 {
-        public static string GetPropertyName(this BoundAttributeDescriptor attribute);

-        public static bool IsDefaultKind(this BoundAttributeDescriptor attribute);

-    }
-    public abstract class DirectiveDescriptor {
 {
-        protected DirectiveDescriptor();

-        public abstract string Description { get; }

-        public abstract string Directive { get; }

-        public abstract string DisplayName { get; }

-        public abstract DirectiveKind Kind { get; }

-        public abstract IReadOnlyList<DirectiveTokenDescriptor> Tokens { get; }

-        public abstract DirectiveUsage Usage { get; }

-        public static DirectiveDescriptor CreateCodeBlockDirective(string directive);

-        public static DirectiveDescriptor CreateCodeBlockDirective(string directive, Action<IDirectiveDescriptorBuilder> configure);

-        public static DirectiveDescriptor CreateDirective(string directive, DirectiveKind kind);

-        public static DirectiveDescriptor CreateDirective(string directive, DirectiveKind kind, Action<IDirectiveDescriptorBuilder> configure);

-        public static DirectiveDescriptor CreateRazorBlockDirective(string directive);

-        public static DirectiveDescriptor CreateRazorBlockDirective(string directive, Action<IDirectiveDescriptorBuilder> configure);

-        public static DirectiveDescriptor CreateSingleLineDirective(string directive);

-        public static DirectiveDescriptor CreateSingleLineDirective(string directive, Action<IDirectiveDescriptorBuilder> configure);

-    }
-    public static class DirectiveDescriptorBuilderExtensions {
 {
-        public static IDirectiveDescriptorBuilder AddMemberToken(this IDirectiveDescriptorBuilder builder);

-        public static IDirectiveDescriptorBuilder AddMemberToken(this IDirectiveDescriptorBuilder builder, string name, string description);

-        public static IDirectiveDescriptorBuilder AddNamespaceToken(this IDirectiveDescriptorBuilder builder);

-        public static IDirectiveDescriptorBuilder AddNamespaceToken(this IDirectiveDescriptorBuilder builder, string name, string description);

-        public static IDirectiveDescriptorBuilder AddOptionalMemberToken(this IDirectiveDescriptorBuilder builder);

-        public static IDirectiveDescriptorBuilder AddOptionalMemberToken(this IDirectiveDescriptorBuilder builder, string name, string description);

-        public static IDirectiveDescriptorBuilder AddOptionalNamespaceToken(this IDirectiveDescriptorBuilder builder);

-        public static IDirectiveDescriptorBuilder AddOptionalNamespaceToken(this IDirectiveDescriptorBuilder builder, string name, string description);

-        public static IDirectiveDescriptorBuilder AddOptionalStringToken(this IDirectiveDescriptorBuilder builder);

-        public static IDirectiveDescriptorBuilder AddOptionalStringToken(this IDirectiveDescriptorBuilder builder, string name, string description);

-        public static IDirectiveDescriptorBuilder AddOptionalTypeToken(this IDirectiveDescriptorBuilder builder);

-        public static IDirectiveDescriptorBuilder AddOptionalTypeToken(this IDirectiveDescriptorBuilder builder, string name, string description);

-        public static IDirectiveDescriptorBuilder AddStringToken(this IDirectiveDescriptorBuilder builder);

-        public static IDirectiveDescriptorBuilder AddStringToken(this IDirectiveDescriptorBuilder builder, string name, string description);

-        public static IDirectiveDescriptorBuilder AddTypeToken(this IDirectiveDescriptorBuilder builder);

-        public static IDirectiveDescriptorBuilder AddTypeToken(this IDirectiveDescriptorBuilder builder, string name, string description);

-    }
-    public enum DirectiveKind {
 {
-        CodeBlock = 2,

-        RazorBlock = 1,

-        SingleLine = 0,

-    }
-    public abstract class DirectiveTokenDescriptor {
 {
-        protected DirectiveTokenDescriptor();

-        public virtual string Description { get; }

-        public abstract DirectiveTokenKind Kind { get; }

-        public virtual string Name { get; }

-        public abstract bool Optional { get; }

-        public static DirectiveTokenDescriptor CreateToken(DirectiveTokenKind kind);

-        public static DirectiveTokenDescriptor CreateToken(DirectiveTokenKind kind, bool optional);

-        public static DirectiveTokenDescriptor CreateToken(DirectiveTokenKind kind, bool optional, string name, string description);

-    }
-    public enum DirectiveTokenKind {
 {
-        Member = 2,

-        Namespace = 1,

-        String = 3,

-        Type = 0,

-    }
-    public enum DirectiveUsage {
 {
-        FileScopedMultipleOccurring = 2,

-        FileScopedSinglyOccurring = 1,

-        Unrestricted = 0,

-    }
-    public abstract class DocumentClassifierPassBase : IntermediateNodePassBase, IRazorDocumentClassifierPass, IRazorEngineFeature, IRazorFeature {
 {
-        protected DocumentClassifierPassBase();

-        protected abstract string DocumentKind { get; }

-        protected virtual void ConfigureTarget(CodeTargetBuilder builder);

-        protected sealed override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-        protected abstract bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-        protected virtual void OnDocumentStructureCreated(RazorCodeDocument codeDocument, NamespaceDeclarationIntermediateNode @namespace, ClassDeclarationIntermediateNode @class, MethodDeclarationIntermediateNode method);

-        protected override void OnInitialized();

-    }
-    public static class HtmlConventions {
 {
-        public static string ToHtmlCase(string name);

-    }
-    public interface IConfigureRazorCodeGenerationOptionsFeature : IRazorEngineFeature, IRazorFeature {
 {
-        int Order { get; }

-        void Configure(RazorCodeGenerationOptionsBuilder options);

-    }
-    public interface IConfigureRazorParserOptionsFeature : IRazorEngineFeature, IRazorFeature {
 {
-        int Order { get; }

-        void Configure(RazorParserOptionsBuilder options);

-    }
-    public interface IDirectiveDescriptorBuilder {
 {
-        string Description { get; set; }

-        string Directive { get; }

-        string DisplayName { get; set; }

-        DirectiveKind Kind { get; }

-        IList<DirectiveTokenDescriptor> Tokens { get; }

-        DirectiveUsage Usage { get; set; }

-        DirectiveDescriptor Build();

-    }
-    public interface IImportProjectFeature : IRazorFeature, IRazorProjectEngineFeature {
 {
-        IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem);

-    }
-    public abstract class IntermediateNodePassBase : RazorEngineFeatureBase {
 {
-        public static readonly int DefaultFeatureOrder;

-        protected IntermediateNodePassBase();

-        public virtual int Order { get; }

-        public void Execute(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-        protected abstract void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public interface IRazorCodeGenerationOptionsFeature : IRazorEngineFeature, IRazorFeature {
 {
-        RazorCodeGenerationOptions GetOptions();

-    }
-    public interface IRazorCSharpLoweringPhase : IRazorEnginePhase

-    public interface IRazorDirectiveClassifierPass : IRazorEngineFeature, IRazorFeature {
 {
-        int Order { get; }

-        void Execute(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public interface IRazorDirectiveClassifierPhase : IRazorEnginePhase

-    public interface IRazorDirectiveFeature : IRazorEngineFeature, IRazorFeature {
 {
-        ICollection<DirectiveDescriptor> Directives { get; }

-    }
-    public interface IRazorDocumentClassifierPass : IRazorEngineFeature, IRazorFeature {
 {
-        int Order { get; }

-        void Execute(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public interface IRazorDocumentClassifierPhase : IRazorEnginePhase

-    public interface IRazorEngineBuilder {
 {
-        bool DesignTime { get; }

-        ICollection<IRazorEngineFeature> Features { get; }

-        IList<IRazorEnginePhase> Phases { get; }

-        RazorEngine Build();

-    }
-    public interface IRazorEngineFeature : IRazorFeature {
 {
-        RazorEngine Engine { get; set; }

-    }
-    public interface IRazorEnginePhase {
 {
-        RazorEngine Engine { get; set; }

-        void Execute(RazorCodeDocument codeDocument);

-    }
-    public interface IRazorFeature

-    public interface IRazorIntermediateNodeLoweringPhase : IRazorEnginePhase

-    public interface IRazorOptimizationPass : IRazorEngineFeature, IRazorFeature {
 {
-        int Order { get; }

-        void Execute(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public interface IRazorOptimizationPhase : IRazorEnginePhase

-    public interface IRazorParserOptionsFeature : IRazorEngineFeature, IRazorFeature {
 {
-        RazorParserOptions GetOptions();

-    }
-    public interface IRazorParsingPhase : IRazorEnginePhase

-    public interface IRazorProjectEngineFeature : IRazorFeature {
 {
-        RazorProjectEngine ProjectEngine { get; set; }

-    }
-    public interface IRazorTagHelperBinderPhase : IRazorEnginePhase

-    public interface IRazorTargetExtensionFeature : IRazorEngineFeature, IRazorFeature {
 {
-        ICollection<ICodeTargetExtension> TargetExtensions { get; }

-    }
-    public interface ITagHelperDescriptorProvider : IRazorEngineFeature, IRazorFeature {
 {
-        int Order { get; }

-        void Execute(TagHelperDescriptorProviderContext context);

-    }
-    public interface ITagHelperFeature : IRazorEngineFeature, IRazorFeature {
 {
-        IReadOnlyList<TagHelperDescriptor> GetDescriptors();

-    }
-    public sealed class ItemCollection : ICollection<KeyValuePair<object, object>>, IEnumerable, IEnumerable<KeyValuePair<object, object>> {
 {
-        public ItemCollection();

-        public int Count { get; }

-        public bool IsReadOnly { get; }

-        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Count { get; }

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.IsReadOnly { get; }

-        public object this[object key] { get; set; }

-        public void Add(KeyValuePair<object, object> item);

-        public void Add(object key, object value);

-        public void Clear();

-        public bool Contains(KeyValuePair<object, object> item);

-        public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex);

-        public IEnumerator<KeyValuePair<object, object>> GetEnumerator();

-        public bool Remove(KeyValuePair<object, object> item);

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public class ProvideRazorExtensionInitializerAttribute : Attribute {
 {
-        public ProvideRazorExtensionInitializerAttribute(string extensionName, Type initializerType);

-        public string ExtensionName { get; }

-        public Type InitializerType { get; }

-    }
-    public abstract class RazorCodeDocument {
 {
-        protected RazorCodeDocument();

-        public abstract IReadOnlyList<RazorSourceDocument> Imports { get; }

-        public abstract ItemCollection Items { get; }

-        public abstract RazorSourceDocument Source { get; }

-        public static RazorCodeDocument Create(RazorSourceDocument source);

-        public static RazorCodeDocument Create(RazorSourceDocument source, IEnumerable<RazorSourceDocument> imports);

-        public static RazorCodeDocument Create(RazorSourceDocument source, IEnumerable<RazorSourceDocument> imports, RazorParserOptions parserOptions, RazorCodeGenerationOptions codeGenerationOptions);

-    }
-    public static class RazorCodeDocumentExtensions {
 {
-        public static RazorCodeGenerationOptions GetCodeGenerationOptions(this RazorCodeDocument document);

-        public static RazorCSharpDocument GetCSharpDocument(this RazorCodeDocument document);

-        public static DocumentIntermediateNode GetDocumentIntermediateNode(this RazorCodeDocument document);

-        public static IReadOnlyList<RazorSyntaxTree> GetImportSyntaxTrees(this RazorCodeDocument document);

-        public static RazorParserOptions GetParserOptions(this RazorCodeDocument document);

-        public static RazorSyntaxTree GetSyntaxTree(this RazorCodeDocument document);

-        public static TagHelperDocumentContext GetTagHelperContext(this RazorCodeDocument document);

-        public static void SetCodeGenerationOptions(this RazorCodeDocument document, RazorCodeGenerationOptions codeGenerationOptions);

-        public static void SetCSharpDocument(this RazorCodeDocument document, RazorCSharpDocument csharp);

-        public static void SetDocumentIntermediateNode(this RazorCodeDocument document, DocumentIntermediateNode documentNode);

-        public static void SetImportSyntaxTrees(this RazorCodeDocument document, IReadOnlyList<RazorSyntaxTree> syntaxTrees);

-        public static void SetParserOptions(this RazorCodeDocument document, RazorParserOptions parserOptions);

-        public static void SetSyntaxTree(this RazorCodeDocument document, RazorSyntaxTree syntaxTree);

-        public static void SetTagHelperContext(this RazorCodeDocument document, TagHelperDocumentContext context);

-    }
-    public abstract class RazorCodeGenerationOptions {
 {
-        protected RazorCodeGenerationOptions();

-        public abstract bool DesignTime { get; }

-        public abstract int IndentSize { get; }

-        public abstract bool IndentWithTabs { get; }

-        public abstract bool SuppressChecksum { get; }

-        public virtual bool SuppressMetadataAttributes { get; protected set; }

-        public static RazorCodeGenerationOptions Create(Action<RazorCodeGenerationOptionsBuilder> configure);

-        public static RazorCodeGenerationOptions CreateDefault();

-        public static RazorCodeGenerationOptions CreateDesignTime(Action<RazorCodeGenerationOptionsBuilder> configure);

-        public static RazorCodeGenerationOptions CreateDesignTimeDefault();

-    }
-    public abstract class RazorCodeGenerationOptionsBuilder {
 {
-        protected RazorCodeGenerationOptionsBuilder();

-        public virtual RazorConfiguration Configuration { get; }

-        public abstract bool DesignTime { get; }

-        public abstract int IndentSize { get; set; }

-        public abstract bool IndentWithTabs { get; set; }

-        public abstract bool SuppressChecksum { get; set; }

-        public virtual bool SuppressMetadataAttributes { get; set; }

-        public abstract RazorCodeGenerationOptions Build();

-        public virtual void SetDesignTime(bool designTime);

-    }
-    public abstract class RazorConfiguration : IEquatable<RazorConfiguration> {
 {
-        public static readonly RazorConfiguration Default;

-        protected RazorConfiguration();

-        public abstract string ConfigurationName { get; }

-        public abstract IReadOnlyList<RazorExtension> Extensions { get; }

-        public abstract RazorLanguageVersion LanguageVersion { get; }

-        public static RazorConfiguration Create(RazorLanguageVersion languageVersion, string configurationName, IEnumerable<RazorExtension> extensions);

-        public virtual bool Equals(RazorConfiguration other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public abstract class RazorCSharpDocument {
 {
-        protected RazorCSharpDocument();

-        public abstract IReadOnlyList<RazorDiagnostic> Diagnostics { get; }

-        public abstract string GeneratedCode { get; }

-        public abstract RazorCodeGenerationOptions Options { get; }

-        public abstract IReadOnlyList<SourceMapping> SourceMappings { get; }

-        public static RazorCSharpDocument Create(string generatedCode, RazorCodeGenerationOptions options, IEnumerable<RazorDiagnostic> diagnostics);

-        public static RazorCSharpDocument Create(string generatedCode, RazorCodeGenerationOptions options, IEnumerable<RazorDiagnostic> diagnostics, IEnumerable<SourceMapping> sourceMappings);

-    }
-    public abstract class RazorDiagnostic : IEquatable<RazorDiagnostic>, IFormattable {
 {
-        protected RazorDiagnostic();

-        public abstract string Id { get; }

-        public abstract RazorDiagnosticSeverity Severity { get; }

-        public abstract SourceSpan Span { get; }

-        public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor, SourceSpan span);

-        public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor, SourceSpan span, params object[] args);

-        public abstract bool Equals(RazorDiagnostic other);

-        public override bool Equals(object obj);

-        public abstract override int GetHashCode();

-        public string GetMessage();

-        public abstract string GetMessage(IFormatProvider formatProvider);

-        string System.IFormattable.ToString(string ignore, IFormatProvider formatProvider);

-        public override string ToString();

-    }
-    public sealed class RazorDiagnosticCollection : ICollection<RazorDiagnostic>, IEnumerable, IEnumerable<RazorDiagnostic>, IList<RazorDiagnostic> {
 {
-        public RazorDiagnosticCollection();

-        public int Count { get; }

-        public bool IsReadOnly { get; }

-        public RazorDiagnostic this[int index] { get; set; }

-        public void Add(RazorDiagnostic item);

-        public void Clear();

-        public bool Contains(RazorDiagnostic item);

-        public void CopyTo(RazorDiagnostic[] array, int arrayIndex);

-        public RazorDiagnosticCollection.Enumerator GetEnumerator();

-        public int IndexOf(RazorDiagnostic item);

-        public void Insert(int index, RazorDiagnostic item);

-        public bool Remove(RazorDiagnostic item);

-        public void RemoveAt(int index);

-        IEnumerator<RazorDiagnostic> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Razor.Language.RazorDiagnostic>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<RazorDiagnostic> {
 {
-            public Enumerator(RazorDiagnosticCollection collection);

-            public RazorDiagnostic Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public sealed class RazorDiagnosticDescriptor : IEquatable<RazorDiagnosticDescriptor> {
 {
-        public RazorDiagnosticDescriptor(string id, Func<string> messageFormat, RazorDiagnosticSeverity severity);

-        public string Id { get; }

-        public RazorDiagnosticSeverity Severity { get; }

-        public bool Equals(RazorDiagnosticDescriptor other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public string GetMessageFormat();

-    }
-    public enum RazorDiagnosticSeverity {
 {
-        Error = 3,

-    }
-    public abstract class RazorEngine {
 {
-        protected RazorEngine();

-        public abstract IReadOnlyList<IRazorEngineFeature> Features { get; }

-        public abstract IReadOnlyList<IRazorEnginePhase> Phases { get; }

-        public static RazorEngine Create();

-        public static RazorEngine Create(Action<IRazorEngineBuilder> configure);

-        public static RazorEngine CreateDesignTime();

-        public static RazorEngine CreateDesignTime(Action<IRazorEngineBuilder> configure);

-        public static RazorEngine CreateDesignTimeEmpty(Action<IRazorEngineBuilder> configure);

-        public static RazorEngine CreateEmpty(Action<IRazorEngineBuilder> configure);

-        public abstract void Process(RazorCodeDocument document);

-    }
-    public static class RazorEngineBuilderExtensions {
 {
-        public static IRazorEngineBuilder AddDirective(this IRazorEngineBuilder builder, DirectiveDescriptor directive);

-        public static IRazorEngineBuilder AddTargetExtension(this IRazorEngineBuilder builder, ICodeTargetExtension extension);

-        public static IRazorEngineBuilder ConfigureClass(this IRazorEngineBuilder builder, Action<RazorCodeDocument, ClassDeclarationIntermediateNode> configureClass);

-        public static IRazorEngineBuilder SetBaseType(this IRazorEngineBuilder builder, string baseType);

-        public static IRazorEngineBuilder SetNamespace(this IRazorEngineBuilder builder, string namespaceName);

-    }
-    public abstract class RazorEngineFeatureBase : IRazorEngineFeature, IRazorFeature {
 {
-        protected RazorEngineFeatureBase();

-        public RazorEngine Engine { get; set; }

-        protected TFeature GetRequiredFeature<TFeature>() where TFeature : IRazorEngineFeature;

-        protected virtual void OnInitialized();

-        protected void ThrowForMissingDocumentDependency<TDocumentDependency>(TDocumentDependency value);

-        protected void ThrowForMissingFeatureDependency<TEngineDependency>(TEngineDependency value);

-    }
-    public abstract class RazorEnginePhaseBase : IRazorEnginePhase {
 {
-        protected RazorEnginePhaseBase();

-        public RazorEngine Engine { get; set; }

-        public void Execute(RazorCodeDocument codeDocument);

-        protected abstract void ExecuteCore(RazorCodeDocument codeDocument);

-        protected T GetRequiredFeature<T>();

-        protected virtual void OnIntialized();

-        protected void ThrowForMissingDocumentDependency<TDocumentDependency>(TDocumentDependency value);

-        protected void ThrowForMissingFeatureDependency<TEngineDependency>(TEngineDependency value);

-    }
-    public abstract class RazorExtension {
 {
-        protected RazorExtension();

-        public abstract string ExtensionName { get; }

-    }
-    public abstract class RazorExtensionInitializer {
 {
-        protected RazorExtensionInitializer();

-        public abstract void Initialize(RazorProjectEngineBuilder builder);

-    }
-    public sealed class RazorLanguageVersion : IComparable<RazorLanguageVersion>, IEquatable<RazorLanguageVersion> {
 {
-        public static readonly RazorLanguageVersion Experimental;

-        public static readonly RazorLanguageVersion Latest;

-        public static readonly RazorLanguageVersion Version_1_0;

-        public static readonly RazorLanguageVersion Version_1_1;

-        public static readonly RazorLanguageVersion Version_2_0;

-        public static readonly RazorLanguageVersion Version_2_1;

-        public int Major { get; }

-        public int Minor { get; }

-        public int CompareTo(RazorLanguageVersion other);

-        public bool Equals(RazorLanguageVersion other);

-        public override int GetHashCode();

-        public static RazorLanguageVersion Parse(string languageVersion);

-        public override string ToString();

-        public static bool TryParse(string languageVersion, out RazorLanguageVersion version);

-    }
-    public abstract class RazorParserOptions {
 {
-        protected RazorParserOptions();

-        public abstract bool DesignTime { get; }

-        public abstract IReadOnlyCollection<DirectiveDescriptor> Directives { get; }

-        public abstract bool ParseLeadingDirectives { get; }

-        public virtual RazorLanguageVersion Version { get; }

-        public static RazorParserOptions Create(Action<RazorParserOptionsBuilder> configure);

-        public static RazorParserOptions CreateDefault();

-        public static RazorParserOptions CreateDesignTime(Action<RazorParserOptionsBuilder> configure);

-    }
-    public abstract class RazorParserOptionsBuilder {
 {
-        protected RazorParserOptionsBuilder();

-        public virtual RazorConfiguration Configuration { get; }

-        public abstract bool DesignTime { get; }

-        public abstract ICollection<DirectiveDescriptor> Directives { get; }

-        public virtual RazorLanguageVersion LanguageVersion { get; }

-        public abstract bool ParseLeadingDirectives { get; set; }

-        public abstract RazorParserOptions Build();

-        public virtual void SetDesignTime(bool designTime);

-    }
-    public abstract class RazorProject {
 {
-        protected RazorProject();

-        public static RazorProject Create(string rootDirectoryPath);

-        public abstract IEnumerable<RazorProjectItem> EnumerateItems(string basePath);

-        public IEnumerable<RazorProjectItem> FindHierarchicalItems(string path, string fileName);

-        public virtual IEnumerable<RazorProjectItem> FindHierarchicalItems(string basePath, string path, string fileName);

-        public abstract RazorProjectItem GetItem(string path);

-        protected virtual string NormalizeAndEnsureValidPath(string path);

-    }
-    public abstract class RazorProjectEngine {
 {
-        protected RazorProjectEngine();

-        public abstract RazorConfiguration Configuration { get; }

-        public abstract RazorEngine Engine { get; }

-        public IReadOnlyList<IRazorEngineFeature> EngineFeatures { get; }

-        public abstract RazorProjectFileSystem FileSystem { get; }

-        public IReadOnlyList<IRazorEnginePhase> Phases { get; }

-        public abstract IReadOnlyList<IRazorProjectEngineFeature> ProjectFeatures { get; }

-        public static RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem);

-        public static RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure);

-        protected abstract RazorCodeDocument CreateCodeDocumentCore(RazorProjectItem projectItem);

-        protected abstract RazorCodeDocument CreateCodeDocumentDesignTimeCore(RazorProjectItem projectItem);

-        public virtual RazorCodeDocument Process(RazorProjectItem projectItem);

-        protected abstract void ProcessCore(RazorCodeDocument codeDocument);

-        public virtual RazorCodeDocument ProcessDesignTime(RazorProjectItem projectItem);

-    }
-    public abstract class RazorProjectEngineBuilder {
 {
-        protected RazorProjectEngineBuilder();

-        public abstract RazorConfiguration Configuration { get; }

-        public abstract ICollection<IRazorFeature> Features { get; }

-        public abstract RazorProjectFileSystem FileSystem { get; }

-        public abstract IList<IRazorEnginePhase> Phases { get; }

-        public abstract RazorProjectEngine Build();

-    }
-    public static class RazorProjectEngineBuilderExtensions {
 {
-        public static RazorProjectEngineBuilder AddDefaultImports(this RazorProjectEngineBuilder builder, params string[] imports);

-        public static RazorProjectEngineBuilder AddDirective(this RazorProjectEngineBuilder builder, DirectiveDescriptor directive);

-        public static RazorProjectEngineBuilder AddTargetExtension(this RazorProjectEngineBuilder builder, ICodeTargetExtension extension);

-        public static RazorProjectEngineBuilder ConfigureClass(this RazorProjectEngineBuilder builder, Action<RazorCodeDocument, ClassDeclarationIntermediateNode> configureClass);

-        public static RazorProjectEngineBuilder SetBaseType(this RazorProjectEngineBuilder builder, string baseType);

-        public static void SetImportFeature(this RazorProjectEngineBuilder builder, IImportProjectFeature feature);

-        public static RazorProjectEngineBuilder SetNamespace(this RazorProjectEngineBuilder builder, string namespaceName);

-    }
-    public abstract class RazorProjectEngineFeatureBase : IRazorFeature, IRazorProjectEngineFeature {
 {
-        protected RazorProjectEngineFeatureBase();

-        public virtual RazorProjectEngine ProjectEngine { get; set; }

-        protected virtual void OnInitialized();

-    }
-    public abstract class RazorProjectFileSystem : RazorProject {
 {
-        protected RazorProjectFileSystem();

-        public static new RazorProjectFileSystem Create(string rootDirectoryPath);

-    }
-    public abstract class RazorProjectItem {
 {
-        protected RazorProjectItem();

-        public abstract string BasePath { get; }

-        public string CombinedPath { get; }

-        public abstract bool Exists { get; }

-        public string Extension { get; }

-        public string FileName { get; }

-        public abstract string FilePath { get; }

-        public string FilePathWithoutExtension { get; }

-        public abstract string PhysicalPath { get; }

-        public virtual string RelativePhysicalPath { get; }

-        public abstract Stream Read();

-    }
-    public abstract class RazorSourceDocument {
 {
-        protected RazorSourceDocument();

-        public abstract Encoding Encoding { get; }

-        public abstract string FilePath { get; }

-        public abstract int Length { get; }

-        public abstract RazorSourceLineCollection Lines { get; }

-        public virtual string RelativePath { get; }

-        public abstract char this[int position] { get; }

-        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

-        public static RazorSourceDocument Create(string content, RazorSourceDocumentProperties properties);

-        public static RazorSourceDocument Create(string content, string fileName);

-        public static RazorSourceDocument Create(string content, string fileName, Encoding encoding);

-        public static RazorSourceDocument Create(string content, Encoding encoding, RazorSourceDocumentProperties properties);

-        public abstract byte[] GetChecksum();

-        public virtual string GetChecksumAlgorithm();

-        public virtual string GetFilePathForDisplay();

-        public static RazorSourceDocument ReadFrom(RazorProjectItem projectItem);

-        public static RazorSourceDocument ReadFrom(Stream stream, string fileName);

-        public static RazorSourceDocument ReadFrom(Stream stream, string fileName, Encoding encoding);

-        public static RazorSourceDocument ReadFrom(Stream stream, Encoding encoding, RazorSourceDocumentProperties properties);

-    }
-    public sealed class RazorSourceDocumentProperties {
 {
-        public RazorSourceDocumentProperties();

-        public RazorSourceDocumentProperties(string filePath, string relativePath);

-        public string FilePath { get; }

-        public string RelativePath { get; }

-    }
-    public abstract class RazorSourceLineCollection {
 {
-        protected RazorSourceLineCollection();

-        public abstract int Count { get; }

-        public abstract int GetLineLength(int index);

-    }
-    public abstract class RazorSyntaxTree {
 {
-        protected RazorSyntaxTree();

-        public abstract IReadOnlyList<RazorDiagnostic> Diagnostics { get; }

-        public abstract RazorParserOptions Options { get; }

-        public abstract RazorSourceDocument Source { get; }

-        public static RazorSyntaxTree Parse(RazorSourceDocument source);

-        public static RazorSyntaxTree Parse(RazorSourceDocument source, RazorParserOptions options);

-    }
-    public class RazorTemplateEngine {
 {
-        public RazorTemplateEngine(RazorEngine engine, RazorProject project);

-        public RazorEngine Engine { get; }

-        public RazorTemplateEngineOptions Options { get; set; }

-        public RazorProject Project { get; }

-        public virtual RazorCodeDocument CreateCodeDocument(RazorProjectItem projectItem);

-        public virtual RazorCodeDocument CreateCodeDocument(string path);

-        public virtual RazorCSharpDocument GenerateCode(RazorCodeDocument codeDocument);

-        public RazorCSharpDocument GenerateCode(RazorProjectItem projectItem);

-        public RazorCSharpDocument GenerateCode(string path);

-        public virtual IEnumerable<RazorProjectItem> GetImportItems(RazorProjectItem projectItem);

-        public IEnumerable<RazorProjectItem> GetImportItems(string path);

-        public virtual IEnumerable<RazorSourceDocument> GetImports(RazorProjectItem projectItem);

-        public IEnumerable<RazorSourceDocument> GetImports(string path);

-    }
-    public sealed class RazorTemplateEngineOptions {
 {
-        public RazorTemplateEngineOptions();

-        public RazorSourceDocument DefaultImports { get; set; }

-        public string ImportsFileName { get; set; }

-    }
-    public abstract class RequiredAttributeDescriptor : IEquatable<RequiredAttributeDescriptor> {
 {
-        protected RequiredAttributeDescriptor();

-        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

-        public string DisplayName { get; protected set; }

-        public bool HasErrors { get; }

-        public string Name { get; protected set; }

-        public RequiredAttributeDescriptor.NameComparisonMode NameComparison { get; protected set; }

-        public string Value { get; protected set; }

-        public RequiredAttributeDescriptor.ValueComparisonMode ValueComparison { get; protected set; }

-        public bool Equals(RequiredAttributeDescriptor other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        public enum NameComparisonMode {
 {
-            FullMatch = 0,

-            PrefixMatch = 1,

-        }
-        public enum ValueComparisonMode {
 {
-            FullMatch = 1,

-            None = 0,

-            PrefixMatch = 2,

-            SuffixMatch = 3,

-        }
-    }
-    public abstract class RequiredAttributeDescriptorBuilder {
 {
-        protected RequiredAttributeDescriptorBuilder();

-        public abstract RazorDiagnosticCollection Diagnostics { get; }

-        public abstract string Name { get; set; }

-        public abstract RequiredAttributeDescriptor.NameComparisonMode NameComparisonMode { get; set; }

-        public abstract string Value { get; set; }

-        public abstract RequiredAttributeDescriptor.ValueComparisonMode ValueComparisonMode { get; set; }

-    }
-    public sealed class SourceChange : IEquatable<SourceChange> {
 {
-        public SourceChange(SourceSpan span, string newText);

-        public SourceChange(int absoluteIndex, int length, string newText);

-        public bool IsDelete { get; }

-        public bool IsInsert { get; }

-        public bool IsReplace { get; }

-        public string NewText { get; }

-        public SourceSpan Span { get; }

-        public bool Equals(SourceChange other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public struct SourceLocation : IEquatable<SourceLocation> {
 {
-        public static readonly SourceLocation Undefined;

-        public static readonly SourceLocation Zero;

-        public SourceLocation(int absoluteIndex, int lineIndex, int characterIndex);

-        public SourceLocation(string filePath, int absoluteIndex, int lineIndex, int characterIndex);

-        public int AbsoluteIndex { get; set; }

-        public int CharacterIndex { get; set; }

-        public string FilePath { get; set; }

-        public int LineIndex { get; set; }

-        public bool Equals(SourceLocation other);

-        public override bool Equals(object obj);

-        public static SourceLocation FromSpan(Nullable<SourceSpan> span);

-        public override int GetHashCode();

-        public static bool operator ==(SourceLocation left, SourceLocation right);

-        public static bool operator !=(SourceLocation left, SourceLocation right);

-        public override string ToString();

-    }
-    public sealed class SourceMapping : IEquatable<SourceMapping> {
 {
-        public SourceMapping(SourceSpan originalSpan, SourceSpan generatedSpan);

-        public SourceSpan GeneratedSpan { get; }

-        public SourceSpan OriginalSpan { get; }

-        public bool Equals(SourceMapping other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public struct SourceSpan : IEquatable<SourceSpan> {
 {
-        public static readonly SourceSpan Undefined;

-        public SourceSpan(SourceLocation location, int contentLength);

-        public SourceSpan(int absoluteIndex, int length);

-        public SourceSpan(int absoluteIndex, int lineIndex, int characterIndex, int length);

-        public SourceSpan(string filePath, int absoluteIndex, int lineIndex, int characterIndex, int length);

-        public int AbsoluteIndex { get; }

-        public int CharacterIndex { get; }

-        public string FilePath { get; }

-        public int Length { get; }

-        public int LineIndex { get; }

-        public bool Equals(SourceSpan other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(SourceSpan left, SourceSpan right);

-        public static bool operator !=(SourceSpan left, SourceSpan right);

-        public override string ToString();

-    }
-    public sealed class TagHelperBinding {
 {
-        public IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }

-        public IEnumerable<TagHelperDescriptor> Descriptors { get; }

-        public string ParentTagName { get; }

-        public string TagHelperPrefix { get; }

-        public string TagName { get; }

-        public IReadOnlyList<TagMatchingRuleDescriptor> GetBoundRules(TagHelperDescriptor descriptor);

-    }
-    public static class TagHelperConventions {
 {
-        public static readonly string DefaultKind;

-    }
-    public abstract class TagHelperDescriptor : IEquatable<TagHelperDescriptor> {
 {
-        protected TagHelperDescriptor(string kind);

-        public IReadOnlyList<AllowedChildTagDescriptor> AllowedChildTags { get; protected set; }

-        public string AssemblyName { get; protected set; }

-        public IReadOnlyList<BoundAttributeDescriptor> BoundAttributes { get; protected set; }

-        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

-        public string DisplayName { get; protected set; }

-        public string Documentation { get; protected set; }

-        public bool HasErrors { get; }

-        public string Kind { get; }

-        public IReadOnlyDictionary<string, string> Metadata { get; protected set; }

-        public string Name { get; protected set; }

-        public IReadOnlyList<TagMatchingRuleDescriptor> TagMatchingRules { get; protected set; }

-        public string TagOutputHint { get; protected set; }

-        public bool Equals(TagHelperDescriptor other);

-        public override bool Equals(object obj);

-        public virtual IEnumerable<RazorDiagnostic> GetAllDiagnostics();

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public abstract class TagHelperDescriptorBuilder {
 {
-        protected TagHelperDescriptorBuilder();

-        public abstract IReadOnlyList<AllowedChildTagDescriptorBuilder> AllowedChildTags { get; }

-        public abstract string AssemblyName { get; }

-        public abstract IReadOnlyList<BoundAttributeDescriptorBuilder> BoundAttributes { get; }

-        public abstract RazorDiagnosticCollection Diagnostics { get; }

-        public abstract string DisplayName { get; set; }

-        public abstract string Documentation { get; set; }

-        public abstract string Kind { get; }

-        public abstract IDictionary<string, string> Metadata { get; }

-        public abstract string Name { get; }

-        public abstract IReadOnlyList<TagMatchingRuleDescriptorBuilder> TagMatchingRules { get; }

-        public abstract string TagOutputHint { get; set; }

-        public abstract void AllowChildTag(Action<AllowedChildTagDescriptorBuilder> configure);

-        public abstract void BindAttribute(Action<BoundAttributeDescriptorBuilder> configure);

-        public abstract TagHelperDescriptor Build();

-        public static TagHelperDescriptorBuilder Create(string name, string assemblyName);

-        public static TagHelperDescriptorBuilder Create(string kind, string name, string assemblyName);

-        public abstract void Reset();

-        public abstract void TagMatchingRule(Action<TagMatchingRuleDescriptorBuilder> configure);

-    }
-    public static class TagHelperDescriptorBuilderExtensions {
 {
-        public static string GetTypeName(this TagHelperDescriptorBuilder builder);

-        public static void SetTypeName(this TagHelperDescriptorBuilder builder, string typeName);

-    }
-    public static class TagHelperDescriptorExtensions {
 {
-        public static string GetTypeName(this TagHelperDescriptor tagHelper);

-        public static bool IsDefaultKind(this TagHelperDescriptor tagHelper);

-        public static bool KindUsesDefaultTagHelperRuntime(this TagHelperDescriptor tagHelper);

-    }
-    public abstract class TagHelperDescriptorProviderContext {
 {
-        protected TagHelperDescriptorProviderContext();

-        public virtual bool ExcludeHidden { get; set; }

-        public virtual bool IncludeDocumentation { get; set; }

-        public abstract ItemCollection Items { get; }

-        public abstract ICollection<TagHelperDescriptor> Results { get; }

-        public static TagHelperDescriptorProviderContext Create();

-        public static TagHelperDescriptorProviderContext Create(ICollection<TagHelperDescriptor> results);

-    }
-    public abstract class TagHelperDocumentContext {
 {
-        protected TagHelperDocumentContext();

-        public abstract string Prefix { get; }

-        public abstract IReadOnlyList<TagHelperDescriptor> TagHelpers { get; }

-        public static TagHelperDocumentContext Create(string prefix, IEnumerable<TagHelperDescriptor> tagHelpers);

-    }
-    public static class TagHelperMetadata {
 {
-        public static class Common {
 {
-            public static readonly string PropertyName;

-            public static readonly string TypeName;

-        }
-        public static class Runtime {
 {
-            public static readonly string Name;

-        }
-    }
-    public abstract class TagMatchingRuleDescriptor : IEquatable<TagMatchingRuleDescriptor> {
 {
-        protected TagMatchingRuleDescriptor();

-        public IReadOnlyList<RequiredAttributeDescriptor> Attributes { get; protected set; }

-        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

-        public bool HasErrors { get; }

-        public string ParentTag { get; protected set; }

-        public string TagName { get; protected set; }

-        public TagStructure TagStructure { get; protected set; }

-        public bool Equals(TagMatchingRuleDescriptor other);

-        public override bool Equals(object obj);

-        public virtual IEnumerable<RazorDiagnostic> GetAllDiagnostics();

-        public override int GetHashCode();

-    }
-    public abstract class TagMatchingRuleDescriptorBuilder {
 {
-        protected TagMatchingRuleDescriptorBuilder();

-        public abstract IReadOnlyList<RequiredAttributeDescriptorBuilder> Attributes { get; }

-        public abstract RazorDiagnosticCollection Diagnostics { get; }

-        public abstract string ParentTag { get; set; }

-        public abstract string TagName { get; set; }

-        public abstract TagStructure TagStructure { get; set; }

-        public abstract void Attribute(Action<RequiredAttributeDescriptorBuilder> configure);

-    }
-    public enum TagMode {
 {
-        SelfClosing = 1,

-        StartTagAndEndTag = 0,

-        StartTagOnly = 2,

-    }
-    public enum TagStructure {
 {
-        NormalOrSelfClosing = 1,

-        Unspecified = 0,

-        WithoutEndTag = 2,

-    }
-}
```

