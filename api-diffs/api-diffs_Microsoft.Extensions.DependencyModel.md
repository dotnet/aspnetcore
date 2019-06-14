# Microsoft.Extensions.DependencyModel

``` diff
-namespace Microsoft.Extensions.DependencyModel {
 {
-    public class CompilationLibrary : Library {
 {
-        public CompilationLibrary(string type, string name, string version, string hash, IEnumerable<string> assemblies, IEnumerable<Dependency> dependencies, bool serviceable);

-        public CompilationLibrary(string type, string name, string version, string hash, IEnumerable<string> assemblies, IEnumerable<Dependency> dependencies, bool serviceable, string path, string hashPath);

-        public IReadOnlyList<string> Assemblies { get; }

-        public IEnumerable<string> ResolveReferencePaths();

-        public IEnumerable<string> ResolveReferencePaths(params ICompilationAssemblyResolver[] customResolvers);

-    }
-    public class CompilationOptions {
 {
-        public CompilationOptions(IEnumerable<string> defines, string languageVersion, string platform, Nullable<bool> allowUnsafe, Nullable<bool> warningsAsErrors, Nullable<bool> optimize, string keyFile, Nullable<bool> delaySign, Nullable<bool> publicSign, string debugType, Nullable<bool> emitEntryPoint, Nullable<bool> generateXmlDocumentation);

-        public Nullable<bool> AllowUnsafe { get; }

-        public string DebugType { get; }

-        public static CompilationOptions Default { get; }

-        public IReadOnlyList<string> Defines { get; }

-        public Nullable<bool> DelaySign { get; }

-        public Nullable<bool> EmitEntryPoint { get; }

-        public Nullable<bool> GenerateXmlDocumentation { get; }

-        public string KeyFile { get; }

-        public string LanguageVersion { get; }

-        public Nullable<bool> Optimize { get; }

-        public string Platform { get; }

-        public Nullable<bool> PublicSign { get; }

-        public Nullable<bool> WarningsAsErrors { get; }

-    }
-    public struct Dependency {
 {
-        public Dependency(string name, string version);

-        public string Name { get; }

-        public string Version { get; }

-        public bool Equals(Dependency other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public class DependencyContext {
 {
-        public DependencyContext(TargetInfo target, CompilationOptions compilationOptions, IEnumerable<CompilationLibrary> compileLibraries, IEnumerable<RuntimeLibrary> runtimeLibraries, IEnumerable<RuntimeFallbacks> runtimeGraph);

-        public CompilationOptions CompilationOptions { get; }

-        public IReadOnlyList<CompilationLibrary> CompileLibraries { get; }

-        public static DependencyContext Default { get; }

-        public IReadOnlyList<RuntimeFallbacks> RuntimeGraph { get; }

-        public IReadOnlyList<RuntimeLibrary> RuntimeLibraries { get; }

-        public TargetInfo Target { get; }

-        public static DependencyContext Load(Assembly assembly);

-        public DependencyContext Merge(DependencyContext other);

-    }
-    public static class DependencyContextExtensions {
 {
-        public static IEnumerable<AssemblyName> GetDefaultAssemblyNames(this DependencyContext self);

-        public static IEnumerable<AssemblyName> GetDefaultAssemblyNames(this RuntimeLibrary self, DependencyContext context);

-        public static IEnumerable<string> GetDefaultNativeAssets(this DependencyContext self);

-        public static IEnumerable<string> GetDefaultNativeAssets(this RuntimeLibrary self, DependencyContext context);

-        public static IEnumerable<RuntimeFile> GetDefaultNativeRuntimeFileAssets(this DependencyContext self);

-        public static IEnumerable<RuntimeFile> GetDefaultNativeRuntimeFileAssets(this RuntimeLibrary self, DependencyContext context);

-        public static IEnumerable<AssemblyName> GetRuntimeAssemblyNames(this DependencyContext self, string runtimeIdentifier);

-        public static IEnumerable<AssemblyName> GetRuntimeAssemblyNames(this RuntimeLibrary self, DependencyContext context, string runtimeIdentifier);

-        public static IEnumerable<string> GetRuntimeNativeAssets(this DependencyContext self, string runtimeIdentifier);

-        public static IEnumerable<string> GetRuntimeNativeAssets(this RuntimeLibrary self, DependencyContext context, string runtimeIdentifier);

-        public static IEnumerable<RuntimeFile> GetRuntimeNativeRuntimeFileAssets(this DependencyContext self, string runtimeIdentifier);

-        public static IEnumerable<RuntimeFile> GetRuntimeNativeRuntimeFileAssets(this RuntimeLibrary self, DependencyContext context, string runtimeIdentifier);

-    }
-    public class DependencyContextJsonReader : IDependencyContextReader, IDisposable {
 {
-        public DependencyContextJsonReader();

-        public void Dispose();

-        protected virtual void Dispose(bool disposing);

-        public DependencyContext Read(Stream stream);

-        public IEnumerable<Dependency> ReadTargetLibraryDependencies(JsonTextReader reader);

-    }
-    public class DependencyContextLoader {
 {
-        public DependencyContextLoader();

-        public static DependencyContextLoader Default { get; }

-        public DependencyContext Load(Assembly assembly);

-    }
-    public class DependencyContextWriter {
 {
-        public DependencyContextWriter();

-        public void Write(DependencyContext context, Stream stream);

-    }
-    public interface IDependencyContextReader : IDisposable {
 {
-        DependencyContext Read(Stream stream);

-    }
-    public class Library {
 {
-        public Library(string type, string name, string version, string hash, IEnumerable<Dependency> dependencies, bool serviceable);

-        public Library(string type, string name, string version, string hash, IEnumerable<Dependency> dependencies, bool serviceable, string path, string hashPath);

-        public Library(string type, string name, string version, string hash, IEnumerable<Dependency> dependencies, bool serviceable, string path, string hashPath, string runtimeStoreManifestName = null);

-        public IReadOnlyList<Dependency> Dependencies { get; }

-        public string Hash { get; }

-        public string HashPath { get; }

-        public string Name { get; }

-        public string Path { get; }

-        public string RuntimeStoreManifestName { get; }

-        public bool Serviceable { get; }

-        public string Type { get; }

-        public string Version { get; }

-    }
-    public class ResourceAssembly {
 {
-        public ResourceAssembly(string path, string locale);

-        public string Locale { get; set; }

-        public string Path { get; set; }

-    }
-    public class RuntimeAssembly {
 {
-        public RuntimeAssembly(string assemblyName, string path);

-        public AssemblyName Name { get; }

-        public string Path { get; }

-        public static RuntimeAssembly Create(string path);

-    }
-    public class RuntimeAssetGroup {
 {
-        public RuntimeAssetGroup(string runtime, IEnumerable<RuntimeFile> runtimeFiles);

-        public RuntimeAssetGroup(string runtime, IEnumerable<string> assetPaths);

-        public RuntimeAssetGroup(string runtime, params string[] assetPaths);

-        public IReadOnlyList<string> AssetPaths { get; }

-        public string Runtime { get; }

-        public IReadOnlyList<RuntimeFile> RuntimeFiles { get; }

-    }
-    public class RuntimeFallbacks {
 {
-        public RuntimeFallbacks(string runtime, IEnumerable<string> fallbacks);

-        public RuntimeFallbacks(string runtime, params string[] fallbacks);

-        public IReadOnlyList<string> Fallbacks { get; set; }

-        public string Runtime { get; set; }

-    }
-    public class RuntimeFile {
 {
-        public RuntimeFile(string path, string assemblyVersion, string fileVersion);

-        public string AssemblyVersion { get; }

-        public string FileVersion { get; }

-        public string Path { get; }

-    }
-    public class RuntimeLibrary : Library {
 {
-        public RuntimeLibrary(string type, string name, string version, string hash, IReadOnlyList<RuntimeAssetGroup> runtimeAssemblyGroups, IReadOnlyList<RuntimeAssetGroup> nativeLibraryGroups, IEnumerable<ResourceAssembly> resourceAssemblies, IEnumerable<Dependency> dependencies, bool serviceable);

-        public RuntimeLibrary(string type, string name, string version, string hash, IReadOnlyList<RuntimeAssetGroup> runtimeAssemblyGroups, IReadOnlyList<RuntimeAssetGroup> nativeLibraryGroups, IEnumerable<ResourceAssembly> resourceAssemblies, IEnumerable<Dependency> dependencies, bool serviceable, string path, string hashPath);

-        public RuntimeLibrary(string type, string name, string version, string hash, IReadOnlyList<RuntimeAssetGroup> runtimeAssemblyGroups, IReadOnlyList<RuntimeAssetGroup> nativeLibraryGroups, IEnumerable<ResourceAssembly> resourceAssemblies, IEnumerable<Dependency> dependencies, bool serviceable, string path, string hashPath, string runtimeStoreManifestName);

-        public IReadOnlyList<RuntimeAssetGroup> NativeLibraryGroups { get; }

-        public IReadOnlyList<ResourceAssembly> ResourceAssemblies { get; }

-        public IReadOnlyList<RuntimeAssetGroup> RuntimeAssemblyGroups { get; }

-    }
-    public class TargetInfo {
 {
-        public TargetInfo(string framework, string runtime, string runtimeSignature, bool isPortable);

-        public string Framework { get; }

-        public bool IsPortable { get; }

-        public string Runtime { get; }

-        public string RuntimeSignature { get; }

-    }
-}
```

