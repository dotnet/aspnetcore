# Microsoft.AspNetCore.Mvc.Razor.Compilation

``` diff
 namespace Microsoft.AspNetCore.Mvc.Razor.Compilation {
-    public class CompilationFailedException : Exception, ICompilationException {
 {
-        public CompilationFailedException(IEnumerable<CompilationFailure> compilationFailures);

-        public IEnumerable<CompilationFailure> CompilationFailures { get; }

-    }
     public class CompiledViewDescriptor {
         public CompiledViewDescriptor();
+        public CompiledViewDescriptor(RazorCompiledItem item);
         public CompiledViewDescriptor(RazorCompiledItem item, RazorViewAttribute attribute);
         public IList<IChangeToken> ExpirationTokens { get; set; }
-        public bool IsPrecompiled { get; set; }

         public RazorCompiledItem Item { get; set; }
         public string RelativePath { get; set; }
         public Type Type { get; }
         public RazorViewAttribute ViewAttribute { get; set; }
     }
-    public interface IViewCompilationMemoryCacheProvider {
 {
-        IMemoryCache CompilationMemoryCache { get; }

-    }
     public interface IViewCompiler {
         Task<CompiledViewDescriptor> CompileAsync(string relativePath);
     }
     public interface IViewCompilerProvider {
         IViewCompiler GetCompiler();
     }
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
     public class RazorViewAttribute : Attribute {
         public RazorViewAttribute(string path, Type viewType);
         public string Path { get; }
         public Type ViewType { get; }
     }
-    public class RoslynCompilationContext {
 {
-        public RoslynCompilationContext(CSharpCompilation compilation);

-        public CSharpCompilation Compilation { get; set; }

-    }
     public class ViewsFeature {
         public ViewsFeature();
         public IList<CompiledViewDescriptor> ViewDescriptors { get; }
     }
-    public class ViewsFeatureProvider : IApplicationFeatureProvider, IApplicationFeatureProvider<ViewsFeature> {
 {
-        public static readonly string PrecompiledViewsAssemblySuffix;

-        public ViewsFeatureProvider();

-        protected virtual IEnumerable<RazorViewAttribute> GetViewAttributes(AssemblyPart assemblyPart);

-        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature);

-    }
 }
```

