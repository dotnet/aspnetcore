# Microsoft.CodeAnalysis.Emit

``` diff
-namespace Microsoft.CodeAnalysis.Emit {
 {
-    public enum DebugInformationFormat {
 {
-        Embedded = 3,

-        Pdb = 1,

-        PortablePdb = 2,

-    }
-    public struct EditAndContinueMethodDebugInformation {
 {
-        public static EditAndContinueMethodDebugInformation Create(ImmutableArray<byte> compressedSlotMap, ImmutableArray<byte> compressedLambdaMap);

-    }
-    public sealed class EmitBaseline {
 {
-        public ModuleMetadata OriginalMetadata { get; }

-        public static EmitBaseline CreateInitialBaseline(ModuleMetadata module, Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> debugInformationProvider);

-        public static EmitBaseline CreateInitialBaseline(ModuleMetadata module, Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> debugInformationProvider, Func<MethodDefinitionHandle, StandaloneSignatureHandle> localSignatureProvider, bool hasPortableDebugInformation);

-    }
-    public sealed class EmitDifferenceResult : EmitResult {
 {
-        public EmitBaseline Baseline { get; }

-    }
-    public sealed class EmitOptions : IEquatable<EmitOptions> {
 {
-        public EmitOptions(bool metadataOnly, DebugInformationFormat debugInformationFormat, string pdbFilePath, string outputNameOverride, int fileAlignment, ulong baseAddress, bool highEntropyVirtualAddressSpace, SubsystemVersion subsystemVersion, string runtimeMetadataVersion, bool tolerateErrors, bool includePrivateMembers);

-        public EmitOptions(bool metadataOnly, DebugInformationFormat debugInformationFormat, string pdbFilePath, string outputNameOverride, int fileAlignment, ulong baseAddress, bool highEntropyVirtualAddressSpace, SubsystemVersion subsystemVersion, string runtimeMetadataVersion, bool tolerateErrors, bool includePrivateMembers, ImmutableArray<InstrumentationKind> instrumentationKinds);

-        public EmitOptions(bool metadataOnly = false, DebugInformationFormat debugInformationFormat = (DebugInformationFormat)0, string pdbFilePath = null, string outputNameOverride = null, int fileAlignment = 0, ulong baseAddress = (ulong)0, bool highEntropyVirtualAddressSpace = false, SubsystemVersion subsystemVersion = default(SubsystemVersion), string runtimeMetadataVersion = null, bool tolerateErrors = false, bool includePrivateMembers = true, ImmutableArray<InstrumentationKind> instrumentationKinds = default(ImmutableArray<InstrumentationKind>), Nullable<HashAlgorithmName> pdbChecksumAlgorithm = default(Nullable<HashAlgorithmName>));

-        public ulong BaseAddress { get; private set; }

-        public DebugInformationFormat DebugInformationFormat { get; private set; }

-        public bool EmitMetadataOnly { get; private set; }

-        public int FileAlignment { get; private set; }

-        public bool HighEntropyVirtualAddressSpace { get; private set; }

-        public bool IncludePrivateMembers { get; private set; }

-        public ImmutableArray<InstrumentationKind> InstrumentationKinds { get; private set; }

-        public string OutputNameOverride { get; private set; }

-        public HashAlgorithmName PdbChecksumAlgorithm { get; private set; }

-        public string PdbFilePath { get; private set; }

-        public string RuntimeMetadataVersion { get; private set; }

-        public SubsystemVersion SubsystemVersion { get; private set; }

-        public bool TolerateErrors { get; private set; }

-        public bool Equals(EmitOptions other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(EmitOptions left, EmitOptions right);

-        public static bool operator !=(EmitOptions left, EmitOptions right);

-        public EmitOptions WithBaseAddress(ulong value);

-        public EmitOptions WithDebugInformationFormat(DebugInformationFormat format);

-        public EmitOptions WithEmitMetadataOnly(bool value);

-        public EmitOptions WithFileAlignment(int value);

-        public EmitOptions WithHighEntropyVirtualAddressSpace(bool value);

-        public EmitOptions WithIncludePrivateMembers(bool value);

-        public EmitOptions WithInstrumentationKinds(ImmutableArray<InstrumentationKind> instrumentationKinds);

-        public EmitOptions WithOutputNameOverride(string outputName);

-        public EmitOptions WithPdbChecksumAlgorithm(HashAlgorithmName name);

-        public EmitOptions WithPdbFilePath(string path);

-        public EmitOptions WithRuntimeMetadataVersion(string version);

-        public EmitOptions WithSubsystemVersion(SubsystemVersion subsystemVersion);

-        public EmitOptions WithTolerateErrors(bool value);

-    }
-    public class EmitResult {
 {
-        public ImmutableArray<Diagnostic> Diagnostics { get; }

-        public bool Success { get; }

-        protected virtual string GetDebuggerDisplay();

-    }
-    public enum InstrumentationKind {
 {
-        None = 0,

-        TestCoverage = 1,

-    }
-    public struct SemanticEdit : IEquatable<SemanticEdit> {
 {
-        public SemanticEdit(SemanticEditKind kind, ISymbol oldSymbol, ISymbol newSymbol, Func<SyntaxNode, SyntaxNode> syntaxMap = null, bool preserveLocalVariables = false);

-        public SemanticEditKind Kind { get; }

-        public ISymbol NewSymbol { get; }

-        public ISymbol OldSymbol { get; }

-        public bool PreserveLocalVariables { get; }

-        public Func<SyntaxNode, SyntaxNode> SyntaxMap { get; }

-        public bool Equals(SemanticEdit other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public enum SemanticEditKind {
 {
-        Delete = 3,

-        Insert = 2,

-        None = 0,

-        Update = 1,

-    }
-}
```

