# Microsoft.AspNetCore.Mvc.Razor.Compilation

``` diff
 namespace Microsoft.AspNetCore.Mvc.Razor.Compilation {
-    public class CompilationFailedException : Exception, ICompilationException {
 {
-        public CompilationFailedException(IEnumerable<CompilationFailure> compilationFailures);

-        public IEnumerable<CompilationFailure> CompilationFailures { get; }

-    }
     public class CompiledViewDescriptor {
+        public CompiledViewDescriptor(RazorCompiledItem item);
-        public bool IsPrecompiled { get; set; }

     }
-    public interface IViewCompilationMemoryCacheProvider {
 {
-        IMemoryCache CompilationMemoryCache { get; }

-    }
-    public class MetadataReferenceFeature {
 {
-        public MetadataReferenceFeature();

-        public IList<MetadataReference> MetadataReferences { get; }

-    }
-    public class MetadataReferenceFeatureProvider : IApplicationFeatureProvider, IApplicationFeatureProvider<MetadataReferenceFeature> {
 {
-        public MetadataReferenceFeatureProvider();

-        public void PopulateFeature(IEnumerable<ApplicationPart> parts, MetadataReferenceFeature feature);

-    }
-    public abstract class RazorReferenceManager {
 {
-        protected RazorReferenceManager();

-        public abstract IReadOnlyList<MetadataReference> CompilationReferences { get; }

-    }
-    public class RoslynCompilationContext {
 {
-        public RoslynCompilationContext(CSharpCompilation compilation);

-        public CSharpCompilation Compilation { get; set; }

-    }
-    public class ViewsFeatureProvider : IApplicationFeatureProvider, IApplicationFeatureProvider<ViewsFeature> {
 {
-        public static readonly string PrecompiledViewsAssemblySuffix;

-        public ViewsFeatureProvider();

-        protected virtual IEnumerable<RazorViewAttribute> GetViewAttributes(AssemblyPart assemblyPart);

-        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature);

-    }
 }
```

