# Microsoft.Extensions.DependencyModel.Resolution

``` diff
-namespace Microsoft.Extensions.DependencyModel.Resolution {
 {
-    public class AppBaseCompilationAssemblyResolver : ICompilationAssemblyResolver {
 {
-        public AppBaseCompilationAssemblyResolver();

-        public AppBaseCompilationAssemblyResolver(string basePath);

-        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies);

-    }
-    public class CompositeCompilationAssemblyResolver : ICompilationAssemblyResolver {
 {
-        public CompositeCompilationAssemblyResolver(ICompilationAssemblyResolver[] resolvers);

-        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies);

-    }
-    public class DotNetReferenceAssembliesPathResolver {
 {
-        public static readonly string DotNetReferenceAssembliesPathEnv;

-        public DotNetReferenceAssembliesPathResolver();

-        public static string Resolve();

-    }
-    public interface ICompilationAssemblyResolver {
 {
-        bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies);

-    }
-    public class PackageCompilationAssemblyResolver : ICompilationAssemblyResolver {
 {
-        public PackageCompilationAssemblyResolver();

-        public PackageCompilationAssemblyResolver(string nugetPackageDirectory);

-        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies);

-    }
-    public class ReferenceAssemblyPathResolver : ICompilationAssemblyResolver {
 {
-        public ReferenceAssemblyPathResolver();

-        public ReferenceAssemblyPathResolver(string defaultReferenceAssembliesPath, string[] fallbackSearchPaths);

-        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies);

-    }
-}
```

